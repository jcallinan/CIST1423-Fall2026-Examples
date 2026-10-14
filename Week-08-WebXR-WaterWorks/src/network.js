/**
 * CIST 1423 - WaterWorks Builder
 * Multiplayer client. One WebSocket to the relay (server/server.mjs); JSON messages.
 *
 *   Local action          -> message                 Remote effect
 *   pull part off shelf   -> spawn {part, held}      part appears (in my hand)
 *   grab                  -> grab {id}               others can't grab it; server may deny
 *   carry (15 Hz)         -> move {id, p, q}         others see it glide
 *   let go / snap         -> release {id, p, q}
 *   valve wheel           -> state {id, state}
 *   bin / B button        -> delete {id}
 *   my head + hands       -> pose (15 Hz)            avatars.js draws me
 *
 * The app works offline (single player). If a relay is reachable, anything built before
 * connecting is uploaded on join.
 */

import { Quaternion, Vector3 } from 'three';

import { GlobalComponent } from './global';
import { PlayerComponent } from './player';
import { System } from 'elics';

const SEND_HZ = 15;

export function relayUrl() {
	const params = new URLSearchParams(location.search);
	if (params.get('server')) return params.get('server');
	if (location.hostname.endsWith('github.io')) return null; // static hosting: no relay there
	return `${location.protocol === 'https:' ? 'wss' : 'ws'}://${location.host}/ws`;
}

export class NetClient {
	constructor(workspace) {
		this.workspace = workspace;
		this.status = 'offline';
		this.me = null;
		this.peers = new Map(); // id -> { id, name, color, pose }
		this.listeners = new Set();
		this._ws = null;
		this._retry = 1000;
	}

	on(fn) {
		this.listeners.add(fn);
	}

	_emit(event, data) {
		for (const fn of this.listeners) fn(event, data);
	}

	_setStatus(status) {
		this.status = status;
		this._emit('status', status);
	}

	connect({ url = relayUrl(), room = 'lab', name, color } = {}) {
		this.options = { url, room, name, color };
		if (!url) {
			this._setStatus('offline');
			return;
		}
		this._setStatus('connecting');
		let ws;
		try {
			ws = new WebSocket(url);
		} catch {
			this._setStatus('offline');
			return;
		}
		this._ws = ws;
		ws.onopen = () => {
			this._retry = 1000;
			ws.send(JSON.stringify({ t: 'join', room, name, color }));
		};
		ws.onmessage = (e) => {
			try {
				this._receive(JSON.parse(e.data));
			} catch (err) {
				console.warn('[net] bad message', err);
			}
		};
		ws.onclose = () => {
			this._ws = null;
			this.peers.clear();
			this._emit('peers', this.peers);
			this._setStatus('offline');
			// Keep trying in the background; the lab server may just not be up yet.
			setTimeout(() => this.connect(this.options), this._retry);
			this._retry = Math.min(this._retry * 2, 15000);
		};
	}

	/** Rejoin with new options (e.g. a new display name). onclose schedules the reconnect. */
	reconnect(options) {
		this.options = options;
		this._retry = 200;
		if (this._ws) this._ws.close();
		else this.connect(options);
	}

	get online() {
		return this.status === 'online';
	}

	send(msg) {
		if (this._ws && this._ws.readyState === WebSocket.OPEN && this.online) this._ws.send(JSON.stringify(msg));
	}

	// ---- outgoing, called by grab.js / desktop.js / shelf.js -------------------------------
	spawn(part, held = false) {
		const { p, q } = this.workspace.pose(part);
		this.send({ t: 'spawn', held, part: { id: part.id, type: part.type, p, q, state: part.state } });
	}
	grab(part) {
		this.send({ t: 'grab', id: part.id });
	}
	move(part) {
		this.send({ t: 'move', id: part.id, ...this.workspace.pose(part) });
	}
	release(part) {
		this.send({ t: 'release', id: part.id, ...this.workspace.pose(part) });
	}
	state(part) {
		this.send({ t: 'state', id: part.id, state: part.state });
	}
	remove(part) {
		this.send({ t: 'delete', id: part.id });
	}
	clear() {
		this.send({ t: 'clear' });
	}

