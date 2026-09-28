import assert from 'node:assert/strict';
import test from 'node:test';
import { Box3, Vector3 } from 'three';

import { PART_ORDER, PART_TYPES, createPartObject } from '../src/parts.js';

test('catalog has the water-system basics', () => {
	for (const type of ['pipe', 'elbow', 'tee', 'gate_valve', 'check_valve', 'pump', 'tank', 'hydrant', 'meter', 'gauge', 'strainer']) {
		assert.ok(PART_TYPES[type], `missing ${type}`);
	}
	assert.equal(PART_ORDER.length, Object.keys(PART_TYPES).length);
});

for (const type of PART_ORDER) {
	test(`${type}: builds, has ports, ports sit on the part and face outward`, () => {
		const obj = createPartObject(type);
		const ports = obj.userData.ports;
		assert.ok(ports.length >= 1, 'at least one port');
		assert.ok(obj.userData.radius > 0.03 && obj.userData.radius < 0.8, `sane radius ${obj.userData.radius}`);
		const box = new Box3().setFromObject(obj).expandByScalar(0.002);
		for (const p of ports) {
			assert.ok(Math.abs(p.dir.length() - 1) < 1e-6, 'unit direction');
			assert.ok(p.pos.dot(p.dir) > 0, 'port faces away from the part centre');
			const justInside = p.pos.clone().addScaledVector(p.dir, -0.005);
			assert.ok(box.containsPoint(justInside), `port ${p.pos.toArray()} is on the geometry`);
		}
	});
}

test('pump has suction on the axis and discharge on top', () => {
	const [suction, discharge] = createPartObject('pump').userData.ports;
	assert.ok(suction.dir.equals(new Vector3(-1, 0, 0)));
	assert.ok(discharge.dir.equals(new Vector3(0, 1, 0)));
});
