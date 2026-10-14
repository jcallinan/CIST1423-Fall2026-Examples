import assert from 'node:assert/strict';
import test from 'node:test';

import { Room } from '../server/rooms.mjs';

const P = [0, 1, 0];
const Q = [0, 0, 0, 1];
const find = (out, t) => out.find((o) => o.msg.t === t);

test('late joiner gets a snapshot of peers and parts', () => {
	const room = new Room('lab');
	room.join('a', { name: 'Ana' });
	room.handle('a', { t: 'spawn', part: { id: 'a-1', type: 'pump', p: P, q: Q } });
	const out = room.join('b', { name: 'Ben' });
	const welcome = find(out, 'welcome');
	assert.equal(welcome.to, 'b');
	assert.deepEqual(welcome.msg.peers.map((p) => p.name), ['Ana']);
	assert.deepEqual(welcome.msg.parts.map((p) => p.type), ['pump']);
	assert.equal(find(out, 'peer-join').to, 'others');
});

test('only one person can hold a part', () => {
	const room = new Room('lab');
	room.join('a');
	room.join('b');
	room.handle('a', { t: 'spawn', part: { id: 'x', type: 'pipe', p: P, q: Q } });
	assert.equal(room.handle('a', { t: 'grab', id: 'x' })[0].msg.t, 'grab');
	const denied = room.handle('b', { t: 'grab', id: 'x' });
	assert.equal(denied[0].to, 'b');
	assert.equal(denied[0].msg.t, 'grab-denied');
	// b can't move, delete or release it either
	assert.deepEqual(room.handle('b', { t: 'move', id: 'x', p: [9, 9, 9], q: Q }), []);
	assert.deepEqual(room.handle('b', { t: 'delete', id: 'x' }), []);
	assert.deepEqual(room.handle('b', { t: 'release', id: 'x', p: [9, 9, 9], q: Q }), []);
	assert.deepEqual(room.parts.get('x').p, P);
	// a moves and releases; then b may grab
	room.handle('a', { t: 'move', id: 'x', p: [1, 1, 1], q: Q });
	room.handle('a', { t: 'release', id: 'x', p: [2, 1, 0], q: Q });
	assert.deepEqual(room.parts.get('x').p, [2, 1, 0]);
	assert.equal(room.handle('b', { t: 'grab', id: 'x' })[0].msg.t, 'grab');
});

test('leaving drops whatever you were holding', () => {
	const room = new Room('lab');
	room.join('a');
	room.join('b');
	room.handle('a', { t: 'spawn', held: true, part: { id: 'x', type: 'tank', p: P, q: Q } });
	assert.equal(room.parts.get('x').heldBy, 'a');
	const out = room.leave('a');
	assert.ok(find(out, 'peer-leave'));
	assert.ok(find(out, 'release'));
	assert.equal(room.parts.get('x').heldBy, null);
});

test('bad input is ignored', () => {
	const room = new Room('lab');
	room.join('a');
	assert.deepEqual(room.handle('a', { t: 'spawn', part: { id: 'x', type: 'pipe', p: [NaN, 0, 0], q: Q } }), []);
	assert.deepEqual(room.handle('a', { t: 'nope' }), []);
	assert.deepEqual(room.handle('stranger', { t: 'clear' }), []);
	assert.equal(room.parts.size, 0);
});

test('valve state is shared', () => {
	const room = new Room('lab');
	room.join('a');
	room.handle('a', { t: 'spawn', part: { id: 'v', type: 'gate_valve', p: P, q: Q, state: 'open' } });
	const out = room.handle('a', { t: 'state', id: 'v', state: 'closed' });
	assert.equal(out[0].msg.state, 'closed');
	assert.equal(room.parts.get('v').state, 'closed');
});
