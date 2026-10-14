/**
 * CIST 1423 - WaterWorks Builder
 * Catalog of water-system parts, built procedurally from three.js primitives (no model files).
 *
 * Every part is a Group whose origin is the part's centre. `ports` are the connection points
 * (local position + outward direction). snap.js lines ports up; all pipe-sized ports share one
 * nominal diameter so anything can connect to anything.
 */

import {
	BoxGeometry,
	Color,
	CylinderGeometry,
	Group,
	Mesh,
	MeshStandardMaterial,
	SphereGeometry,
	TorusGeometry,
	Vector3,
} from 'three';

export const PIPE_R = 0.03; // 6 cm pipe at "model scale"
const FLANGE_R = 0.045;

const mat = (color, metalness = 0.2, roughness = 0.5) =>
	new MeshStandardMaterial({ color: new Color(color), metalness, roughness });

const COLORS = {
	pvc: 0x2f6fd6, // blue C900 PVC water main
	iron: 0x4a4f57, // ductile iron
	brass: 0xb58a3c,
	valveRed: 0xc8342b,
	pumpBlue: 0x1f4e8c,
	motor: 0x3b7a57,
	tank: 0xd9dde2,
	hydrant: 0xd23b2f,
	hydrantCap: 0xf2c230,
	gaugeFace: 0xf5f5f0,
	meter: 0x2d5a88,
};

// A cylinder whose axis runs along X (three's CylinderGeometry runs along Y).
function xCylinder(r, length, material, radial = 24) {
	const m = new Mesh(new CylinderGeometry(r, r, length, radial), material);
	m.rotation.z = Math.PI / 2;
	return m;
}

function yCylinder(r, length, material, radial = 24) {
	return new Mesh(new CylinderGeometry(r, r, length, radial), material);
}

function flangeX(x, material) {
	const f = xCylinder(FLANGE_R, 0.012, material);
	f.position.x = x;
	return f;
}

function flangeY(y, material) {
	const f = yCylinder(FLANGE_R, 0.012, material);
	f.position.y = y;
	return f;
}

const port = (pos, dir) => ({ pos: new Vector3(...pos), dir: new Vector3(...dir).normalize() });

// ---------------------------------------------------------------------------
// Builders: each returns { group, ports }
// ---------------------------------------------------------------------------

function straightPipe(length) {
	return () => {
		const g = new Group();
		const m = mat(COLORS.pvc, 0.05, 0.45);
		g.add(xCylinder(PIPE_R, length, m));
		g.add(flangeX(-length / 2, m), flangeX(length / 2, m));
		return { group: g, ports: [port([-length / 2, 0, 0], [-1, 0, 0]), port([length / 2, 0, 0], [1, 0, 0])] };
	};
}

function elbow() {
	const g = new Group();
	const m = mat(COLORS.pvc, 0.05, 0.45);
	const r = 0.1; // bend radius
	const torus = new Mesh(new TorusGeometry(r, PIPE_R, 16, 24, Math.PI / 2), m);
	// Torus arc runs from (r,0,0) to (0,r,0); shift so the bend's corner is near the origin.
	torus.position.set(-r / 2, -r / 2, 0);
	g.add(torus);
	const a = [r / 2, -r / 2, 0]; // end of arc at angle 0, pipe continues along -Y
	const b = [-r / 2, r / 2, 0]; // end of arc at angle 90deg, pipe continues along -X
	const fa = flangeY(0, m);
	fa.position.set(...a);
	const fb = flangeX(0, m);
	fb.position.set(...b);
	g.add(fa, fb);
	return { group: g, ports: [port(a, [0, -1, 0]), port(b, [-1, 0, 0])] };
}

function tee() {
	const g = new Group();
	const m = mat(COLORS.pvc, 0.05, 0.45);
	const L = 0.3;
	g.add(xCylinder(PIPE_R, L, m));
	const branch = yCylinder(PIPE_R, 0.15, m);
	branch.position.y = 0.075;
	g.add(branch);
	g.add(flangeX(-L / 2, m), flangeX(L / 2, m), flangeY(0.15, m));
	return {
		group: g,
		ports: [port([-L / 2, 0, 0], [-1, 0, 0]), port([L / 2, 0, 0], [1, 0, 0]), port([0, 0.15, 0], [0, 1, 0])],
	};
}

