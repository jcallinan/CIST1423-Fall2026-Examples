// End-to-end: start the real relay, connect two WebSocket clients, and play out a grab race.
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import WebSocket from 'ws';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const PORT = 18090 + Math.floor(Math.random() * 500);

function client(name) {
	const ws = new WebSocket(`ws://localhost:${PORT}/ws`);
	const inbox = [];
	const waiters = [];
	ws.on('message', (raw) => {
		const msg = JSON.parse(raw);
		const i = waiters.findIndex((w) => w.t === msg.t);
		if (i >= 0) waiters.splice(i, 1)[0].resolve(msg);
		else inbox.push(msg);
	});
	return {
		ws,
		send: (m) => ws.send(JSON.stringify(m)),
		next: (t) => {
			const i = inbox.findIndex((m) => m.t === t);
			if (i >= 0) return Promise.resolve(inbox.splice(i, 1)[0]);
			return new Promise((resolve, reject) => {
				waiters.push({ t, resolve });
				setTimeout(() => reject(new Error(`${name}: timed out waiting for ${t}`)), 3000);
			});
		},
	};
}

test('two clients share parts through the relay', async (t) => {
	const server = spawn(process.execPath, ['server/server.mjs', '--relay-only', `--port=${PORT}`], { cwd: root });
	t.after(() => server.kill());
	await new Promise((resolve) => server.stdout.on('data', (d) => String(d).includes('relay') && resolve()));

	const a = client('A');
	await once(a.ws, 'open');
	a.send({ t: 'join', room: 'test', name: 'Ana' });
	const welcomeA = await a.next('welcome');
	assert.equal(welcomeA.parts.length, 0);

	a.send({ t: 'spawn', part: { id: 'p1', type: 'pump', p: [0, 0.2, 0], q: [0, 0, 0, 1] } });

	const b = client('B');
	await once(b.ws, 'open');
	b.send({ t: 'join', room: 'test', name: 'Ben' });
	const welcomeB = await b.next('welcome');
	assert.deepEqual(welcomeB.parts.map((p) => p.id), ['p1'], 'late joiner sees the pump');
	assert.equal((await a.next('peer-join')).peer.name, 'Ben');

	a.send({ t: 'grab', id: 'p1' });
	assert.equal((await b.next('grab')).by, welcomeA.you.id);
	b.send({ t: 'grab', id: 'p1' });
	assert.equal((await b.next('grab-denied')).id, 'p1', 'second grab is denied');

	a.send({ t: 'move', id: 'p1', p: [0.5, 0.2, 0], q: [0, 0, 0, 1] });
	assert.deepEqual((await b.next('move')).p, [0.5, 0.2, 0]);

	a.ws.close();
	assert.equal((await b.next('peer-leave')).id, welcomeA.you.id);
	assert.equal((await b.next('release')).id, 'p1', 'part is dropped when its holder leaves');
	b.ws.close();
});
