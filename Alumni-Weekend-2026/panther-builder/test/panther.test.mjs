import assert from 'node:assert/strict';
import test from 'node:test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { PANTHER_FINISHES, PANTHER_SIZES, DEFAULT_SCALE_INDEX } from '../src/constants.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

test('Panther finishes are properly defined', () => {
	assert.ok(PANTHER_FINISHES.length >= 6, 'Should have at least 6 finishes');
	const ids = PANTHER_FINISHES.map(f => f.id);
	assert.ok(ids.includes('scan'), 'Must include original photogrammetry scan');
	assert.ok(ids.includes('pitt_blue'), 'Must include Pitt Royal Blue');
	assert.ok(ids.includes('pitt_gold'), 'Must include Pitt Athletic Gold');
	assert.ok(ids.includes('bronze'), 'Must include Classic Cast Bronze');
	assert.ok(ids.includes('marble'), 'Must include White Marble');
	assert.ok(ids.includes('onyx'), 'Must include Bradford Onyx');
});

test('Panther sizing presets have ascending scales and valid display strings', () => {
	assert.equal(PANTHER_SIZES.length, 5, 'Should have 5 sizing presets');
	
	// Presets must be ordered from smallest to largest
	for (let i = 0; i < PANTHER_SIZES.length - 1; i++) {
		assert.ok(
			PANTHER_SIZES[i].scale < PANTHER_SIZES[i + 1].scale,
			`Scale at index ${i} should be strictly less than index ${i + 1}`
		);
	}

	assert.equal(DEFAULT_SCALE_INDEX, 1, 'Default scale should be Tabletop');
	assert.equal(PANTHER_SIZES[1].id, 'tabletop');
	assert.ok(PANTHER_SIZES[3].scale === 1.0, 'Life-size statue must have 1.0 (100%) scale');
});

test('panther.glb asset exists and has valid glTF binary header', () => {
	const glbPath = path.resolve(__dirname, '../src/assets/gltf/panther.glb');
	assert.ok(fs.existsSync(glbPath), 'panther.glb must exist in src/assets/gltf/');

	const buf = fs.readFileSync(glbPath);
	assert.ok(buf.length > 1000000, 'panther.glb should be a substantial binary asset (>1MB)');

	// Check glTF binary magic: 0x46546C67 ('glTF')
	const magic = buf.toString('ascii', 0, 4);
	assert.equal(magic, 'glTF', 'Magic header must equal glTF');
	
	const version = buf.readUInt32LE(4);
	assert.equal(version, 2, 'glTF version must be 2');
});