function gateValve() {
	const g = new Group();
	const iron = mat(COLORS.iron, 0.6, 0.4);
	const L = 0.24;
	g.add(xCylinder(PIPE_R, L, iron));
	g.add(flangeX(-L / 2, iron), flangeX(L / 2, iron));
	const body = new Mesh(new BoxGeometry(0.09, 0.12, 0.08), iron);
	body.position.y = 0.03;
	g.add(body);
	const stem = yCylinder(0.006, 0.1, mat(COLORS.brass, 0.8, 0.3));
	stem.position.y = 0.14;
	g.add(stem);
	const wheel = new Mesh(new TorusGeometry(0.045, 0.008, 8, 24), mat(COLORS.valveRed, 0.3, 0.5));
	wheel.rotation.x = Math.PI / 2;
	wheel.position.y = 0.19;
	wheel.name = 'handwheel';
	g.add(wheel);
	return { group: g, ports: [port([-L / 2, 0, 0], [-1, 0, 0]), port([L / 2, 0, 0], [1, 0, 0])] };
}

function checkValve() {
	const g = new Group();
	const brass = mat(COLORS.brass, 0.8, 0.35);
	const L = 0.2;
	g.add(xCylinder(PIPE_R, L, brass));
	g.add(flangeX(-L / 2, brass), flangeX(L / 2, brass));
	const body = xCylinder(0.05, 0.1, brass);
	g.add(body);
	// Flow arrow on top: water may only go +X.
	const arrowMat = mat(0xffffff, 0, 0.6);
	const shaft = new Mesh(new BoxGeometry(0.06, 0.006, 0.012), arrowMat);
	shaft.position.set(-0.01, 0.052, 0);
	const head = new Mesh(new CylinderGeometry(0, 0.014, 0.025, 3), arrowMat);
	head.rotation.z = -Math.PI / 2;
	head.position.set(0.03, 0.052, 0);
	g.add(shaft, head);
	return { group: g, ports: [port([-L / 2, 0, 0], [-1, 0, 0]), port([L / 2, 0, 0], [1, 0, 0])] };
}

function pump() {
	// End-suction centrifugal pump: suction enters on the impeller axis (-X), discharge leaves the
	// volute straight up (+Y). Motor sits behind it on a shared base plate.
	const g = new Group();
	const base = new Mesh(new BoxGeometry(0.5, 0.02, 0.18), mat(COLORS.iron, 0.5, 0.5));
	base.position.y = -0.11;
	g.add(base);
	const volute = xCylinder(0.09, 0.07, mat(COLORS.pumpBlue, 0.4, 0.45));
	volute.position.set(-0.08, 0, 0);
	g.add(volute);
	const motor = xCylinder(0.07, 0.2, mat(COLORS.motor, 0.3, 0.5));
	motor.position.set(0.11, 0, 0);
	g.add(motor);
	for (let i = 0; i < 6; i++) {
		// cooling fins
		const fin = xCylinder(0.074, 0.006, mat(COLORS.motor, 0.3, 0.5));
		fin.position.set(0.04 + i * 0.03, 0, 0);
		g.add(fin);
	}
	const suction = xCylinder(PIPE_R, 0.08, mat(COLORS.pumpBlue, 0.4, 0.45));
	suction.position.set(-0.155, 0, 0);
	g.add(suction, flangeX(-0.195, mat(COLORS.pumpBlue, 0.4, 0.45)));
	const discharge = yCylinder(PIPE_R, 0.06, mat(COLORS.pumpBlue, 0.4, 0.45));
	discharge.position.set(-0.08, 0.12, 0);
	const df = flangeY(0, mat(COLORS.pumpBlue, 0.4, 0.45));
	df.position.set(-0.08, 0.15, 0);
	g.add(discharge, df);
	for (const z of [-0.07, 0.07]) {
		const foot = new Mesh(new BoxGeometry(0.03, 0.08, 0.02), mat(COLORS.iron, 0.5, 0.5));
		foot.position.set(0.11, -0.065, z);
		g.add(foot);
	}
	return { group: g, ports: [port([-0.195, 0, 0], [-1, 0, 0]), port([-0.08, 0.15, 0], [0, 1, 0])] };
}

