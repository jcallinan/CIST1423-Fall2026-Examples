/**
 * University of Pittsburgh at Bradford - Panther Showcase
 * Sizing system for interactive scaling in WebXR and 2D UI
 */

import {
	Mesh,
	MeshBasicMaterial,
	PlaneGeometry,
	RingGeometry,
	SRGBColorSpace,
	TextureLoader,
	Vector3,
} from 'three';
import { PANTHER_SIZES } from './constants';
import { GlobalComponent } from './global';
import { GrabComponent } from './grab';
import { POINTER_MODE, PlayerComponent } from './player';
import { SoundEffectComponent } from './audio';
import { System } from 'elics';
import { Text } from 'troika-three-text';

const SIZING_MODE_ACTIVATION_Y_THRESHOLD = 0.5;

export class PantherSizingSystem extends System {
	init() {
		this._currentScaleIndex = 1; // Tabletop
		this._scaleValue = PANTHER_SIZES[1].scale;
		this._vec3 = new Vector3();
		this._needsUpdate = false;
	}

	setScaleIndex(index) {
		if (index >= 0 && index < PANTHER_SIZES.length) {
			this._currentScaleIndex = index;
			this._scaleValue = PANTHER_SIZES[index].scale;
			this._needsUpdate = true;
		}
	}

	setScaleValue(val) {
		this._scaleValue = Math.max(0.04, Math.min(2.0, val));
		this._needsUpdate = true;
	}

	update(delta) {
		const global = this.getEntities(this.queries.global)[0]?.getComponent(
			GlobalComponent,
		);
		const player = this.getEntities(this.queries.player)[0]?.getComponent(
			PlayerComponent,
		);

		if (!global || !global.panther) return;
		const panther = global.panther;
		const object = panther.root;

		// Check if 2D UI changed scale
		const sizeContainer = document.querySelector('.size-options');
		if (sizeContainer && sizeContainer.dataset.value) {
			const selectedId = sizeContainer.dataset.value;
			const foundIdx = PANTHER_SIZES.findIndex((s) => s.id === selectedId);
			if (foundIdx !== -1 && foundIdx !== this._currentScaleIndex) {
				this._currentScaleIndex = foundIdx;
				this._scaleValue = PANTHER_SIZES[foundIdx].scale;
				this._needsUpdate = true;
			}
		}

		// Also check scale slider if present
		const scaleSlider = document.getElementById('panther-scale-slider');
		if (scaleSlider && scaleSlider.dataset.userAdjusted === 'true') {
			scaleSlider.dataset.userAdjusted = 'false';
			this.setScaleValue(parseFloat(scaleSlider.value));
		}

		// Set up 3D in-XR sizing indicator panel if not present
		if (!object.userData.sizingPanel) {
			const panelGeom = new PlaneGeometry(0.35, 0.2);
			panelGeom.rotateX(-Math.PI / 2);
			const panelMat = new MeshBasicMaterial({
				color: 0x003594,
				transparent: true,
				opacity: 0.85,
			});
			const panel = new Mesh(panelGeom, panelMat);
			panel.position.set(0, 0.01, 0.4);
			object.add(panel);
			object.userData.sizingPanel = panel;

			const sizeLabel = new Text();
			sizeLabel.text = PANTHER_SIZES[this._currentScaleIndex].heightDisplay;
			sizeLabel.fontSize = 0.04;
			sizeLabel.rotateX(-Math.PI / 2);
			sizeLabel.anchorX = 'center';
			sizeLabel.anchorY = 'middle';
			sizeLabel.position.set(0, 0.002, 0);
			sizeLabel.color = 0xffb81c; // Pitt Gold
			panel.add(sizeLabel);
			object.userData.sizeText = sizeLabel;
			sizeLabel.sync();
		}

		// Update 3D model scale if needed
		if (this._needsUpdate) {
			this._needsUpdate = false;
			panther.setScale(this._scaleValue);

			if (object.userData.sizeText) {
				const currentPreset = PANTHER_SIZES[this._currentScaleIndex];
				const labelText = currentPreset
					? `${currentPreset.name} (${currentPreset.heightDisplay})`
					: `Scale: ${(this._scaleValue * 100).toFixed(0)}%`;
				object.userData.sizeText.text = labelText;
				object.userData.sizeText.sync();
			}

			// Update 2D slider and readouts if present
			if (scaleSlider) {
				scaleSlider.value = this._scaleValue;
			}
			const scaleValueDisplay = document.getElementById('scale-value-display');
			if (scaleValueDisplay) {
				scaleValueDisplay.innerText = `${(this._scaleValue * 100).toFixed(0)}% (${(this._scaleValue * 2.7).toFixed(1)}m)`;
			}
		}

		// XR Controller Pointer interaction for scaling if in VR
		if (player && player.controllers) {
			Object.values(player.controllers).forEach((controllerObject) => {
				if (controllerObject.justStartedSelecting) {
					// Controller trigger click
				}
			});
		}
	}
}

PantherSizingSystem.queries = {
	global: { required: [GlobalComponent] },
	player: { required: [PlayerComponent] },
	grabbables: { required: [GrabComponent] },
};
