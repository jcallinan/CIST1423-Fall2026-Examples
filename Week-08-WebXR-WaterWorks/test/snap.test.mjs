import assert from 'node:assert/strict';
import test from 'node:test';
import { Group, Vector3 } from 'three';

import { createPartObject } from '../src/parts.js';
import { applySnap, connectedPorts, findSnap, worldPorts } from '../src/snap.js';

const place = (type, [x, y, z], rotY = 0) => {
	const scene = new Group();
	const o = createPartObject(type);
	o.position.set(x, y, z);
	o.rotation.y = rotY;
	scene.add(o);
	return o;
};

test('two pipes near each other snap end to end', () => {
	const a = place('pipe', [0, 0.5, 0]);
	const b = place('pipe', [0.45, 0.53, 0.02]); // ~5 cm gap, slightly off-line
	const snap = findSnap(b, [a]);
	assert.ok(snap, 'found a snap');
	applySnap(b, snap);
	assert.ok(b.position.distanceTo(new Vector3(0.4, 0.5, 0)) < 1e-6, `b at ${b.position.toArray()}`);
	assert.equal(connectedPorts([a, b]).size, 2);
});

test('too far apart: no snap', () => {
	const a = place('pipe', [0, 0.5, 0]);
	const b = place('pipe', [0.7, 0.5, 0]);
	assert.equal(findSnap(b, [a]), null);
});

test('a rotated part is turned to face the port it snaps to', () => {
	const pipe = place('pipe', [0, 0.5, 0]);
	// Elbow near the pipe's +X end, rotated so one of its ports roughly faces the pipe end.
	const elbow = place('elbow', [0.26, 0.45, 0], Math.PI * 0.1); // its -X port is 18deg off facing the pipe
	const snap = findSnap(elbow, [pipe]);
	assert.ok(snap, 'found a snap');
	applySnap(elbow, snap);
	const [pipeEnd] = worldPorts(pipe).filter((p) => p.dir.x > 0.9);
	const touching = worldPorts(elbow).find((p) => p.pos.distanceTo(pipeEnd.pos) < 1e-6);
	assert.ok(touching, 'an elbow port now touches the pipe end');
	assert.ok(touching.dir.dot(pipeEnd.dir) < -0.999, 'and faces it exactly');
});

test('pump discharge takes a riser pipe pointing up', () => {
	const pump = place('pump', [0, 0.2, 0]);
	const riser = place('pipe', [-0.08, 0.6, 0]);
	riser.rotation.z = Math.PI / 2 - 0.2; // held roughly vertical
	const snap = findSnap(riser, [pump], 0.3);
	assert.ok(snap);
	applySnap(riser, snap);
	assert.equal(connectedPorts([pump, riser]).size, 2);
});

test('ports pointing the same way never snap', () => {
	const pipe = place('pipe', [0, 0.5, 0]);
	const elbow = place('elbow', [0.26, 0.45, 0], Math.PI * 0.9); // port faces away from the pipe end
	assert.equal(findSnap(elbow, [pipe]), null);
});