function tank() {
	const g = new Group();
	const shell = mat(COLORS.tank, 0.5, 0.35);
	const body = yCylinder(0.18, 0.5, shell, 32);
	g.add(body);
	const dome = new Mesh(new SphereGeometry(0.18, 32, 12, 0, Math.PI * 2, 0, Math.PI / 2), shell);
	dome.position.y = 0.25;
	g.add(dome);
	for (let i = 0; i < 4; i++) {
		const leg = yCylinder(0.012, 0.12, mat(COLORS.iron, 0.6, 0.4), 8);
		const a = (i / 4) * Math.PI * 2 + Math.PI / 4;
		leg.position.set(Math.cos(a) * 0.14, -0.31, Math.sin(a) * 0.14);
		g.add(leg);
	}
	const inlet = yCylinder(PIPE_R, 0.06, shell);
	inlet.position.y = 0.44;
	g.add(inlet, flangeY(0.47, shell));
	const outlet = xCylinder(PIPE_R, 0.06, shell);
	outlet.position.set(0.21, -0.18, 0);
	const of = flangeX(0.24, shell);
	of.position.y = -0.18;
	g.add(outlet, of);
	return { group: g, ports: [port([0, 0.47, 0], [0, 1, 0]), port([0.24, -0.18, 0], [1, 0, 0])] };
}

function hydrant() {
	const g = new Group();
	const red = mat(COLORS.hydrant, 0.3, 0.45);
	const cap = mat(COLORS.hydrantCap, 0.3, 0.45);
	const barrel = yCylinder(0.05, 0.3, red);
	barrel.position.y = 0.05;
	g.add(barrel);
	const bonnet = new Mesh(new SphereGeometry(0.055, 24, 12, 0, Math.PI * 2, 0, Math.PI / 2), cap);
	bonnet.position.y = 0.2;
	g.add(bonnet);
	const nut = yCylinder(0.012, 0.03, cap, 5);
	nut.position.y = 0.26;
	g.add(nut);
	for (const s of [-1, 1]) {
		const nozzle = xCylinder(0.02, 0.05, red);
		nozzle.position.set(s * 0.065, 0.1, 0);
		const nozzleCap = xCylinder(0.024, 0.012, cap);
		nozzleCap.position.set(s * 0.09, 0.1, 0);
		g.add(nozzle, nozzleCap);
	}
	const shoe = yCylinder(PIPE_R, 0.06, red);
	shoe.position.y = -0.13;
	g.add(shoe, flangeY(-0.16, red));
	return { group: g, ports: [port([0, -0.16, 0], [0, -1, 0])] };
}

function meter() {
	const g = new Group();
	const bronze = mat(COLORS.brass, 0.7, 0.4);
	const L = 0.22;
	g.add(xCylinder(PIPE_R, L, bronze));
	g.add(flangeX(-L / 2, bronze), flangeX(L / 2, bronze));
	const body = new Mesh(new BoxGeometry(0.1, 0.06, 0.08), bronze);
	g.add(body);
	const register = yCylinder(0.04, 0.03, mat(COLORS.meter, 0.2, 0.5));
	register.position.y = 0.045;
	g.add(register);
	const face = yCylinder(0.034, 0.004, mat(COLORS.gaugeFace, 0, 0.3));
	face.position.y = 0.062;
	g.add(face);
	return { group: g, ports: [port([-L / 2, 0, 0], [-1, 0, 0]), port([L / 2, 0, 0], [1, 0, 0])] };
}

function gauge() {
	const g = new Group();
	const brass = mat(COLORS.brass, 0.8, 0.35);
	const stem = yCylinder(0.012, 0.08, brass);
	g.add(stem);
	const socket = yCylinder(PIPE_R, 0.03, brass);
	socket.position.y = -0.055;
	g.add(socket, flangeY(-0.07, brass));
	const dial = new Mesh(new CylinderGeometry(0.05, 0.05, 0.025, 32), mat(0x222222, 0.6, 0.3));
	dial.rotation.x = Math.PI / 2;
	dial.position.y = 0.09;
	g.add(dial);
	const face = new Mesh(new CylinderGeometry(0.044, 0.044, 0.004, 32), mat(COLORS.gaugeFace, 0, 0.3));
	face.rotation.x = Math.PI / 2;
	face.position.set(0, 0.09, 0.013);
	g.add(face);
	const needle = new Mesh(new BoxGeometry(0.035, 0.003, 0.002), mat(COLORS.valveRed, 0, 0.5));
	needle.position.set(0.01, 0.095, 0.016);
	needle.rotation.z = 0.5;
	needle.name = 'needle';
	g.add(needle);
	return { group: g, ports: [port([0, -0.07, 0], [0, -1, 0])] };
}

