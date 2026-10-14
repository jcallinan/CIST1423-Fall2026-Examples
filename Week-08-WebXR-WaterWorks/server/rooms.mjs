/**
 * CIST 1423 - WaterWorks Builder: multiplayer room logic (no networking in here, so it's testable).
 *
 * The server is a *relay with memory*: it forwards messages between everyone in a room, and keeps
 * the current list of parts so a late joiner gets the whole build in one "welcome" snapshot.
 * It also enforces one rule: only one person can hold a part at a time (grab ownership).
 *
 * handle() returns a list of { to, msg } where `to` is 'all', 'others', or a peer id.
 */

const PALETTE = ['#ffb81c', '#4fc3f7', '#ef5350', '#66bb6a', '#ab47bc', '#ff7043', '#26c6da', '#d4e157'];
const MAX_PARTS = 400;

const isVec = (v, n) => Array.isArray(v) && v.length === n && v.every((x) => Number.isFinite(x));

export class Room {
	constructor(name) {
		this.name = name;
		this.peers = new Map(); // id -> { id, name, color }
		this.parts = new Map(); // id -> { id, type, p, q, state, heldBy }
		this._colorIndex = 0;
	}

	get isEmpty() {
		return this.peers.size === 0;
	}

	join(id, { name, color } = {}) {
		const peer = {
			id,
			name: String(name || `Guest ${id.slice(0, 4)}`).slice(0, 24),
			color: typeof color === 'string' && /^#[0-9a-f]{6}$/i.test(color) ? color : PALETTE[this._colorIndex++ % PALETTE.length],
		};
		this.peers.set(id, peer);
		return [
			{ to: id, msg: { t: 'welcome', you: peer, room: this.name, peers: [...this.peers.values()].filter((p) => p.id !== id), parts: [...this.parts.values()] } },
			{ to: 'others', msg: { t: 'peer-join', peer } },
		];
	}

	leave(id) {
		if (!this.peers.delete(id)) return [];
		const out = [{ to: 'all', msg: { t: 'peer-leave', id } }];
		// Anything they were holding is dropped where it is.
		for (const part of this.parts.values()) {
			if (part.heldBy === id) {
				part.heldBy = null;
				out.push({ to: 'all', msg: { t: 'release', id: part.id, p: part.p, q: part.q, by: id } });
			}
		}
		return out;
	}

	handle(from, msg) {
		if (!msg || typeof msg.t !== 'string' || !this.peers.has(from)) return [];
		switch (msg.t) {
			case 'pose': {
				// Pure relay; never stored.
				return [{ to: 'others', msg: { t: 'pose', id: from, head: msg.head, left: msg.left, right: msg.right } }];
			}
			case 'spawn': {
				const part = msg.part;
				if (!part || typeof part.id !== 'string' || typeof part.type !== 'string') return [];
				if (!isVec(part.p, 3) || !isVec(part.q, 4)) return [];
				if (this.parts.has(part.id) || this.parts.size >= MAX_PARTS) return [];
				// A part spawned "in hand" starts out held by its spawner.
				const stored = { id: part.id, type: part.type, p: part.p, q: part.q, state: part.state ?? null, heldBy: msg.held ? from : null };
				this.parts.set(part.id, stored);
				return [{ to: 'others', msg: { t: 'spawn', part: stored, by: from } }];
			}
			case 'grab': {
				const part = this.parts.get(msg.id);
				if (!part) return [];
				if (part.heldBy && part.heldBy !== from) {
					return [{ to: from, msg: { t: 'grab-denied', id: part.id, heldBy: part.heldBy, p: part.p, q: part.q } }];
				}
				part.heldBy = from;
				return [{ to: 'others', msg: { t: 'grab', id: part.id, by: from } }];
			}
			case 'move': {
				const part = this.parts.get(msg.id);
				if (!part || part.heldBy !== from || !isVec(msg.p, 3) || !isVec(msg.q, 4)) return [];
				part.p = msg.p;
				part.q = msg.q;
				return [{ to: 'others', msg: { t: 'move', id: part.id, p: part.p, q: part.q } }];
			}
			case 'release': {
				const part = this.parts.get(msg.id);
				if (!part || (part.heldBy && part.heldBy !== from)) return [];
				if (isVec(msg.p, 3) && isVec(msg.q, 4)) {
					part.p = msg.p;
					part.q = msg.q;
				}
				part.heldBy = null;
				return [{ to: 'others', msg: { t: 'release', id: part.id, p: part.p, q: part.q, by: from } }];
			}
			case 'state': {
				// e.g. a gate valve opened/closed. Anyone may toggle a part nobody else is holding.
				const part = this.parts.get(msg.id);
				if (!part || (part.heldBy && part.heldBy !== from)) return [];
				part.state = msg.state ?? null;
				return [{ to: 'others', msg: { t: 'state', id: part.id, state: part.state } }];
			}
			case 'delete': {
				const part = this.parts.get(msg.id);
				if (!part || (part.heldBy && part.heldBy !== from)) return [];
				this.parts.delete(part.id);
				return [{ to: 'others', msg: { t: 'delete', id: part.id } }];
			}
			case 'clear': {
				this.parts.clear();
				return [{ to: 'others', msg: { t: 'clear', by: from } }];
			}
			default:
				return [];
		}
	}
}
