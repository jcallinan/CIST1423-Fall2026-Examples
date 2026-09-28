/**
 * CIST 1423 - WaterWorks Builder
 * Desktop / laptop mode (and the page around the 3D view): parts palette, click-and-drag building,
 * people list, connection status, and the Enter VR / MR button.
 *
 * Mouse:  drag a part to slide it along the floor · Shift+drag to lift/lower · drag empty space to orbit
 * Keys:   R / Shift+R rotate 90° around vertical · T tip forward · E roll · V open/close valve
 *         Delete remove · Esc deselect
 */

import { ARButton, VRButton } from 'ratk';
import { Plane, Raycaster, Vector2, Vector3 } from 'three';
import { PART_ORDER, PART_TYPES } from './parts';

import { GlobalComponent } from './global';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import { System } from 'elics';

const $ = (id) => document.getElementById(id);

export class DesktopSystem extends System {
	init() {
		this._ready = false;
		this._raycaster = new Raycaster();
		this._pointer = new Vector2();
		this._plane = new Plane();
		this._offset = new Vector3();
		this._hit = new Vector3();
		this.selected = null;
		this.drag = null;
	}

	_setup(global) {
		const { renderer, camera, workspace, net } = global;
		this.global = global;

		camera.position.set(1.4, 1.4, 1.8);
		this.orbit = new OrbitControls(camera, renderer.domElement);
		this.orbit.target.set(0, 0.35, 0);
		this.orbit.enableDamping = true;
		this.orbit.maxPolarAngle = Math.PI * 0.49;
		this.orbit.update();
		if (window.waterworks) window.waterworks.orbit = this.orbit;

		this._buildPalette();
		this._bindPointer(renderer.domElement);
		this._bindKeys();
		this._bindSelectionButtons();
		this._bindXRButton(renderer);

		$('clear-all').onclick = () => {
			if (!workspace.parts.size || !confirm('Remove every part for everyone in this room?')) return;
			this._select(null);
			workspace.clear();
			net.clear();
		};

		// Name (remembered per browser) and room (from ?room=, default "lab").
		const nameInput = $('player-name');
		let name = null;
		try {
			name = localStorage.getItem('waterworks-name');
		} catch {
			/* storage blocked */
		}
		nameInput.value = name || `Builder ${Math.floor(Math.random() * 90 + 10)}`;
		nameInput.onchange = () => {
			try {
				localStorage.setItem('waterworks-name', nameInput.value);
			} catch {
				/* storage blocked */
			}
			net.reconnect({ ...net.options, name: nameInput.value });
		};
		const room = new URLSearchParams(location.search).get('room') || 'lab';
		$('room-name').textContent = room;

		net.on((event) => {
			if (event === 'status') this._renderStatus();
			if (event === 'peers') this._renderPeers();
		});
		workspace.on(() => this._renderCounts());
		net.connect({ room, name: nameInput.value });
		this._renderStatus();
		this._renderCounts();

		renderer.xr.addEventListener('sessionstart', () => {
			this._select(null);
			$('config-panel').style.display = 'none';
			this.orbit.enabled = false;
		});
		renderer.xr.addEventListener('sessionend', () => {
			$('config-panel').style.display = 'flex';
			this.orbit.enabled = true;
			camera.position.set(1.4, 1.4, 1.8);
			this.orbit.update();
		});
	}

	// ---- palette ----------------------------------------------------------------------------
	_buildPalette() {
		const container = $('parts-palette');
		const byCategory = {};
		for (const type of PART_ORDER) (byCategory[PART_TYPES[type].category] ||= []).push(type);
		for (const [category, types] of Object.entries(byCategory)) {
			const heading = document.createElement('div');
			heading.className = 'palette-category';
			heading.textContent = category;
			container.appendChild(heading);
			const grid = document.createElement('div');
			grid.className = 'palette-grid';
			for (const type of types) {
				const btn = document.createElement('button');
				btn.className = 'part-btn';
				btn.dataset.type = type;
				btn.title = PART_TYPES[type].info;
				btn.textContent = PART_TYPES[type].name;
				btn.onclick = () => this._spawn(type);
				grid.appendChild(btn);
			}
			container.appendChild(grid);
		}
	}

