/**
 * CIST 1423 - WaterWorks Builder: all-in-one lab server.
 *
 *   npm start                 build, then serve dist/ + the multiplayer relay at https://<this-PC>:8443
 *   npm run relay             relay only, plain ws://localhost:8090 (used by `npm run serve` for dev)
 *
 * WebXR needs HTTPS, and an https page may only open wss:// sockets, so by default this serves both
 * over one self-signed HTTPS port. Every headset on the lab Wi-Fi opens the same URL.
 */

import fs from 'node:fs';
import http from 'node:http';
import https from 'node:https';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { randomUUID } from 'node:crypto';
import { WebSocketServer } from 'ws';
import { Room } from './rooms.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const args = new Set(process.argv.slice(2));
const argValue = (name, fallback) => {
	const hit = process.argv.find((a) => a.startsWith(`--${name}=`));
	return hit ? hit.split('=')[1] : fallback;
};

const RELAY_ONLY = args.has('--relay-only');
const PORT = Number(argValue('port', RELAY_ONLY ? 8090 : 8443));
const DIST = path.resolve(__dirname, '..', 'dist');

// ---------------------------------------------------------------------------
// Rooms
// ---------------------------------------------------------------------------

const rooms = new Map();
const getRoom = (name) => {
	if (!rooms.has(name)) rooms.set(name, new Room(name));
	return rooms.get(name);
};

function deliver(room, from, outbox, sockets) {
	for (const { to, msg } of outbox) {
		const data = JSON.stringify(msg);
		for (const [id, ws] of sockets) {
			if (!room.peers.has(id)) continue;
			if (to === 'all' || (to === 'others' && id !== from) || to === id) {
				if (ws.readyState === ws.OPEN) ws.send(data);
			}
		}
	}
}

const socketsByRoom = new Map(); // room name -> Map(peerId -> ws)

function attachRelay(server) {
	const wss = new WebSocketServer({ server, path: '/ws' });
	wss.on('connection', (ws) => {
		const id = randomUUID();
		let room = null;
		ws.on('message', (raw) => {
			let msg;
			try {
				msg = JSON.parse(raw);
			} catch {
				return;
			}
			if (!room) {
				if (msg.t !== 'join') return;
				const roomName = String(msg.room || 'lab').slice(0, 32);
				room = getRoom(roomName);
				if (!socketsByRoom.has(roomName)) socketsByRoom.set(roomName, new Map());
				socketsByRoom.get(roomName).set(id, ws);
				deliver(room, id, room.join(id, msg), socketsByRoom.get(roomName));
				console.log(`[relay] ${room.peers.get(id).name} joined "${roomName}" (${room.peers.size} in room)`);
				return;
			}
			deliver(room, id, room.handle(id, msg), socketsByRoom.get(room.name));
		});
		ws.on('close', () => {
			if (!room) return;
			const sockets = socketsByRoom.get(room.name);
			const out = room.leave(id);
			sockets.delete(id);
			deliver(room, id, out, sockets);
			console.log(`[relay] peer left "${room.name}" (${room.peers.size} in room)`);
			if (room.isEmpty) {
				// Keep an empty room's build around for 10 minutes in case people reconnect.
				setTimeout(() => {
					if (room.isEmpty) rooms.delete(room.name);
				}, 10 * 60 * 1000);
			}
		});
	});
	return wss;
}

// ---------------------------------------------------------------------------
// Static files (dist/)
// ---------------------------------------------------------------------------

const TYPES = {
	'.html': 'text/html; charset=utf-8',
	'.js': 'text/javascript',
	'.css': 'text/css',
	'.map': 'application/json',
	'.json': 'application/json',
	'.png': 'image/png',
	'.jpg': 'image/jpeg',
	'.ico': 'image/x-icon',
	'.glb': 'model/gltf-binary',
	'.gltf': 'model/gltf+json',
	'.exr': 'application/octet-stream',
	'.ktx2': 'image/ktx2',
	'.wasm': 'application/wasm',
	'.webm': 'audio/webm',
};

function serveStatic(req, res) {
	const url = new URL(req.url, 'http://x');
	let file = path.normalize(path.join(DIST, decodeURIComponent(url.pathname)));
	if (!file.startsWith(DIST)) {
		res.writeHead(403).end();
		return;
	}
	if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.html');
	fs.readFile(file, (err, data) => {
		if (err) {
			res.writeHead(404).end('Not found. Did you run `npm run build`?');
			return;
		}
		res.writeHead(200, { 'Content-Type': TYPES[path.extname(file)] || 'application/octet-stream' });
		res.end(data);
	});
}

// ---------------------------------------------------------------------------
// Start
// ---------------------------------------------------------------------------

async function certificate() {
	const dir = path.join(__dirname, '.cert');
	const keyFile = path.join(dir, 'key.pem');
	const certFile = path.join(dir, 'cert.pem');
	if (fs.existsSync(keyFile) && fs.existsSync(certFile)) {
		return { key: fs.readFileSync(keyFile), cert: fs.readFileSync(certFile) };
	}
	const selfsigned = (await import('selfsigned')).default;
	const pems = await selfsigned.generate([{ name: 'commonName', value: 'waterworks.local' }], { days: 365, keySize: 2048 });
	fs.mkdirSync(dir, { recursive: true });
	fs.writeFileSync(keyFile, pems.private);
	fs.writeFileSync(certFile, pems.cert);
	return { key: pems.private, cert: pems.cert };
}

const lanAddresses = () =>
	Object.values(os.networkInterfaces())
		.flat()
		.filter((a) => a && a.family === 'IPv4' && !a.internal)
		.map((a) => a.address);

if (RELAY_ONLY) {
	const server = http.createServer((req, res) => res.writeHead(200).end('WaterWorks relay'));
	attachRelay(server);
	server.listen(PORT, () => console.log(`[relay] ws://localhost:${PORT}/ws`));
} else {
	const server = https.createServer(await certificate(), serveStatic);
	attachRelay(server);
	server.listen(PORT, '0.0.0.0', () => {
		console.log('WaterWorks Builder is running. Open one of these in each headset or browser:');
		for (const ip of ['localhost', ...lanAddresses()]) console.log(`   https://${ip}:${PORT}/`);
		console.log('(Accept the self-signed certificate warning once per device.)');
	});
}