	// ---- incoming ---------------------------------------------------------------------------
	_receive(msg) {
		const ws = this.workspace;
		const part = msg.id ? ws.parts.get(msg.id) : null;
		switch (msg.t) {
			case 'welcome': {
				this.me = msg.you;
				this.peers = new Map(msg.peers.map((p) => [p.id, { ...p, pose: null }]));
				const known = new Set();
				for (const remote of msg.parts) {
					known.add(remote.id);
					const existing = ws.parts.get(remote.id) || ws.add(remote.type, remote);
					existing.heldBy = remote.heldBy || null;
					if (existing.heldBy !== 'me') ws.setTarget(existing, remote.p, remote.q, true);
				}
				this._setStatus('online');
				// Offline-first: upload whatever we built before connecting.
				for (const local of ws.parts.values()) if (!known.has(local.id)) this.spawn(local, local.heldBy === 'me');
				this._emit('peers', this.peers);
				break;
			}
			case 'peer-join':
				this.peers.set(msg.peer.id, { ...msg.peer, pose: null });
				this._emit('peers', this.peers);
				break;
			case 'peer-leave':
				this.peers.delete(msg.id);
				this._emit('peers', this.peers);
				break;
			case 'pose': {
				const peer = this.peers.get(msg.id);
				if (peer) peer.pose = { head: msg.head, left: msg.left, right: msg.right, at: performance.now() };
				break;
			}
			case 'spawn': {
				const created = ws.add(msg.part.type, msg.part);
				if (created) created.heldBy = msg.part.heldBy || null;
				break;
			}
			case 'grab':
				if (part) part.heldBy = msg.by;
				break;
			case 'grab-denied':
				// Someone beat us to it. Drop it (grab.js/desktop.js watch forceDrop) and snap back.
				if (part) {
					part.forceDrop = true;
					part.heldBy = msg.heldBy;
					ws.setTarget(part, msg.p, msg.q);
				}
				break;
			case 'move':
				if (part && part.heldBy !== 'me') ws.setTarget(part, msg.p, msg.q);
				break;
			case 'release':
				if (part && part.heldBy !== 'me') {
					part.heldBy = null;
					ws.setTarget(part, msg.p, msg.q);
				}
				break;
			case 'state':
				if (part) {
					part.state = msg.state;
					ws.applyState(part);
				}
				break;
			case 'delete':
				ws.remove(msg.id);
				break;
			case 'clear':
				ws.clear();
				break;
		}
	}
}

/** Sends my pose and the pose of anything I'm carrying, SEND_HZ times a second. */
export class NetworkSystem extends System {
	init() {
		this._accum = 0;
		this._q = new Quaternion();
		this._v = new Vector3();
	}

	_poseArray(object) {
		object.getWorldPosition(this._v);
		object.getWorldQuaternion(this._q);
		return [...this._v.toArray(), ...this._q.toArray()];
	}

	update(delta) {
		const global = this.getEntities(this.queries.global)[0].getComponent(GlobalComponent);
		const player = this.getEntities(this.queries.player)[0]?.getComponent(PlayerComponent);
		const { net, workspace, renderer, camera } = global;
		workspace.smooth(delta);
		if (!net.online) return;

		this._accum += delta;
		if (this._accum < 1 / SEND_HZ) return;
		this._accum = 0;

		for (const part of workspace.parts.values()) if (part.heldBy === 'me') net.move(part);

		const inXR = renderer.xr.isPresenting;
		const msg = { t: 'pose', head: this._poseArray(inXR && player ? player.head : camera) };
		if (inXR && player) {
			for (const c of Object.values(player.controllers)) msg[c.handedness] = this._poseArray(c.gripSpace);
		}
		net.send(msg);
	}
}

NetworkSystem.queries = {
	global: { required: [GlobalComponent] },
	player: { required: [PlayerComponent] },
};
