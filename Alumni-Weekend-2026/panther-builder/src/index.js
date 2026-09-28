/**
 * University of Pittsburgh at Bradford - Panther Showcase
 * WebXR & 3D Interactive Customizer
 */

import './styles/index.css';

import { AudioSystem, SoundEffectComponent } from './audio';
import { BufferGeometry, Clock, Mesh } from 'three';
import { FollowComponent, FollowSystem } from './follow';
import { GlobalComponent, KTX2_LOADER, gltfLoader } from './global';
import { GrabComponent, GrabSystem } from './grab';
import { PlayerComponent, PlayerSystem } from './player';
import {
	acceleratedRaycast,
	computeBoundsTree,
	disposeBoundsTree,
} from 'three-mesh-bvh';

import { InlineSystem } from './landing';
import { PantherSizingSystem } from './pantherSize';
import { PointerSystem } from './pointer';
import { WelcomeSystem } from './welcome';
import { World } from 'elics';
import { loadPanther } from './panther';
import { setupScene } from './scene';

BufferGeometry.prototype.computeBoundsTree = computeBoundsTree;
BufferGeometry.prototype.disposeBoundsTree = disposeBoundsTree;
Mesh.prototype.raycast = acceleratedRaycast;

const world = new World();
world
	.registerComponent(GlobalComponent)
	.registerComponent(PlayerComponent)
	.registerComponent(SoundEffectComponent)
	.registerComponent(FollowComponent)
	.registerComponent(GrabComponent)
	.registerSystem(PlayerSystem)
	.registerSystem(WelcomeSystem)
	.registerSystem(PantherSizingSystem)
	.registerSystem(InlineSystem)
	.registerSystem(GrabSystem)
	.registerSystem(FollowSystem)
	.registerSystem(AudioSystem)
	.registerSystem(PointerSystem);

const clock = new Clock();

const { scene, camera, renderer } = setupScene();

gltfLoader.setKTX2Loader(KTX2_LOADER.detectSupport(renderer));

const global = world.createEntity().addComponent(GlobalComponent, {
	renderer,
	camera,
	scene,
});

loadPanther(world, global);

renderer.setAnimationLoop(function () {
	const delta = clock.getDelta();
	const time = clock.elapsedTime;
	world.update(delta, time);
	renderer.render(scene, camera);
});
