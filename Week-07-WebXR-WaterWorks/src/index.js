/**
 * CIST 1423 - WaterWorks Builder
 * A multiplayer WebXR sandbox for laying out a small water system (pumps, pipes, valves, tanks...).
 * Forked from panther-builder, which is forked from Meta's Sneaker Builder: same ECS engine layer
 * (player / pointer / follow / audio / global / scene), new product layer.
 */

import './styles/index.css';

import { AudioSystem, SoundEffectComponent } from './audio';
import { CircleGeometry, Clock, GridHelper, Mesh, MeshStandardMaterial, BufferGeometry } from 'three';
import { FollowComponent, FollowSystem } from './follow';
import { GrabComponent, GrabSystem } from './grab';
import { NetClient, NetworkSystem } from './network';
import { PlayerComponent, PlayerSystem } from './player';
import { acceleratedRaycast, computeBoundsTree, disposeBoundsTree } from 'three-mesh-bvh';

import { AvatarSystem } from './avatars';
import { DesktopSystem } from './desktop';
import { GlobalComponent } from './global';
import { PointerSystem } from './pointer';
import { ShelfSystem } from './shelf';
import { Workspace } from './workspace';
import { World } from 'elics';
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
	.registerSystem(PlayerSystem) // 1st: controllers -> justStartedSelecting etc.
	.registerSystem(GrabSystem) // proximity grab / release / snap (marks nearGrabbable)
	.registerSystem(ShelfSystem) // ray-click on shelf + valve wheels (skips hands near a part)
	.registerSystem(DesktopSystem) // mouse + HTML page
	.registerSystem(NetworkSystem) // smooth remote parts, send my pose + carried parts
	.registerSystem(AvatarSystem) // draw other people
	.registerSystem(FollowSystem)
	.registerSystem(AudioSystem)
	.registerSystem(PointerSystem); // last: draw claw / ray in whatever mode was chosen

const { scene, camera, renderer } = setupScene();

// A floor for VR and desktop. Hidden in passthrough MR, where your real floor is the floor.
const floor = new Mesh(new CircleGeometry(4, 48), new MeshStandardMaterial({ color: 0x1d2733, roughness: 0.95 }));
floor.rotation.x = -Math.PI / 2;
const grid = new GridHelper(8, 32, 0x3d5a73, 0x2a3a4a);
grid.position.y = 0.001;
scene.add(floor, grid);
renderer.xr.addEventListener('sessionstart', () => {
	const passthrough = renderer.xr.getSession()?.environmentBlendMode !== 'opaque';
	floor.visible = grid.visible = !passthrough;
});
renderer.xr.addEventListener('sessionend', () => {
	floor.visible = grid.visible = true;
});

const workspace = new Workspace(world, scene);
const net = new NetClient(workspace);

world.createEntity().addComponent(GlobalComponent, { renderer, camera, scene, workspace, net });

// Handy in the browser console while debugging: waterworks.workspace.parts, waterworks.net.peers ...
window.waterworks = { world, workspace, net, camera, renderer, scene };

const clock = new Clock();
renderer.setAnimationLoop(() => {
	world.update(clock.getDelta(), clock.elapsedTime);
	renderer.render(scene, camera);
});
