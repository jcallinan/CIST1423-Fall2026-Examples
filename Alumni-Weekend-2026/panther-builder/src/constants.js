/**
 * University of Pittsburgh at Bradford - Panther Showcase
 * Constants, materials, and configuration options
 */

export const PITT_COLORS = {
	ROYAL_BLUE: 0x003594,
	GOLD: 0xffb81c,
	DARK_BLUE: 0x001d52,
	BRONZE: 0x8c6239,
	MARBLE: 0xf5f5f0,
	ONYX: 0x1c1c1c,
	CHROME: 0xd8d8d8,
};

export const PANTHER_FINISHES = [
	{
		id: 'scan',
		name: 'Original 3D Scan',
		description: 'Campus Photogrammetry Scan',
		type: 'original',
		color: '#8d7b68',
	},
	{
		id: 'pitt_blue',
		name: 'Pitt Royal Blue',
		description: 'Official Pitt Blue Finish',
		type: 'pbr',
		color: '#003594',
		roughness: 0.35,
		metalness: 0.15,
	},
	{
		id: 'pitt_gold',
		name: 'Pitt Athletic Gold',
		description: 'Official Pitt Gold Metallic',
		type: 'pbr',
		color: '#ffb81c',
		roughness: 0.25,
		metalness: 0.85,
	},
	{
		id: 'bronze',
		name: 'Cast Bronze',
		description: 'Classic Bronze Statue Finish',
		type: 'pbr',
		color: '#7b532d',
		roughness: 0.45,
		metalness: 0.7,
	},
	{
		id: 'marble',
		name: 'White Marble',
		description: 'Classical Polished Stone',
		type: 'pbr',
		color: '#eae6df',
		roughness: 0.2,
		metalness: 0.05,
	},
	{
		id: 'onyx',
		name: 'Bradford Onyx',
		description: 'Midnight Sleek Finish',
		type: 'pbr',
		color: '#18181a',
		roughness: 0.3,
		metalness: 0.4,
	},
];

export const PANTHER_SIZES = [
	{
		id: 'mini',
		name: 'Desk Mini',
		heightDisplay: '20 cm',
		scale: 0.075,
	},
	{
		id: 'tabletop',
		name: 'Tabletop',
		heightDisplay: '50 cm',
		scale: 0.18,
	},
	{
		id: 'pedestal',
		name: 'Pedestal',
		heightDisplay: '1.2 m',
		scale: 0.45,
	},
	{
		id: 'lifesize',
		name: 'Life-Size Statue',
		heightDisplay: '2.7 m (1:1)',
		scale: 1.0,
	},
	{
		id: 'monumental',
		name: 'Monumental',
		heightDisplay: '4.0 m',
		scale: 1.5,
	},
];

export const DEFAULT_SCALE_INDEX = 1; // Tabletop (~50cm)