	_spawn(type) {
		const { workspace, net } = this.global;
		// Drop new parts near the middle of the view, staggered so they don't stack exactly.
		const n = workspace.parts.size;
		const p = this.orbit.target.clone().add(new Vector3(((n % 5) - 2) * 0.12, 0.05, (Math.floor(n / 5) % 3) * 0.12));
		const part = workspace.add(type, { p: p.toArray() });
		net.spawn(part);
		this._select(part);
	}

	// ---- selection --------------------------------------------------------------------------
	_select(part) {
		if (this.selected) this._tint(this.selected, false);
		this.selected = part && this.global.workspace.parts.has(part.id) ? part : null;
		if (this.selected) this._tint(this.selected, true);
		const info = $('selection-info');
		if (!this.selected) {
			info.hidden = true;
			return;
		}
		info.hidden = false;
		const def = PART_TYPES[this.selected.type];
		$('selection-name').textContent = def.name;
		$('selection-desc').textContent = def.info;
		$('btn-valve').hidden = this.selected.type !== 'gate_valve';
		$('btn-valve').textContent = this.selected.state === 'closed' ? 'Open valve (V)' : 'Close valve (V)';
	}

	_tint(part, on) {
		part.object3D.traverse((child) => {
			if (child.isMesh && child.material.emissive) child.material.emissive.setHex(on ? 0x1d3f66 : 0x000000);
		});
	}

	_partFromHit(object) {
		while (object && !object.userData.partId) object = object.parent;
		return object ? this.global.workspace.parts.get(object.userData.partId) : null;
	}

	/** Change a selected part's pose as one grab/release, so the room sees it and it re-snaps. */
	_edit(fn) {
		const part = this.selected;
		if (!part || part.heldBy) return;
		const { workspace, net } = this.global;
		net.grab(part);
		fn(part.object3D);
		part.object3D.updateMatrixWorld(true);
		workspace.settle(part);
		net.release(part);
	}

	_remove() {
		const part = this.selected;
		if (!part || part.heldBy) return;
		this._select(null);
		this.global.workspace.remove(part.id);
		this.global.net.remove(part);
	}

	_toggleValve() {
		const part = this.selected;
		if (!part || part.type !== 'gate_valve' || part.heldBy) return;
		this.global.workspace.toggleValve(part);
		this.global.net.state(part);
		this._select(part);
	}

	_bindSelectionButtons() {
		$('btn-rot-y').onclick = () => this._edit((o) => o.rotateOnWorldAxis(new Vector3(0, 1, 0), Math.PI / 2));
		$('btn-rot-x').onclick = () => this._edit((o) => o.rotateOnWorldAxis(new Vector3(1, 0, 0), Math.PI / 2));
		$('btn-rot-z').onclick = () => this._edit((o) => o.rotateOnWorldAxis(new Vector3(0, 0, 1), Math.PI / 2));
		$('btn-delete').onclick = () => this._remove();
		$('btn-valve').onclick = () => this._toggleValve();
	}

	_bindKeys() {
		window.addEventListener('keydown', (e) => {
			if (e.target.tagName === 'INPUT') return;
			const key = e.key.toLowerCase();
			if (key === 'r') this._edit((o) => o.rotateOnWorldAxis(new Vector3(0, 1, 0), (e.shiftKey ? -1 : 1) * Math.PI / 2));
			else if (key === 't') this._edit((o) => o.rotateOnWorldAxis(new Vector3(1, 0, 0), Math.PI / 2));
			else if (key === 'e') this._edit((o) => o.rotateOnWorldAxis(new Vector3(0, 0, 1), Math.PI / 2));
			else if (key === 'v') this._toggleValve();
			else if (key === 'delete' || key === 'backspace') this._remove();
			else if (key === 'escape') this._select(null);
		});
	}

	// ---- mouse drag -------------------------------------------------------------------------
	_ray(e, el) {
		const rect = el.getBoundingClientRect();
		this._pointer.set(((e.clientX - rect.left) / rect.width) * 2 - 1, -((e.clientY - rect.top) / rect.height) * 2 + 1);
		this._raycaster.setFromCamera(this._pointer, this.global.camera);
	}

