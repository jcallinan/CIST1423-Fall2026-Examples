/**
 * CIST 1423 - WaterWorks Builder
 * The in-headset parts shelf (XR only).
 *
 *  - A board of miniature parts appears in front of you when the session starts
 *    (press A / X to call it back in front of you).
 *  - Reach into a miniature and squeeze the trigger: you pull out a full-size part (grab.js).
 *  - Or point at a miniature from further away and click: the part appears in front of the shelf.
 *  - Point at a gate valve's red handwheel and click to open / close it (synced to everyone).
 *  - Drop a part into the red recycle bin to delete it.
 */

import { Box3, BoxGeometry, Group, Mesh, MeshStandardMaterial, Quaternion, Vector3 } from 'three';
import { PART_ORDER, PART_TYPES, createPartObject } from './parts';
import { POINTER_MODE, PlayerComponent } from './player';

import { GlobalComponent } from './global';
import { GrabComponent } from './grab';
import { SoundEffectComponent } from './audio';
import { System } from 'elics';
import { Text } from 'troika-three-text';

const COLS = 4;
const CELL = 0.17;
const MINI = 0.11; // miniature size (m)

export class ShelfSystem extends System {
	init() {
		this.root = null;
		this.minis = [];
		this._bin = null;
		this._binBox = new Box3();
		this._v = new Vector3();
	}

	_build(global) {
		const root = new Group();
		root.name = 'PartsShelf';
		const rows = Math.ceil(PART_ORDER.length / COLS);
		const width = COLS * CELL + 0.06;
		const height = rows * CELL + 0.1;

		const board = new Mesh(new BoxGeometry(width, height, 0.02), new MeshStandardMaterial({ color: 0x0e2841, roughness: 0.8, transparent: true, opacity: 0.85 }));
		board.position.set(0, height / 2, -0.03);
		root.add(board);
		const title = new Text();
		title.text = 'WaterWorks parts: reach in and squeeze';
		title.fontSize = 0.025;
		title.color = 0xffb81c;
		title.anchorX = 'center';
		title.position.set(0, height + 0.04, -0.02);
		title.sync();
		root.add(title);

		PART_ORDER.forEach((type, i) => {
			const col = i % COLS;
			const row = Math.floor(i / COLS);
			const x = (col - (COLS - 1) / 2) * CELL;
			const y = height - 0.05 - (row + 0.5) * CELL + 0.02;
			const mini = createPartObject(type);
			const scale = MINI / (2 * mini.userData.radius);
			mini.scale.setScalar(scale);
			mini.position.set(x, y, 0.02);
			mini.rotation.y = -0.4; // three-quarter view
			root.add(mini);
			const label = new Text();
			label.text = PART_TYPES[type].name;
			label.fontSize = 0.016;
			label.color = 0xffffff;
			label.anchorX = 'center';
			label.anchorY = 'top';
			label.position.set(x, y - MINI / 2 - 0.005, 0.0);
			label.sync();
			root.add(label);
			mini.userData.spawnerType = type;
			this.world.createEntity().addComponent(GrabComponent, { object3D: mini, spawnerType: type });
			this.minis.push(mini);
		});

		// Recycle bin, to the right of the board at waist height.
		const bin = new Group();
		const red = new MeshStandardMaterial({ color: 0xc8342b, roughness: 0.6 });
		const w = 0.22;
		const walls = [
			[w, 0.02, w, 0, -0.1, 0],
			[w, 0.2, 0.01, 0, 0, w / 2],
			[w, 0.2, 0.01, 0, 0, -w / 2],
			[0.01, 0.2, w, w / 2, 0, 0],
			[0.01, 0.2, w, -w / 2, 0, 0],
		];
		for (const [bx, by, bz, px, py, pz] of walls) {
			const m = new Mesh(new BoxGeometry(bx, by, bz), red);
			m.position.set(px, py, pz);
			bin.add(m);
		}
		const binLabel = new Text();
		binLabel.text = 'Recycle';
		binLabel.fontSize = 0.03;
		binLabel.color = 0xffffff;
		binLabel.anchorX = 'center';
		binLabel.position.set(0, 0.15, w / 2 + 0.01);
		binLabel.sync();
		bin.add(binLabel);
		bin.position.set(width / 2 + 0.2, -0.25, 0.1);
		root.add(bin);
		this._bin = bin;

		global.scene.add(root);
		this.root = root;
		global.shelf = this;
	}

