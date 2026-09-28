/**
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

import { GlobalComponent, textureLoader } from './global';
import {
	Mesh,
	MeshBasicMaterial,
	Object3D,
	PlaneGeometry,
	SRGBColorSpace,
} from 'three';

import { FollowComponent } from './follow';
import { PlayerComponent } from './player';
import { System } from 'elics';

export class WelcomeSystem extends System {
	init() {
		this._welcomePanel = null;
	}

	update() {
		const global = this.getEntities(this.queries.global)[0].getComponent(
			GlobalComponent,
		);

		const player = this.getEntities(this.queries.player)[0].getComponent(
			PlayerComponent,
		);

		const panther = global.panther;
		if (!panther) return;

		// Only dock the Panther to a head-following welcome card while actually
		// in an XR session; on desktop it should just sit where panther.js put it.
		if (!this._welcomePanel && global.renderer.xr.isPresenting) {
			const geometry = new PlaneGeometry(0.5, 0.12);
			const material = new MeshBasicMaterial({
				transparent: true,
			});
			textureLoader.load('assets/intro_panel.png', (texture) => {
				texture.colorSpace = SRGBColorSpace;
				material.map = texture;
			});
			const plane = new Mesh(geometry, material);
			this._welcomePanel = plane;
			global.scene.add(this._welcomePanel);
			const uiAnchor = new Object3D();
			uiAnchor.position.set(0, 0, -0.5);
			player.head.add(uiAnchor);

			this.world.createEntity().addComponent(FollowComponent, {
				object3D: this._welcomePanel,
				followDistanceThreshold: 0.1,
				positionTarget: uiAnchor,
				lookatTarget: player.head,
			});

			// Dock the real, grabbable Panther root itself (not a copy) so that
			// what you see is exactly what the grab system operates on. Grabbing
			// it will naturally pull it out of this panel via GrabSystem's own
			// Object3D.attach(), which works regardless of current parent.
			panther.root.position.set(0, -0.22, 0.05);
			this._welcomePanel.add(panther.root);
		}

		if (this._welcomePanel) {
			// Once the Panther has been grabbed away from the card, hide the
			// (now empty) card instead of leaving a blank plane in view.
			const stillDocked = panther.root.parent === this._welcomePanel;
			this._welcomePanel.visible =
				global.renderer.xr.isPresenting && stillDocked;
		}
	}
}

WelcomeSystem.queries = {
	global: { required: [GlobalComponent] },
	player: { required: [PlayerComponent] },
};
