/**
 * CIST 1423 - WaterWorks Builder
 * The shared workspace: every part in the scene, keyed by id. Both local input (grab, desktop drag)
 * and the network go through here, so there is exactly one place that adds/removes/moves parts.
 */

import { Mesh, MeshBasicMaterial, Quaternion, TorusGeometry, Vector3 } from 'three';
import { PART_TYPES, PIPE_R, createPartObject } from './parts';
import { applySnap, connectedPorts, findSnap } from './snap';

import { GrabComponent } from './grab';

const OPEN_PORT = new MeshBasicMaterial({ color: 0xff9800 });
const CONNECTED_PORT = new MeshBasicMaterial({ color: 0x4caf50 });
const PORT_RING = new TorusGeometry(PIPE_R * 1.7, 0.004, 6, 24);

let localCounter = 0;
const clientTag = Math.random().toString(36).slice(2, 7);
export const newPartId = () => `${clientTag}-${++localCounter}`;

export class Workspace {
	constructor(world, scene) {
		this.world = world;
		this.scene = scene;
		this.parts = new Map();
		this.listeners = new Set(); // (event, part) => void; the UI listens for 'changed'
	}

	on(fn) {
		this.listeners.add(fn);
	}

	_emit(event, part) {
		for (const fn of this.listeners) fn(event, part);
	}

	/** Create a part locally. Does not talk to the network; callers decide that. */
	add(type, { id = newPartId(), p = [0, 1, -0.5], q = [0, 0, 0, 1], state = null } = {}) {
		if (!PART_TYPES[type] || this.parts.has(id)) return this.parts.get(id) || null;
		const object3D = createPartObject(type);
		object3D.position.fromArray(p);
		object3D.quaternion.fromArray(q);
		object3D.userData.partId = id;
		for (const port of object3D.userData.ports) {
			const ring = new Mesh(PORT_RING, OPEN_PORT);
			ring.position.copy(port.pos);
			ring.quaternion.setFromUnitVectors(new Vector3(0, 0, 1), port.dir);
			ring.userData.isPortMarker = true;
			object3D.add(ring);
			port.marker = ring;
		}
		this.scene.add(object3D);
		const part = {
			id,
			type,
			object3D,
			state,
			heldBy: null, // null | 'me' | remote peer id
			target: null, // { p: Vector3, q: Quaternion } smoothing target for remote moves
		};
		part.entity = this.world.createEntity();
		part.entity.addComponent(GrabComponent, { object3D, part });
		this.parts.set(id, part);
		this.applyState(part);
		this.refreshConnections();
		this._emit('changed', part);
		return part;
	}

	remove(id) {
		const part = this.parts.get(id);
		if (!part) return;
		part.object3D.removeFromParent();
		part.entity.destroy();
		this.parts.delete(id);
		this.refreshConnections();
		this._emit('changed', part);
	}

	clear() {
		for (const id of [...this.parts.keys()]) this.remove(id);
	}

	/** Pose of a part in world space, as plain arrays for the wire. */
	pose(part) {
		const o = part.object3D;
		return {
			p: o.getWorldPosition(new Vector3()).toArray(),
			q: o.getWorldQuaternion(new Quaternion()).toArray(),
		};
	}

	/**
	 * Called after a local release (XR grab or desktop drag). Snaps to the nearest open port.
	 * Returns true if it snapped.
	 */
	settle(part) {
		// Only snap to parts that are sitting still (not in anyone's hand).
		const others = [...this.parts.values()].filter((p) => p !== part && p.heldBy === null).map((p) => p.object3D);
		const snap = findSnap(part.object3D, others);
		if (snap) applySnap(part.object3D, snap);
		this.refreshConnections();
		return !!snap;
	}

	refreshConnections() {
		const list = [...this.parts.values()];
		const connected = connectedPorts(list.map((p) => p.object3D));
		list.forEach((part, i) => {
			part.object3D.userData.ports.forEach((port, j) => {
				port.marker.material = connected.has(`${i}:${j}`) ? CONNECTED_PORT : OPEN_PORT;
			});
		});
		this.connectedCount = connected.size / 2;
		this._emit('connections');
	}

	/** Visual state (only gate valves have one: open / closed). */
	applyState(part) {
		if (part.type !== 'gate_valve') return;
		const wheel = part.object3D.getObjectByName('handwheel');
		const closed = part.state === 'closed';
		wheel.position.y = closed ? 0.16 : 0.19; // rising-stem valve: wheel sits lower when shut
		wheel.material = wheel.material.clone();
		wheel.material.color.set(closed ? 0x7a1c16 : 0xc8342b);
	}

	toggleValve(part) {
		part.state = part.state === 'closed' ? 'open' : 'closed';
		this.applyState(part);
		this._emit('changed', part);
		return part.state;
	}

	/** Remote parts ease toward their last network pose instead of teleporting. */
	smooth(delta) {
		const k = Math.min(1, delta * 15);
		for (const part of this.parts.values()) {
			if (!part.target) continue;
			part.object3D.position.lerp(part.target.p, k);
			part.object3D.quaternion.slerp(part.target.q, k);
			if (part.object3D.position.distanceTo(part.target.p) < 0.0005) {
				part.object3D.position.copy(part.target.p);
				part.object3D.quaternion.copy(part.target.q);
				part.target = null;
				this.refreshConnections();
			}
		}
	}

	setTarget(part, p, q, immediate = false) {
		const target = { p: new Vector3().fromArray(p), q: new Quaternion().fromArray(q) };
		if (immediate) {
			part.object3D.position.copy(target.p);
			part.object3D.quaternion.copy(target.q);
			part.target = null;
			this.refreshConnections();
		} else {
			part.target = target;
		}
	}
}
