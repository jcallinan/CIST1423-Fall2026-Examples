/**
 * CIST 1423 - WaterWorks Builder
 * Draws the other people in the room: a visor-style head with a name tag, and two hands.
 * Desktop users only send a head (their camera), so they show up as a floating head.
 */

import { BoxGeometry, Group, Mesh, MeshStandardMaterial, Quaternion, SphereGeometry, Vector3 } from 'three';

import { GlobalComponent } from './global';
import { System } from 'elics';
import { Text } from 'troika-three-text';

const HEAD = new BoxGeometry(0.18, 0.12, 0.14);
const VISOR = new BoxGeometry(0.16, 0.06, 0.02);
const HAND = new SphereGeometry(0.035, 16, 12);
const STALE_MS = 3000;

function makeAvatar(peer) {
	const color = new MeshStandardMaterial({ color: peer.color, roughness: 0.5 });
	const dark = new MeshStandardMaterial({ color: 0x111111, roughness: 0.2, metalness: 0.5 });
	const head = new Group();
	head.add(new Mesh(HEAD, color));
	const visor = new Mesh(VISOR, dark);
	visor.position.set(0, 0.005, -0.075); // -Z is "forward" for a camera / headset
	head.add(visor);
	const label = new Text();
	label.text = peer.name;
	label.fontSize = 0.05;
	label.anchorX = 'center';
	label.anchorY = 'bottom';
	label.color = peer.color;
	label.outlineWidth = 0.004;
	label.outlineColor = 0x000000;
	label.position.y = 0.1;
	label.sync();
	const left = new Mesh(HAND, color);
	const right = new Mesh(HAND, color);
	const root = new Group();
	root.add(head, label, left, right);
	return { root, head, label, left, right };
}

export class AvatarSystem extends System {
	init() {
		this._avatars = new Map();
		this._v = new Vector3();
		this._q = new Quaternion();
	}

	_apply(object, arr, k) {
		if (!arr) {
			object.visible = false;
			return;
		}
		object.visible = true;
		this._v.fromArray(arr, 0);
		this._q.fromArray(arr, 3);
		object.position.lerp(this._v, k);
		object.quaternion.slerp(this._q, k);
	}

	update(delta) {
		const global = this.getEntities(this.queries.global)[0].getComponent(GlobalComponent);
		const { net, scene, camera, renderer } = global;
		const viewer = renderer.xr.isPresenting ? renderer.xr.getCamera() : camera;

		for (const [id, avatar] of this._avatars) {
			if (!net.peers.has(id)) {
				avatar.root.removeFromParent();
				this._avatars.delete(id);
			}
		}

		const k = Math.min(1, delta * 12);
		const now = performance.now();
		for (const peer of net.peers.values()) {
			let avatar = this._avatars.get(peer.id);
			if (!avatar) {
				avatar = makeAvatar(peer);
				scene.add(avatar.root);
				this._avatars.set(peer.id, avatar);
			}
			const pose = peer.pose;
			avatar.root.visible = !!pose && now - pose.at < STALE_MS;
			if (!avatar.root.visible) continue;
			this._apply(avatar.head, pose.head, k);
			this._apply(avatar.left, pose.left, k);
			this._apply(avatar.right, pose.right, k);
			avatar.label.position.copy(avatar.head.position).y += 0.1;
			avatar.label.quaternion.copy(viewer.getWorldQuaternion(this._q)); // name tag faces me
		}
	}
}

AvatarSystem.queries = {
	global: { required: [GlobalComponent] },
};
