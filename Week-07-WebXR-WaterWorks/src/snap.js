/**
 * CIST 1423 - WaterWorks Builder
 * Port snapping: when you let go of a part near another part's open port, rotate it so the two
 * ports face each other and slide it so they touch. Pure three.js math, so it runs in Node tests.
 */

import { Quaternion, Vector3 } from 'three';

export const SNAP_DISTANCE = 0.12; // metres between port centres
export const SNAP_MIN_OPPOSITION = -0.5; // dot(dirA, dirB) must be below this (within 60deg of facing)
export const CONNECTED_EPSILON = 0.01; // ports this close and opposed count as connected

/** World-space ports of an Object3D that carries userData.ports (local). */
export function worldPorts(object) {
	object.updateMatrixWorld(true);
	const q = object.getWorldQuaternion(new Quaternion());
	return (object.userData.ports || []).map((p, index) => ({
		index,
		pos: p.pos.clone().applyMatrix4(object.matrixWorld),
		dir: p.dir.clone().applyQuaternion(q).normalize(),
	}));
}

/**
 * Find the best snap for `moving` against `others` (arrays of Object3D).
 * Returns { target, movingPort, targetPort, distance } or null.
 */
export function findSnap(moving, others, maxDistance = SNAP_DISTANCE) {
	const mine = worldPorts(moving);
	let best = null;
	for (const other of others) {
		if (other === moving) continue;
		for (const tp of worldPorts(other)) {
			for (const mp of mine) {
				const distance = mp.pos.distanceTo(tp.pos);
				if (distance > maxDistance) continue;
				if (mp.dir.dot(tp.dir) > SNAP_MIN_OPPOSITION) continue;
				if (!best || distance < best.distance) {
					best = { target: other, movingPort: mp, targetPort: tp, distance };
				}
			}
		}
	}
	return best;
}

/**
 * Move/rotate `moving` (a direct child of the scene) so snap.movingPort meets snap.targetPort
 * face to face. Mutates moving.position / moving.quaternion.
 */
export function applySnap(moving, snap) {
	const align = new Quaternion().setFromUnitVectors(snap.movingPort.dir, snap.targetPort.dir.clone().negate());
	moving.quaternion.premultiply(align);
	moving.updateMatrixWorld(true);
	const local = moving.userData.ports[snap.movingPort.index].pos;
	const portWorld = local.clone().applyMatrix4(moving.matrixWorld);
	moving.position.add(new Vector3().subVectors(snap.targetPort.pos, portWorld));
	moving.updateMatrixWorld(true);
}

/**
 * All port-to-port connections between the given objects.
 * Returns a Set of "objectIndex:portIndex" keys that are connected.
 */
export function connectedPorts(objects) {
	const ports = objects.map((o) => worldPorts(o));
	const connected = new Set();
	for (let a = 0; a < objects.length; a++) {
		for (let b = a + 1; b < objects.length; b++) {
			for (const pa of ports[a]) {
				for (const pb of ports[b]) {
					if (pa.pos.distanceTo(pb.pos) < CONNECTED_EPSILON && pa.dir.dot(pb.dir) < -0.95) {
						connected.add(`${a}:${pa.index}`);
						connected.add(`${b}:${pb.index}`);
					}
				}
			}
		}
	}
	return connected;
}
