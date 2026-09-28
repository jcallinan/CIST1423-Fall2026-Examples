/**
 * University of Pittsburgh at Bradford - Panther Showcase
 * 3D Panther model loader and customization logic
 */

import {
	Box3,
	Color,
	Group,
	Mesh,
	MeshStandardMaterial,
	Vector3,
} from 'three';
import { PANTHER_FINISHES, PANTHER_SIZES } from './constants';
import { gltfLoader } from './global';
import { GrabComponent } from './grab';

export class Panther {
	constructor(rootGroup, mainMesh, originalMaterial) {
		this._root = rootGroup;
		this._mesh = mainMesh;
		this._originalMaterial = originalMaterial;
		this._currentFinishId = 'scan';
		this._currentScale = PANTHER_SIZES[1].scale; // Default to tabletop

		// Cache bounding box and dimensions
		const box = new Box3().setFromObject(this._mesh);
		this._dimensions = new Vector3();
		box.getSize(this._dimensions);
	}

	get root() {
		return this._root;
	}

	get mesh() {
		return this._mesh;
	}

	get currentScale() {
		return this._currentScale;
	}

	get currentFinish() {
		return this._currentFinishId;
	}

	setScale(scale) {
		this._currentScale = scale;
		this._mesh.scale.set(scale, scale, scale);
	}

	setFinish(finishId) {
		const finish = PANTHER_FINISHES.find((f) => f.id === finishId);
		if (!finish) return;

		this._currentFinishId = finishId;

		if (finish.type === 'original') {
			this._mesh.material = this._originalMaterial;
			return;
		}

		// PBR material with Pitt colors / statue finishes
		const mat = new MeshStandardMaterial({
			color: new Color(finish.color),
			roughness: finish.roughness !== undefined ? finish.roughness : 0.4,
			metalness: finish.metalness !== undefined ? finish.metalness : 0.2,
		});

		// Retain normal map if available on original
		if (this._originalMaterial.normalMap) {
			mat.normalMap = this._originalMaterial.normalMap;
		}

		this._mesh.material = mat;
	}

	createCopy() {
		const copyGroup = new Group();
		const clonedMesh = this._mesh.clone();
		clonedMesh.material = this._mesh.material.clone();
		copyGroup.add(clonedMesh);
		copyGroup.userData.pantherInstance = this;
		return copyGroup;
	}
}

export const loadPanther = (world, global, onLoaded) => {
	gltfLoader.load('assets/gltf/panther.glb', (gltf) => {
		const model = gltf.scene;

		let mainMesh = null;
		let originalMaterial = null;

		model.traverse((node) => {
			if (node.isMesh && !mainMesh) {
				mainMesh = node;
				originalMaterial = node.material.clone();
				if (node.geometry) {
					node.geometry.computeBoundsTree();
					node.geometry.computeVertexNormals();
				}
			}
		});

		if (!mainMesh) {
			console.error('Failed to find main mesh in panther.glb');
			return;
		}

		// Compute bounding box to normalize pivot:
		// Center X and Z around 0, and align the base at Y = 0
		const box = new Box3().setFromObject(mainMesh);
		const center = new Vector3();
		box.getCenter(center);
		const size = new Vector3();
		box.getSize(size);

		mainMesh.position.x = -center.x;
		mainMesh.position.z = -center.z;
		mainMesh.position.y = -box.min.y; // Sits nicely on floor/table

		// Wrap in a Panther root group
		const pantherRoot = new Group();
		pantherRoot.add(mainMesh);
		pantherRoot.userData.shoeId = 'panther'; // Keep compatibility with GrabSystem
		pantherRoot.name = 'PittBradfordPanther';

		const pantherInstance = new Panther(pantherRoot, mainMesh, originalMaterial);
		pantherInstance.setScale(PANTHER_SIZES[1].scale); // Tabletop default
		pantherRoot.position.set(0, 0.8, -0.6); // Comfortable viewing position in VR

		global.panther = pantherInstance;

		pantherInstance.grabComponent = world
			.createEntity()
			.addComponent(GrabComponent, { object3D: pantherRoot });

		global.scene.add(pantherRoot);

		if (onLoaded) {
			onLoaded(pantherInstance);
		}
	});
};