	_bindPointer(el) {
		el.addEventListener('pointerdown', (e) => {
			if (e.button !== 0 || this.global.renderer.xr.isPresenting) return;
			this._ray(e, el);
			const objects = [...this.global.workspace.parts.values()].map((p) => p.object3D);
			const hit = this._raycaster.intersectObjects(objects, true)[0];
			const part = hit && this._partFromHit(hit.object);
			if (!part) {
				this._select(null);
				return;
			}
			this._select(part);
			if (part.heldBy) return; // someone else has it
			const o = part.object3D;
			// Drag on a horizontal plane (or a vertical, camera-facing one with Shift).
			if (e.shiftKey) {
				const n = this.global.camera.getWorldDirection(new Vector3()).setY(0).normalize();
				this._plane.setFromNormalAndCoplanarPoint(n, hit.point);
			} else {
				this._plane.setFromNormalAndCoplanarPoint(new Vector3(0, 1, 0), hit.point);
			}
			this._offset.subVectors(o.position, hit.point);
			this.drag = { part, vertical: e.shiftKey };
			part.heldBy = 'me';
			this.global.net.grab(part);
			this.orbit.enabled = false;
			el.setPointerCapture(e.pointerId);
		});
		el.addEventListener('pointermove', (e) => {
			if (!this.drag) return;
			const part = this.drag.part;
			if (part.forceDrop) return this._endDrag(false);
			this._ray(e, el);
			if (!this._raycaster.ray.intersectPlane(this._plane, this._hit)) return;
			const next = this._hit.add(this._offset);
			if (this.drag.vertical) part.object3D.position.y = Math.max(-0.2, next.y);
			else part.object3D.position.set(next.x, part.object3D.position.y, next.z);
		});
		const end = () => this.drag && this._endDrag(true);
		el.addEventListener('pointerup', end);
		el.addEventListener('pointercancel', end);
	}

	_endDrag(release) {
		const { part } = this.drag;
		this.drag = null;
		this.orbit.enabled = true;
		if (part.forceDrop) {
			part.forceDrop = false;
			return;
		}
		part.heldBy = null;
		if (release && this.global.workspace.parts.has(part.id)) {
			this.global.workspace.settle(part);
			this.global.net.release(part);
		}
	}

	// ---- XR button (same AR -> VR fallback as panther-builder) --------------------------------
	async _bindXRButton(renderer) {
		const button = $('ar-button');
		const message = $('xr-status-message');
		const supports = async (mode) => {
			try {
				return !!(navigator.xr && (await navigator.xr.isSessionSupported(mode)));
			} catch {
				return false;
			}
		};
		if (await supports('immersive-ar')) {
			ARButton.convertToARButton(button, renderer, {
				ENTER_XR_TEXT: 'Build in Mixed Reality',
				LEAVE_XR_TEXT: 'Exit Mixed Reality',
				requiredFeatures: [],
				optionalFeatures: ['local-floor', 'bounded-floor', 'hit-test'],
			});
		} else if (await supports('immersive-vr')) {
			VRButton.convertToVRButton(button, renderer, {
				ENTER_XR_TEXT: 'Build in VR',
				LEAVE_XR_TEXT: 'Exit VR',
				requiredFeatures: [],
				optionalFeatures: ['local-floor', 'bounded-floor'],
			});
		} else {
			button.disabled = true;
			button.textContent = 'VR / MR not available here';
			message.hidden = false;
		}
	}

	// ---- status panels ----------------------------------------------------------------------
	_renderStatus() {
		const { net } = this.global;
		const el = $('net-status');
		el.dataset.status = net.status;
		el.textContent = { online: 'Connected', connecting: 'Connecting…', offline: 'Offline (single player)' }[net.status];
	}

	_renderPeers() {
		const { net } = this.global;
		const list = $('people-list');
		list.innerHTML = '';
		const people = [...(net.me ? [{ ...net.me, you: true }] : []), ...net.peers.values()];
		for (const p of people) {
			const li = document.createElement('li');
			const dot = document.createElement('span');
			dot.className = 'person-dot';
			dot.style.background = p.color;
			li.appendChild(dot);
			li.appendChild(document.createTextNode(p.you ? `${p.name} (you)` : p.name));
			list.appendChild(li);
		}
	}

	_renderCounts() {
		const { workspace } = this.global;
		$('part-count').textContent = `${workspace.parts.size} parts · ${workspace.connectedCount || 0} connections`;
		if (this.selected && !workspace.parts.has(this.selected.id)) this._select(null);
	}

	update() {
		const global = this.getEntities(this.queries.global)[0]?.getComponent(GlobalComponent);
		if (!global || !global.workspace) return;
		if (!this._ready) {
			this._setup(global);
			this._ready = true;
		}
		if (this.orbit.enabled) this.orbit.update();
	}
}

DesktopSystem.queries = {
	global: { required: [GlobalComponent] },
};
