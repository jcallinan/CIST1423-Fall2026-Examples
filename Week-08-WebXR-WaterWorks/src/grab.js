/**
 * CIST 1423 - WaterWorks Builder
 * Proximity grab for any number of parts (generalised from panther-builder's single-statue grab).
 *
 *  - Reach a controller into a part and pull the trigger to pick it up (re-parent to the controller).
 *  - Reach into a shelf miniature and pull the trigger to pull out a brand-new full-size part.
 *  - Let go: the part snaps to a nearby open port, or is deleted if dropped in the recycle bin.
 *  - B / Y while holding deletes the part.
 *  - Parts someone else is holding can't be grabbed (the server enforces this too).
 */

import { Component, System } from 'elics';
import { Quaternion, Vector3 } from 'three';

import { GlobalComponent } from './global';
import { PlayerComponent } from './player';
import { SoundEffectComponent } from './audio';

export class GrabComponent extends Component {}

const GRAB_MARGIN = 0.05;

export class GrabSystem extends System {
	init() {
		this._vec3 = new Vector3();
		this._vec32 = new Vector3();
		this._hovered = new Map(); // controller -> object3D currently highlighted
	}

	_highlight(object, on) {
		object.traverse((child) => {
			if (child.isMesh && child.material.emissive) child.material.emissive.setHex(on ? 0x333333 : 0x000000);
		});
	}

	_candidate(controllerObject) {
		const hand = controllerObject.targetRaySpace.getWorldPosition(this._vec3);
		let best = null;
		let bestDist = Infinity;
		for (const entity of this.getEntities(this.queries.grabbables)) {
			const grab = entity.getComponent(GrabComponent);
			if (grab.part && grab.part.heldBy) continue; // held by me or someone else
			if (!grab.object3D.visible || !grab.object3D.parent) continue;
			const center = grab.object3D.getWorldPosition(this._vec32);
			const scale = grab.object3D.getWorldScale(new Vector3()).x;
			const reach = (grab.object3D.userData.radius || 0.1) * scale + GRAB_MARGIN;
			const dist = center.distanceTo(hand);
			if (dist < reach && dist < bestDist) {
				best = grab;
				bestDist = dist;
			}
		}
		return best;
	}

	_haptic(controllerObject, intensity, ms) {
		try {
			controllerObject.gamepadWrapper.getHapticActuator(0)?.pulse(intensity, ms);
		} catch {
			/* no haptics on this controller */
		}
	}

	_sound(type, object) {
		this.world.createEntity().addComponent(SoundEffectComponent, { type, sourceObject: object });
	}

	update() {
		const global = this.getEntities(this.queries.global)[0].getComponent(GlobalComponent);
		const player = this.getEntities(this.queries.player)[0]?.getComponent(PlayerComponent);
		const { workspace, net, scene } = global;
		if (!player || !workspace) return;

		for (const controllerObject of Object.values(player.controllers)) {
			controllerObject.nearGrabbable = false;
			const held = controllerObject.heldPart;

			if (held) {
				// Deleted remotely (e.g. "clear all") while in our hand.
				if (!workspace.parts.has(held.id)) {
					controllerObject.heldPart = null;
					controllerObject.attached = false;
					continue;
				}
				// The server said someone else grabbed it first: let go without a release message.
				if (held.forceDrop) {
					held.forceDrop = false;
					scene.attach(held.object3D);
					controllerObject.heldPart = null;
					controllerObject.attached = false;
					this._haptic(controllerObject, 0.8, 150);
					continue;
				}
				// xr-standard button 5 = B / Y on Touch controllers (absent on Vive wands: use the bin).
				const deleteNow = controllerObject.gamepadWrapper.getButtonDownByIndex(5);
				if (controllerObject.justStoppedSelecting || deleteNow) {
					controllerObject.heldPart = null;
					controllerObject.attached = false;
					scene.attach(held.object3D);
					held.heldBy = null;
					const inBin = global.shelf?.isInRecycleBin(held.object3D.getWorldPosition(this._vec3));
					if (deleteNow || inBin) {
						this._sound('minimize', held.object3D);
						workspace.remove(held.id);
						net.remove(held);
						this._haptic(controllerObject, 0.4, 80);
					} else {
						const snapped = workspace.settle(held);
						net.release(held);
						this._sound(snapped ? 'confirm' : 'click', held.object3D);
						this._haptic(controllerObject, snapped ? 0.6 : 0.2, snapped ? 100 : 40);
					}
				}
				continue;
			}

			const candidate = this._candidate(controllerObject);
			const previous = this._hovered.get(controllerObject);
			if (previous && previous !== candidate?.object3D) this._highlight(previous, false);
			if (candidate) {
				this._highlight(candidate.object3D, true);
				this._hovered.set(controllerObject, candidate.object3D);
				controllerObject.nearGrabbable = true;
			} else {
				this._hovered.delete(controllerObject);
			}

			if (!candidate || !controllerObject.justStartedSelecting) continue;
			this._highlight(candidate.object3D, false);

			let part = candidate.part;
			if (candidate.spawnerType) {
				// Pull a new full-size part out of the shelf, already in hand.
				const pose = {
					p: controllerObject.targetRaySpace.getWorldPosition(new Vector3()).toArray(),
					q: candidate.object3D.getWorldQuaternion(new Quaternion()).toArray(),
				};
				part = workspace.add(candidate.spawnerType, pose);
				part.heldBy = 'me';
				net.spawn(part, true);
				this._sound('maximize', part.object3D);
			} else {
				part.heldBy = 'me';
				net.grab(part);
				this._sound('click', part.object3D);
			}
			controllerObject.targetRaySpace.attach(part.object3D);
			controllerObject.heldPart = part;
			controllerObject.attached = true;
			this._haptic(controllerObject, 0.3, 50);
		}
	}
}

GrabSystem.queries = {
	global: { required: [GlobalComponent] },
	player: { required: [PlayerComponent] },
	grabbables: { required: [GrabComponent] },
};
