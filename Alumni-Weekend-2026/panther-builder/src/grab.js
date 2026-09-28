/**
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

import { Component, System } from 'elics';

import { GlobalComponent } from './global';
import { PlayerComponent } from './player';
import { Vector3 } from 'three';

export class GrabComponent extends Component {}

export class GrabSystem extends System {
	init() {
		this._vec3 = new Vector3();
		this._vec32 = new Vector3();
		this._vec33 = new Vector3();
	}

	update() {
		const global = this.getEntities(this.queries.global)[0].getComponent(
			GlobalComponent,
		);

		const player = this.getEntities(this.queries.player)[0].getComponent(
			PlayerComponent,
		);

		const attachMap = {};

		Object.values(player.controllers).forEach((controllerObject) => {
			const { targetRaySpace } = controllerObject;
			if (!controllerObject.attached) {
				targetRaySpace.getWorldPosition(this._vec3);
				const intersectObjects = this.getEntities(this.queries.grabbables)
					.filter((entity) => !entity.getComponent(GrabComponent).attached)
					.map((entity) => entity.getComponent(GrabComponent).object3D)
					.sort(
						(a, b) =>
							a.getWorldPosition(this._vec32).distanceTo(this._vec3) -
							b.getWorldPosition(this._vec33).distanceTo(this._vec3),
					);

				if (intersectObjects[0]) {
					const dist = intersectObjects[0]
						.getWorldPosition(this._vec32)
						.distanceTo(this._vec3);
					// The panther's visual scale is applied to its inner mesh, not this
					// grabbed root object (which stays at scale 1 so in-XR UI panels
					// don't scale with it), so read the real displayed scale from it.
					const objScale = global.panther
						? global.panther.currentScale
						: intersectObjects[0].scale.x || 1.0;
					const grabThreshold = Math.max(0.4, objScale * 0.8);
					if (dist < grabThreshold) {
						const key =
							intersectObjects[0].userData.shoeId || 'panther';
						attachMap[key] = controllerObject;
					}
				}
			}
		});

		for (const entity of this.getEntities(this.queries.grabbables)) {
			const grabComponent = entity.getComponent(GrabComponent);
			grabComponent.justAttached = false;
			grabComponent.justDetached = false;
			const object = grabComponent.object3D;
			const shoeId = object.userData.shoeId;

			if (!grabComponent.attached) {
				const controllerObject = attachMap[shoeId];
				if (controllerObject && controllerObject.justStartedSelecting) {
					grabComponent.attached = controllerObject;
					grabComponent.justAttached = true;
					controllerObject.targetRaySpace.attach(object);
					controllerObject.attached = true;
				}
			} else {
				const controllerObject = grabComponent.attached;
				if (controllerObject.justStoppedSelecting) {
					global.scene.attach(object);
					grabComponent.attached = null;
					grabComponent.justDetached = true;
					controllerObject.attached = false;
				}
			}
		}
	}
}

GrabSystem.queries = {
	global: { required: [GlobalComponent] },
	player: { required: [PlayerComponent] },
	grabbables: { required: [GrabComponent] },
};