function strainer() {
	// Y-strainer: a screen in the angled leg catches grit before it reaches the pump or meter.
	const g = new Group();
	const iron = mat(COLORS.iron, 0.6, 0.4);
	const L = 0.22;
	g.add(xCylinder(PIPE_R, L, iron));
	g.add(flangeX(-L / 2, iron), flangeX(L / 2, iron));
	const leg = yCylinder(0.035, 0.12, iron);
	leg.rotation.z = Math.PI / 4;
	leg.position.set(0.03, -0.045, 0);
	g.add(leg);
	const cap = yCylinder(0.04, 0.015, mat(COLORS.brass, 0.8, 0.35));
	cap.rotation.z = Math.PI / 4;
	cap.position.set(0.075, -0.09, 0);
	g.add(cap);
	return { group: g, ports: [port([-L / 2, 0, 0], [-1, 0, 0]), port([L / 2, 0, 0], [1, 0, 0])] };
}

// ---------------------------------------------------------------------------
// Catalog
// ---------------------------------------------------------------------------

export const PART_TYPES = {
	pipe: { name: 'Pipe (40 cm)', category: 'Pipe & fittings', build: straightPipe(0.4), info: 'Blue C900 PVC water main. Two flanged ends.' },
	pipe_long: { name: 'Pipe (80 cm)', category: 'Pipe & fittings', build: straightPipe(0.8), info: 'A longer run of water main.' },
	elbow: { name: '90° Elbow', category: 'Pipe & fittings', build: elbow, info: 'Turns the flow 90 degrees. Every bend adds friction loss.' },
	tee: { name: 'Tee', category: 'Pipe & fittings', build: tee, info: 'Splits one line into two. Used to branch off to a hydrant or gauge.' },
	gate_valve: { name: 'Gate Valve', category: 'Valves', build: gateValve, info: 'Fully open or fully closed isolation valve. Turn the handwheel to shut off a section for repair.', stateful: true },
	check_valve: { name: 'Check Valve', category: 'Valves', build: checkValve, info: 'One-way valve: water may only flow in the arrow direction, so it cannot drain back through the pump.' },
	pump: { name: 'Centrifugal Pump', category: 'Equipment', build: pump, info: 'The impeller spins and flings water outward. Suction enters on the axis, discharge leaves the volute.' },
	tank: { name: 'Storage Tank', category: 'Equipment', build: tank, info: 'Stores treated water and provides pressure (head) and reserve for peak demand and fire flow.' },
	strainer: { name: 'Y-Strainer', category: 'Equipment', build: strainer, info: 'A screen in the angled leg catches grit before it reaches the pump or meter.' },
	meter: { name: 'Water Meter', category: 'Instruments', build: meter, info: 'Measures volume passing through, for billing and for finding leaks.' },
	gauge: { name: 'Pressure Gauge', category: 'Instruments', build: gauge, info: 'Reads line pressure (psi). Mount it on a tee branch.' },
	hydrant: { name: 'Fire Hydrant', category: 'Equipment', build: hydrant, info: 'Dry-barrel hydrant. Connects from below to a branch off the main.' },
};

export const PART_ORDER = Object.keys(PART_TYPES);

/**
 * Build a part's visuals. Returns a Group with userData:
 *   type, ports (local), radius (bounding-sphere radius, used for grab distance)
 */
export function createPartObject(type) {
	const def = PART_TYPES[type];
	if (!def) throw new Error(`Unknown part type: ${type}`);
	const { group, ports } = def.build();
	group.name = type;
	group.userData.type = type;
	group.userData.ports = ports;
	let radius = 0;
	group.traverse((child) => {
		if (child.isMesh) {
			child.geometry.computeBoundingSphere();
			const s = child.geometry.boundingSphere;
			radius = Math.max(radius, child.position.length() + s.radius);
		}
	});
	group.userData.radius = radius;
	return group;
}
