/**
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

import {
	AmbientLight,
	DirectionalLight,
	PMREMGenerator,
	PerspectiveCamera,
	SRGBColorSpace,
	Scene,
	WebGLRenderer,
} from 'three';

import { exrLoader } from './global';

export const setupScene = () => {
	const scene = new Scene();

	const camera = new PerspectiveCamera(
		70,
		window.innerWidth / window.innerHeight,
		0.01,
		5000,
	);

	const renderer = new WebGLRenderer({ antialias: true, alpha: true });
	renderer.setPixelRatio(window.devicePixelRatio);
	renderer.setSize(window.innerWidth, window.innerHeight);
	renderer.outputColorSpace = SRGBColorSpace;
	renderer.xr.enabled = true;
	document.getElementById('scene-container').appendChild(renderer.domElement);

	const pmremGenerator = new PMREMGenerator(renderer);
	pmremGenerator.compileEquirectangularShader();

	// Add key, fill, and ambient lighting for the Panther statue
	const ambientLight = new AmbientLight(0xffffff, 0.85);
	scene.add(ambientLight);

	const dirLight1 = new DirectionalLight(0xffffff, 1.2);
	dirLight1.position.set(3, 6, 4);
	scene.add(dirLight1);

	const dirLight2 = new DirectionalLight(0xdbeafe, 0.6);
	dirLight2.position.set(-3, 2, -3);
	scene.add(dirLight2);

	exrLoader.load('assets/venice_sunset_1k.exr', (texture) => {
		const envMap = pmremGenerator.fromEquirectangular(texture).texture;
		pmremGenerator.dispose();
		scene.environment = envMap;
	});

	const onResize = () => {
		resizeCanvas(
			document.getElementById('scene-container').offsetWidth,
			document.getElementById('scene-container').offsetHeight,
			camera,
			renderer,
		);
	};

	window.addEventListener('resize', onResize);

	window.addEventListener('load', onResize);

	renderer.xr.addEventListener('sessionstart', () => {
		const session = renderer.xr.getSession();
		if (!session) {
			console.warn('[XR debug] XR session started without a session object');
			return;
		}
		if (typeof session.updateTargetFrameRate !== 'function') {
			console.warn('[XR debug] updateTargetFrameRate unsupported on this XR session', {
				mode: session.mode,
				type: typeof session.updateTargetFrameRate,
			});
			return;
		}
		try {
			session.updateTargetFrameRate(72);
			console.debug('[XR debug] updateTargetFrameRate set to 72', { mode: session.mode });
		} catch (error) {
			console.warn('[XR debug] updateTargetFrameRate rejected by XR browser', {
				error: String(error),
				mode: session.mode,
			});
		}
	});

	document.getElementById('ar-button').addEventListener('click', () => {
		resizeCanvas(window.innerWidth, window.innerHeight, camera, renderer);
	});

	return { scene, camera, renderer };
};

export const resizeCanvas = (width, height, camera, renderer) => {
	camera.aspect = width / height;
	camera.updateProjectionMatrix();
	renderer.setSize(width, height);
};