	/** Put the shelf ~0.6 m in front of the head, a little below eye level, facing you. */
	place(head) {
		const forward = new Vector3(0, 0, -1).applyQuaternion(head.quaternion);
		forward.y = 0;
		forward.normalize();
		this.root.position.copy(head.position).addScaledVector(forward, 0.6);
		this.root.position.y = Math.max(0.6, head.position.y - 0.55);
		this.root.lookAt(head.position.x, this.root.position.y, head.position.z);
		this.root.updateMatrixWorld(true);
	}

	isInRecycleBin(worldPos) {
		if (!this._bin || !this.root.visible) return false;
		this._binBox.setFromObject(this._bin).expandByScalar(0.05);
		return this._binBox.containsPoint(worldPos);
	}

	update() {
		const global = this.getEntities(this.queries.global)[0].getComponent(GlobalComponent);
		const player = this.getEntities(this.queries.player)[0]?.getComponent(PlayerComponent);
		const { renderer, workspace, net } = global;
		const inXR = renderer.xr.isPresenting;

		if (!this.root) this._build(global);
		// ?shelf in the URL shows the shelf on desktop too (handy for screenshots and teaching).
		const preview = !inXR && new URLSearchParams(location.search).has('shelf');
		this.root.visible = inXR || preview;
		if (preview && !this._previewPlaced) {
			this.root.position.set(-0.9, 0.55, -0.3);
			this.root.rotation.y = 0.5;
			this._previewPlaced = true;
		}
		if (!inXR || !player) {
			this._placed = false;
			return;
		}
		if (!this._placed && player.head.position.y > 0.3) {
			this.place(player.head);
			this._placed = true;
		}

		const valveWheels = [...workspace.parts.values()]
			.filter((p) => p.type === 'gate_valve' && !p.heldBy)
			.map((p) => p.object3D.getObjectByName('handwheel'));

		for (const c of Object.values(player.controllers)) {
			if (c.gamepadWrapper.getButtonDownByIndex(4)) this.place(player.head); // A / X: summon shelf
			if (c.attached || c.nearGrabbable) continue;
			const hit = c.raycaster.intersectObjects([...this.minis, ...valveWheels], true)[0];
			if (!hit) continue;
			c.pointerMode = POINTER_MODE.Ray;
			c.intersectDistance = hit.distance;
			if (!c.justStartedSelecting) continue;

			let node = hit.object;
			while (node && !node.userData.spawnerType && node.name !== 'handwheel') node = node.parent;
			if (!node) continue;
			if (node.name === 'handwheel') {
				let partObject = node;
				while (partObject && !partObject.userData.partId) partObject = partObject.parent;
				const part = workspace.parts.get(partObject.userData.partId);
				workspace.toggleValve(part);
				net.state(part);
				this.world.createEntity().addComponent(SoundEffectComponent, { type: 'confirm', sourceObject: node });
			} else {
				// Spawn in front of the shelf, level, at the miniature's height.
				const at = node.getWorldPosition(new Vector3());
				const out = new Vector3(0, 0, 1).applyQuaternion(this.root.getWorldQuaternion(new Quaternion()));
				at.addScaledVector(out, 0.35);
				const part = workspace.add(node.userData.spawnerType, { p: at.toArray(), q: this.root.getWorldQuaternion(new Quaternion()).toArray() });
				net.spawn(part);
				this.world.createEntity().addComponent(SoundEffectComponent, { type: 'maximize', sourceObject: part.object3D });
			}
			try {
				c.gamepadWrapper.getHapticActuator(0)?.pulse(0.3, 60);
			} catch {
				/* no haptics */
			}
		}
	}
}

ShelfSystem.queries = {
	global: { required: [GlobalComponent] },
	player: { required: [PlayerComponent] },
};
