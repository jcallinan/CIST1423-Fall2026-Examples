// Node.js script to generate MiniGolf_AlumniCourse.unity
const fs = require('fs');

const examplePath = 'c:/GitHub/Alumni-Weekend-2026/Alumni-Weekend-2026/GolfVR/GolfVR/Assets/Scenes/Golf_Example.unity';
const outPath = 'c:/GitHub/Alumni-Weekend-2026/Alumni-Weekend-2026/GolfVR/GolfVR/Assets/Scenes/MiniGolf_AlumniCourse.unity';

const exText = fs.readFileSync(examplePath, 'utf8');

// 1. Scene Header (up to NavMeshSettings)
const navMeshEnd = exText.indexOf('--- !u!1001 &2062846');
let header = exText.substring(0, navMeshEnd);
// Replace LightingDataAsset and LightingSettings to eliminate broken references and version errors
header = header.replace(/m_LightingDataAsset: \{[^}]+\}/g, 'm_LightingDataAsset: {fileID: 0}');
header = header.replace(/m_LightingSettings: \{[^}]+\}/g, 'm_LightingSettings: {fileID: 0}');

// 2. Extract self-contained Environment from Golf_Example
const blocks = exText.split(/(?=--- !u!)/);
const blockMap = {};
blocks.forEach((b, idx) => {
  const m = b.match(/^--- !u!(\d+) &(\d+)/);
  if (m) blockMap[m[2]] = { idx, type: parseInt(m[1]), block: b };
});

function getOutgoingRefs(blockStr) {
  const refs = new Set();
  const reg = /fileID: (\d+)/g;
  let m;
  while ((m = reg.exec(blockStr)) !== null) {
    const id = m[1];
    if (id !== '0' && blockMap[id]) refs.add(id);
  }
  return Array.from(refs);
}

const envChildGoIds = ['1794918250', '1477037010', '1489781403', '36739965', '1964847907', '1351404439', '644274443', '535797689', '720330320', '20582818'];
const envEntities = new Set();
function addEntityAndComponents(goId) {
  envEntities.add(goId);
  const b = blockMap[goId];
  if (!b) return;
  getOutgoingRefs(b.block).forEach(id => envEntities.add(id));
}
addEntityAndComponents('1856428176'); // Environment
envEntities.add('1856428177'); // Environment Transform
envChildGoIds.forEach(id => addEntityAndComponents(id));

const sortedEnvBlocks = Array.from(envEntities).sort((a,b) => blockMap[a].idx - blockMap[b].idx).map(id => blockMap[id].block);
const envChunk = sortedEnvBlocks.join('');

// 3. SteamVR Player, SkySphere, CloudSpawner from Golf_Example
const playerBlock = blockMap['1893138394'].block;
const skySphereBlock = blockMap['618158414'].block;
const cloudSpawnerBlock = blockMap['1861812223'].block;

// 4. Prefab Definitions for 9 Holes
const PREFABS = {
  End: { guid: '04b8659cad704624588994bd97ac4637', target: '4696218340833953831' },
  Sides: { guid: '07965f88790b1a1459b5f42b85a2d6e2', target: '2432656271882505951' },
  Endhole: { guid: 'ad6c6fd7e14427047823a5d2f14b53c6', target: '8957525375532298913' },
  Corner: { guid: '19823bec1d1f4774991c51230a3d2630', target: '5860950571058749913' },
  Cornerhole: { guid: '2416362289f898b4c893c64eb9b92d60', target: '3698351029494824602' },
  FullSquare: { guid: 'dda27e2aa31614444b81737908e61659', target: '7847407099462762031' },
  Bumper: { guid: 'a65279c4fad7ab448813d9e259e9a3b9', target: '5898372634543305685' },
  Cross: { guid: '732d3378ecf71f042b9d98bc913dc68b', target: '2043745636102285409' },
  Line: { guid: 'c12ea050b83597a47a9dc334f863c691', target: '5357976474474777233' },
  Triangle: { guid: 'b465c127c7a70024ea9586596540d14a', target: '1638772576579974000' },
  Ramp: { guid: '9d670e2f7d433be4bb98e34d82568e4b', target: '8674819748471811732' },
  WindMill: { guid: '2ab0f99324683f945be41d406774f595', target: '9174169648645378515' },
  Flag: { guid: '80bc920729f878f43b98497f9905649f', target: '8354544366753741266' },
  Panther: { guid: '54e13750bd214d42b03b3ae18d7b95d6', target: '447954' },
  Zippo: { guid: '7b9148d234a948e28402c589b218412a', target: '447954' }
};

let instCounter = 810000000;
function makeInstance(prefabName, pos, rotY, scale, parentId) {
  instCounter++;
  rotY = rotY || 0;
  parentId = parentId || 0;
  const p = PREFABS[prefabName];
  if (!p) throw new Error('Unknown prefab ' + prefabName);

  const rad = rotY * Math.PI / 180.0;
  const qy = Math.sin(rad / 2.0);
  const qw = Math.cos(rad / 2.0);

  let scaleBlock = '';
  if (scale) {
    scaleBlock = '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
                 '      propertyPath: m_LocalScale.x\r\n' +
                 '      value: ' + scale[0] + '\r\n' +
                 '      objectReference: {fileID: 0}\r\n' +
                 '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
                 '      propertyPath: m_LocalScale.y\r\n' +
                 '      value: ' + scale[1] + '\r\n' +
                 '      objectReference: {fileID: 0}\r\n' +
                 '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
                 '      propertyPath: m_LocalScale.z\r\n' +
                 '      value: ' + scale[2] + '\r\n' +
                 '      objectReference: {fileID: 0}\r\n';
  }

  return '--- !u!1001 &' + instCounter + '\r\n' +
         'PrefabInstance:\r\n' +
         '  m_ObjectHideFlags: 0\r\n' +
         '  serializedVersion: 2\r\n' +
         '  m_Modification:\r\n' +
         '    serializedVersion: 3\r\n' +
         '    m_TransformParent: {fileID: ' + parentId + '}\r\n' +
         '    m_Modifications:\r\n' +
         '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
         '      propertyPath: m_LocalPosition.x\r\n' +
         '      value: ' + pos[0].toFixed(4) + '\r\n' +
         '      objectReference: {fileID: 0}\r\n' +
         '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
         '      propertyPath: m_LocalPosition.y\r\n' +
         '      value: ' + pos[1].toFixed(4) + '\r\n' +
         '      objectReference: {fileID: 0}\r\n' +
         '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
         '      propertyPath: m_LocalPosition.z\r\n' +
         '      value: ' + pos[2].toFixed(4) + '\r\n' +
         '      objectReference: {fileID: 0}\r\n' +
         '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
         '      propertyPath: m_LocalRotation.x\r\n' +
         '      value: 0\r\n' +
         '      objectReference: {fileID: 0}\r\n' +
         '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
         '      propertyPath: m_LocalRotation.y\r\n' +
         '      value: ' + qy.toFixed(6) + '\r\n' +
         '      objectReference: {fileID: 0}\r\n' +
         '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
         '      propertyPath: m_LocalRotation.z\r\n' +
         '      value: 0\r\n' +
         '      objectReference: {fileID: 0}\r\n' +
         '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
         '      propertyPath: m_LocalRotation.w\r\n' +
         '      value: ' + qw.toFixed(6) + '\r\n' +
         '      objectReference: {fileID: 0}\r\n' +
         '    - target: {fileID: ' + p.target + ', guid: ' + p.guid + ', type: 3}\r\n' +
         '      propertyPath: m_LocalEulerAnglesHint.y\r\n' +
         '      value: ' + rotY.toFixed(2) + '\r\n' +
         '      objectReference: {fileID: 0}\r\n' +
         scaleBlock +
         '    m_RemovedComponents: []\r\n' +
         '    m_RemovedGameObjects: []\r\n' +
         '    m_AddedGameObjects: []\r\n' +
         '    m_AddedComponents: []\r\n' +
         '  m_SourcePrefab: {fileID: 100100000, guid: ' + p.guid + ', type: 3}\r\n';
}

// 5. Generate all 9 holes modular tiles and obstacles
let courseTilesYaml = '';

// ============================================================
// ROTATION REFERENCE:
//  End/Endhole at rotY=0  : open at -X  (ball rolls -X / West)
//  End/Endhole at rotY=90 : open at -Z  (ball rolls -Z / South)
//  End/Endhole at rotY=180: open at +X  (ball rolls +X / East)
//  End/Endhole at rotY=270: open at +Z  (ball rolls +Z / North)
//  Sides at rotY=0  : rails N/S, open East-West  (+X/-X holes)
//  Sides at rotY=90 : rails E/W, open North-South (+Z/-Z holes)
// ============================================================

// ---- Hole 1: Long Drive (+Z, 4 tiles) ----
// Straight intro hole — extra-long run to warm up. Ball rolls North (+Z).
// Tee [-10, 0, -10]  Cup [-10, 0, -4]
courseTilesYaml += makeInstance('End',    [-10.0, 0.0, -10.0], 270); // open +Z toward Sides
courseTilesYaml += makeInstance('Sides',  [-10.0, 0.0,  -8.0],  90);
courseTilesYaml += makeInstance('Sides',  [-10.0, 0.0,  -6.0],  90);
courseTilesYaml += makeInstance('Endhole',[-10.0, 0.0,  -4.0], 270); // open -Z toward Sides
courseTilesYaml += makeInstance('Flag',   [-10.0, 0.0,  -4.0],   0);

// ---- Hole 2: Chicane Slalom (+Z, 3 tiles + staggered bumpers) ----
// Weave between two offset bumpers. Ball rolls North (+Z).
// Tee [-10, 0, -1]  Cup [-10, 0, 3]
courseTilesYaml += makeInstance('End',    [-10.0, 0.0, -1.0], 270);
courseTilesYaml += makeInstance('Sides',  [-10.0, 0.0,  1.0],  90);
courseTilesYaml += makeInstance('Bumper', [-10.4, 0.0,  0.5],   0); // left side
courseTilesYaml += makeInstance('Bumper', [ -9.6, 0.0,  1.5],   0); // right side
courseTilesYaml += makeInstance('Endhole',[-10.0, 0.0,  3.0], 270);
courseTilesYaml += makeInstance('Flag',   [-10.0, 0.0,  3.0],   0);

// ---- Hole 3: Wedge Alley (+X, 4 tiles + Triangle) ----
// Long straight with a Triangle deflector on the first Sides tile. Ball rolls East (+X).
// Tee [-12, 0, 9]  Cup [-6, 0, 9]
courseTilesYaml += makeInstance('End',    [-12.0, 0.0, 9.0], 180); // open +X toward Sides
courseTilesYaml += makeInstance('Sides',  [-10.0, 0.0, 9.0],   0);
courseTilesYaml += makeInstance('Triangle',[-10.0, 0.0, 9.55],  0); // nudged to rail edge
courseTilesYaml += makeInstance('Sides',  [ -8.0, 0.0, 9.0],   0);
courseTilesYaml += makeInstance('Endhole',[ -6.0, 0.0, 9.0], 180); // open -X toward Sides
courseTilesYaml += makeInstance('Flag',   [ -6.0, 0.0, 9.0],   0);

// ---- Hole 4: Gate Keeper (+X, 3 tiles + narrow bumper gate) ----
// Thread the needle between two bumpers. Ball rolls East (+X).
// Tee [-2, 0, 9]  Cup [2, 0, 9]
courseTilesYaml += makeInstance('End',    [-2.0, 0.0, 9.0], 180);
courseTilesYaml += makeInstance('Sides',  [ 0.0, 0.0, 9.0],   0);
courseTilesYaml += makeInstance('Bumper', [ 0.0, 0.0, 8.55],  0); // south gate post
courseTilesYaml += makeInstance('Bumper', [ 0.0, 0.0, 9.45],  0); // north gate post
courseTilesYaml += makeInstance('Endhole',[ 2.0, 0.0, 9.0], 180);
courseTilesYaml += makeInstance('Flag',   [ 2.0, 0.0, 9.0],   0);

// ---- Hole 5: Cross Roads (-Z, 4 tiles + Cross obstacle) ----
// Long run South through the X-shaped blocker. Ball rolls South (-Z).
// Tee [10, 0, 9]  Cup [10, 0, 3]
courseTilesYaml += makeInstance('End',    [10.0, 0.0, 9.0],  90); // open -Z toward Sides
courseTilesYaml += makeInstance('Sides',  [10.0, 0.0, 7.0],  90);
courseTilesYaml += makeInstance('Cross',  [10.0, 0.0, 7.0],  45); // sits on Sides tile
courseTilesYaml += makeInstance('Sides',  [10.0, 0.0, 5.0],  90);
courseTilesYaml += makeInstance('Endhole',[10.0, 0.0, 3.0],  90); // open +Z toward Sides
courseTilesYaml += makeInstance('Flag',   [10.0, 0.0, 3.0],   0);

// ---- Hole 6: The Zippo Flame (-X, 3 tiles + bumpers + Zippo monument) ----
// Historic Bradford Zippo Lighter monument towers beside the fairway.
// Ball rolls West (-X). Tee [4, 0, 1]  Cup [0, 0, 1]
courseTilesYaml += makeInstance('End',    [ 4.0, 0.0, 1.0],   0); // open -X toward Sides
courseTilesYaml += makeInstance('Sides',  [ 2.0, 0.0, 1.0],   0);
courseTilesYaml += makeInstance('Bumper', [ 2.0, 0.0, 0.55],  0);
courseTilesYaml += makeInstance('Bumper', [ 2.0, 0.0, 1.45],  0);
courseTilesYaml += makeInstance('Endhole',[ 0.0, 0.0, 1.0],   0); // open +X toward Sides
courseTilesYaml += makeInstance('Flag',   [ 0.0, 0.0, 1.0],   0);
// Iconic Bradford Zippo Lighter — stands guard north of the fairway!
courseTilesYaml += makeInstance('Zippo',  [ 2.0, 0.0, 3.2], 180, [1.8, 1.8, 1.8], 0);

// ---- Hole 7: Bumper Maze (+X, 4 tiles + staggered bumpers) ----
// Two bumpers offset on alternating sides create a zigzag challenge. Ball rolls East (+X).
// Tee [-8, 0, -4]  Cup [-2, 0, -4]
courseTilesYaml += makeInstance('End',    [-8.0, 0.0, -4.0], 180);
courseTilesYaml += makeInstance('Sides',  [-6.0, 0.0, -4.0],   0);
courseTilesYaml += makeInstance('Bumper', [-6.0, 0.0, -4.5],   0); // south-side
courseTilesYaml += makeInstance('Sides',  [-4.0, 0.0, -4.0],   0);
courseTilesYaml += makeInstance('Bumper', [-4.0, 0.0, -3.5],   0); // north-side
courseTilesYaml += makeInstance('Endhole',[-2.0, 0.0, -4.0], 180);
courseTilesYaml += makeInstance('Flag',   [-2.0, 0.0, -4.0],   0);

// ---- Hole 8: Windmill Gauntlet (+X, 4 tiles + WindMill) ----
// Two straight sections — time your shot through the spinning blades! Ball rolls East (+X).
// Tee [-4, 0, -10]  Cup [2, 0, -10]
courseTilesYaml += makeInstance('End',    [-4.0, 0.0, -10.0], 180);
courseTilesYaml += makeInstance('Sides',  [-2.0, 0.0, -10.0],   0);
courseTilesYaml += makeInstance('Sides',  [ 0.0, 0.0, -10.0],   0);
courseTilesYaml += makeInstance('WindMill',[ 0.0, 0.0, -10.0],  90); // sits on Sides tile
courseTilesYaml += makeInstance('Endhole',[ 2.0, 0.0, -10.0], 180);
courseTilesYaml += makeInstance('Flag',   [ 2.0, 0.0, -10.0],   0);

// ---- Hole 9: Grand Finale: Panther's Roar (-Z, 4 tiles + Panther Statue + bumpers) ----
// Championship finale — long run South past the Pitt Panther guardian. Ball rolls South (-Z).
// Tee [10, 0, -1]  Cup [10, 0, -7]
courseTilesYaml += makeInstance('End',    [10.0, 0.0, -1.0],  90); // open -Z toward Sides
courseTilesYaml += makeInstance('Sides',  [10.0, 0.0, -3.0],  90);
courseTilesYaml += makeInstance('Bumper', [ 9.5, 0.0, -3.0],   0); // west bumper
courseTilesYaml += makeInstance('Sides',  [10.0, 0.0, -5.0],  90);
courseTilesYaml += makeInstance('Bumper', [10.5, 0.0, -5.0],   0); // east bumper (zigzag)
courseTilesYaml += makeInstance('Endhole',[10.0, 0.0, -7.0],  90); // open +Z toward Sides
courseTilesYaml += makeInstance('Flag',   [10.0, 0.0, -7.0],   0);
// Authentic Pitt Panther Statue — guardian of the championship green!
courseTilesYaml += makeInstance('Panther',[12.2, 0.0, -7.0],  -90, [1.0, 1.0, 1.0], 0);

// 6. Floor TeleportArea (Allows VR teleportation across the entire 30m x 30m hall)
const floorTeleportYaml = 
'--- !u!1 &807809069\r\n' +
'GameObject:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  serializedVersion: 6\r\n' +
'  m_Component:\r\n' +
'  - component: {fileID: 807809070}\r\n' +
'  - component: {fileID: 807809074}\r\n' +
'  - component: {fileID: 807809073}\r\n' +
'  - component: {fileID: 807809072}\r\n' +
'  - component: {fileID: 807809071}\r\n' +
'  m_Layer: 0\r\n' +
'  m_Name: TeleportArea_Floor\r\n' +
'  m_TagString: Untagged\r\n' +
'  m_Icon: {fileID: 0}\r\n' +
'  m_NavMeshLayer: 0\r\n' +
'  m_StaticEditorFlags: 0\r\n' +
'  m_IsActive: 1\r\n' +
'--- !u!4 &807809070\r\n' +
'Transform:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 807809069}\r\n' +
'  serializedVersion: 2\r\n' +
'  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_LocalPosition: {x: 0, y: 0.01, z: 0}\r\n' +
'  m_LocalScale: {x: 3.5, y: 1.0, z: 3.5}\r\n' +
'  m_ConstrainProportionsScale: 0\r\n' +
'  m_Children: []\r\n' +
'  m_Father: {fileID: 0}\r\n' +
'  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n' +
'--- !u!114 &807809071\r\n' +
'MonoBehaviour:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 807809069}\r\n' +
'  m_Enabled: 1\r\n' +
'  m_EditorHideFlags: 0\r\n' +
'  m_Script: {fileID: 11500000, guid: 84a4f15a3179f1d4993aded11f4d0232, type: 3}\r\n' +
'  m_Name: \r\n' +
'  m_EditorClassIdentifier: \r\n' +
'  locked: 0\r\n' +
'  markerActive: 1\r\n' +
'--- !u!23 &807809072\r\n' +
'MeshRenderer:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 807809069}\r\n' +
'  m_Enabled: 1\r\n' +
'  m_CastShadows: 0\r\n' +
'  m_ReceiveShadows: 1\r\n' +
'  m_DynamicOccludee: 1\r\n' +
'  m_StaticShadowCaster: 0\r\n' +
'  m_MotionVectors: 1\r\n' +
'  m_LightProbeUsage: 1\r\n' +
'  m_ReflectionProbeUsage: 1\r\n' +
'  m_RayTracingMode: 2\r\n' +
'  m_RayTraceProcedural: 0\r\n' +
'  m_RenderingLayerMask: 1\r\n' +
'  m_RendererPriority: 0\r\n' +
'  m_Materials:\r\n' +
'  - {fileID: 2100000, guid: 417e639b66cd3a24e9c5312be99ce3bd, type: 2}\r\n' +
'  m_StaticBatchInfo:\r\n' +
'    firstSubMesh: 0\r\n' +
'    subMeshCount: 0\r\n' +
'  m_StaticBatchRoot: {fileID: 0}\r\n' +
'  m_ProbeAnchor: {fileID: 0}\r\n' +
'  m_LightProbeVolumeOverride: {fileID: 0}\r\n' +
'  m_ScaleInLightmap: 1\r\n' +
'  m_ReceiveGI: 1\r\n' +
'  m_PreserveUVs: 1\r\n' +
'  m_IgnoreNormalsForChartDetection: 0\r\n' +
'  m_ImportantGI: 0\r\n' +
'  m_StitchLightmapSeams: 1\r\n' +
'  m_SelectedEditorRenderState: 3\r\n' +
'  m_MinimumChartSize: 4\r\n' +
'  m_AutoUVMaxDistance: 0.5\r\n' +
'  m_AutoUVMaxAngle: 89\r\n' +
'  m_LightmapParameters: {fileID: 0}\r\n' +
'  m_SortingLayerID: 0\r\n' +
'  m_SortingLayer: 0\r\n' +
'  m_SortingOrder: 0\r\n' +
'  m_AdditionalVertexStreams: {fileID: 0}\r\n' +
'--- !u!64 &807809073\r\n' +
'MeshCollider:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 807809069}\r\n' +
'  m_Material: {fileID: 0}\r\n' +
'  m_IncludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_ExcludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_LayerOverridePriority: 0\r\n' +
'  m_IsTrigger: 0\r\n' +
'  m_ProvidesContacts: 0\r\n' +
'  m_Enabled: 1\r\n' +
'  serializedVersion: 5\r\n' +
'  m_Convex: 0\r\n' +
'  m_CookingOptions: 30\r\n' +
'  m_Mesh: {fileID: 10209, guid: 0000000000000000e000000000000000, type: 0}\r\n' +
'--- !u!33 &807809074\r\n' +
'MeshFilter:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 807809069}\r\n' +
'  m_Mesh: {fileID: 10209, guid: 0000000000000000e000000000000000, type: 0}\r\n';

// 7. Golf Ball GameObject and Components
const golfBallYaml =
'--- !u!1 &293071535\r\n' +
'GameObject:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  serializedVersion: 6\r\n' +
'  m_Component:\r\n' +
'  - component: {fileID: 293071541}\r\n' +
'  - component: {fileID: 293071540}\r\n' +
'  - component: {fileID: 293071539}\r\n' +
'  - component: {fileID: 293071538}\r\n' +
'  - component: {fileID: 293071537}\r\n' +
'  - component: {fileID: 293071542}\r\n' +
'  m_Layer: 0\r\n' +
'  m_Name: Ball\r\n' +
'  m_TagString: Ball\r\n' +
'  m_Icon: {fileID: 0}\r\n' +
'  m_NavMeshLayer: 0\r\n' +
'  m_StaticEditorFlags: 0\r\n' +
'  m_IsActive: 1\r\n' +
'--- !u!4 &293071541\r\n' +
'Transform:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 293071535}\r\n' +
'  serializedVersion: 2\r\n' +
'  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_LocalPosition: {x: -10.0, y: 0.05, z: -10.0}\r\n' +
'  m_LocalScale: {x: 0.1, y: 0.1, z: 0.1}\r\n' +
'  m_ConstrainProportionsScale: 1\r\n' +
'  m_Children: []\r\n' +
'  m_Father: {fileID: 0}\r\n' +
'  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n' +
'--- !u!33 &293071540\r\n' +
'MeshFilter:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 293071535}\r\n' +
'  m_Mesh: {fileID: 10207, guid: 0000000000000000e000000000000000, type: 0}\r\n' +
'--- !u!23 &293071539\r\n' +
'MeshRenderer:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 293071535}\r\n' +
'  m_Enabled: 1\r\n' +
'  m_CastShadows: 1\r\n' +
'  m_ReceiveShadows: 1\r\n' +
'  m_DynamicOccludee: 1\r\n' +
'  m_StaticShadowCaster: 0\r\n' +
'  m_MotionVectors: 1\r\n' +
'  m_LightProbeUsage: 1\r\n' +
'  m_ReflectionProbeUsage: 1\r\n' +
'  m_RayTracingMode: 2\r\n' +
'  m_RayTraceProcedural: 0\r\n' +
'  m_RenderingLayerMask: 1\r\n' +
'  m_RendererPriority: 0\r\n' +
'  m_Materials:\r\n' +
'  - {fileID: 2100000, guid: 61b2cbcbc7214c04cbd8532e98166027, type: 2}\r\n' +
'  m_StaticBatchInfo:\r\n' +
'    firstSubMesh: 0\r\n' +
'    subMeshCount: 0\r\n' +
'  m_StaticBatchRoot: {fileID: 0}\r\n' +
'  m_ProbeAnchor: {fileID: 0}\r\n' +
'  m_LightProbeVolumeOverride: {fileID: 0}\r\n' +
'  m_ScaleInLightmap: 1\r\n' +
'  m_ReceiveGI: 1\r\n' +
'  m_PreserveUVs: 0\r\n' +
'  m_IgnoreNormalsForChartDetection: 0\r\n' +
'  m_ImportantGI: 0\r\n' +
'  m_StitchLightmapSeams: 1\r\n' +
'  m_SelectedEditorRenderState: 3\r\n' +
'  m_MinimumChartSize: 4\r\n' +
'  m_AutoUVMaxDistance: 0.5\r\n' +
'  m_AutoUVMaxAngle: 89\r\n' +
'  m_LightmapParameters: {fileID: 0}\r\n' +
'  m_SortingLayerID: 0\r\n' +
'  m_SortingLayer: 0\r\n' +
'  m_SortingOrder: 0\r\n' +
'  m_AdditionalVertexStreams: {fileID: 0}\r\n' +
'--- !u!135 &293071538\r\n' +
'SphereCollider:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 293071535}\r\n' +
'  m_Material: {fileID: 13400000, guid: 1d1c945b14b8ee443b83c3ee79c57476, type: 2}\r\n' +
'  m_IncludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_ExcludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_LayerOverridePriority: 0\r\n' +
'  m_IsTrigger: 0\r\n' +
'  m_ProvidesContacts: 0\r\n' +
'  m_Enabled: 1\r\n' +
'  serializedVersion: 3\r\n' +
'  m_Radius: 0.5\r\n' +
'  m_Center: {x: 0, y: 0, z: 0}\r\n' +
'--- !u!54 &293071537\r\n' +
'Rigidbody:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 293071535}\r\n' +
'  serializedVersion: 4\r\n' +
'  m_Mass: 0.5\r\n' +
'  m_Drag: 1\r\n' +
'  m_AngularDrag: 0.8\r\n' +
'  m_CenterOfMass: {x: 0, y: 0, z: 0}\r\n' +
'  m_InertiaTensor: {x: 1, y: 1, z: 1}\r\n' +
'  m_InertiaRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_IncludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_ExcludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_ImplicitCom: 1\r\n' +
'  m_ImplicitTensor: 1\r\n' +
'  m_UseGravity: 1\r\n' +
'  m_IsKinematic: 0\r\n' +
'  m_Interpolate: 0\r\n' +
'  m_Constraints: 0\r\n' +
'  m_CollisionDetection: 1\r\n' +
'--- !u!114 &293071542\r\n' +
'MonoBehaviour:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 293071535}\r\n' +
'  m_Enabled: 1\r\n' +
'  m_EditorHideFlags: 0\r\n' +
'  m_Script: {fileID: 11500000, guid: 01d9da23cb7f470aa1c01222b60afc0b, type: 3}\r\n' +
'  m_Name: \r\n' +
'  m_EditorClassIdentifier: \r\n' +
'  stopVelocityThreshold: 0.08\r\n' +
'  rollingDrag: 0.4\r\n' +
'  rollingAngularDrag: 0.6\r\n' +
'  outOfBoundsY: -3.0\r\n' +
'  maxRollDuration: 12.0\r\n' +
'  audioSource: {fileID: 0}\r\n' +
'  bounceClip: {fileID: 0}\r\n';

// 8. Golf Putter GameObject and Components
const golfPutterYaml =
'--- !u!1 &910000100\r\n' +
'GameObject:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  serializedVersion: 6\r\n' +
'  m_Component:\r\n' +
'  - component: {fileID: 910000101}\r\n' +
'  - component: {fileID: 910000102}\r\n' +
'  - component: {fileID: 910000103}\r\n' +
'  - component: {fileID: 910000104}\r\n' +
'  - component: {fileID: 910000105}\r\n' +
'  - component: {fileID: 910000106}\r\n' +
'  m_Layer: 0\r\n' +
'  m_Name: GolfPutter_VR\r\n' +
'  m_TagString: Untagged\r\n' +
'  m_Icon: {fileID: 0}\r\n' +
'  m_NavMeshLayer: 0\r\n' +
'  m_StaticEditorFlags: 0\r\n' +
'  m_IsActive: 1\r\n' +
'--- !u!4 &910000101\r\n' +
'Transform:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000100}\r\n' +
'  serializedVersion: 2\r\n' +
'  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_LocalPosition: {x: -10.4, y: 0.5, z: -10.0}\r\n' +
'  m_LocalScale: {x: 1, y: 1, z: 1}\r\n' +
'  m_ConstrainProportionsScale: 0\r\n' +
'  m_Children:\r\n' +
'  - {fileID: 910000111}\r\n' +
'  - {fileID: 910000121}\r\n' +
'  m_Father: {fileID: 0}\r\n' +
'  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n' +
'--- !u!54 &910000102\r\n' +
'Rigidbody:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000100}\r\n' +
'  serializedVersion: 4\r\n' +
'  m_Mass: 1.0\r\n' +
'  m_Drag: 0.2\r\n' +
'  m_AngularDrag: 0.5\r\n' +
'  m_CenterOfMass: {x: 0, y: 0, z: 0}\r\n' +
'  m_InertiaTensor: {x: 1, y: 1, z: 1}\r\n' +
'  m_InertiaRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_IncludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_ExcludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_ImplicitCom: 1\r\n' +
'  m_ImplicitTensor: 1\r\n' +
'  m_UseGravity: 1\r\n' +
'  m_IsKinematic: 0\r\n' +
'  m_Interpolate: 1\r\n' +
'  m_Constraints: 0\r\n' +
'  m_CollisionDetection: 2\r\n' +
'--- !u!114 &910000103\r\n' +
'MonoBehaviour:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000100}\r\n' +
'  m_Enabled: 1\r\n' +
'  m_EditorHideFlags: 0\r\n' +
'  m_Script: {fileID: 11500000, guid: 7e511d1345674fcc9f0714782593be3f, type: 3}\r\n' +
'  m_Name: \r\n' +
'  m_EditorClassIdentifier: \r\n' +
'  clubHead: {fileID: 910000121}\r\n' +
'  gripPoint: {fileID: 910000111}\r\n' +
'  powerMultiplier: 2.2\r\n' +
'  minSwingSpeed: 0.15\r\n' +
'  maxBallSpeed: 15.0\r\n' +
'  hapticDuration: 0.12\r\n' +
'  hapticFrequency: 160.0\r\n' +
'  hapticAmplitude: 0.75\r\n' +
'  audioSource: {fileID: 910000106}\r\n' +
'  customHitClip: {fileID: 0}\r\n' +
'--- !u!114 &910000104\r\n' +
'MonoBehaviour:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000100}\r\n' +
'  m_Enabled: 1\r\n' +
'  m_EditorHideFlags: 0\r\n' +
'  m_Script: {fileID: 11500000, guid: b93b6a877adcbf94c89a9d6e0c0e844d, type: 3}\r\n' +
'  m_Name: \r\n' +
'  m_EditorClassIdentifier: \r\n' +
'  activateActionSetOnAttach: {fileID: 0}\r\n' +
'  hideHandOnAttach: 0\r\n' +
'  hideSkeletonOnAttach: 0\r\n' +
'  hideControllerOnAttach: 0\r\n' +
'  handAnimationOnPickup: 0\r\n' +
'  setRangeOfMotionOnPickup: 0\r\n' +
'  useHandObjectAttachmentPoint: 1\r\n' +
'  attachEaseIn: 1\r\n' +
'  snapAttachEaseInTime: 0.15\r\n' +
'  handFollowTransform: 1\r\n' +
'--- !u!136 &910000105\r\n' +
'CapsuleCollider:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000100}\r\n' +
'  m_Material: {fileID: 0}\r\n' +
'  m_IncludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_ExcludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_LayerOverridePriority: 0\r\n' +
'  m_IsTrigger: 0\r\n' +
'  m_Radius: 0.02\r\n' +
'  m_Height: 0.85\r\n' +
'  m_Direction: 1\r\n' +
'  m_Center: {x: 0, y: 0.42, z: 0}\r\n' +
'--- !u!82 &910000106\r\n' +
'AudioSource:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000100}\r\n' +
'  m_Enabled: 1\r\n' +
'  serializedVersion: 4\r\n' +
'  OutputAudioMixerGroup: {fileID: 0}\r\n' +
'  m_audioClip: {fileID: 0}\r\n' +
'  m_PlayOnAwake: 0\r\n' +
'  m_Volume: 1\r\n' +
'  m_Pitch: 1\r\n' +
'  Loop: 0\r\n' +
'  Mute: 0\r\n' +
'  Spatialize: 0\r\n' +
'  SpatializePostEffects: 0\r\n' +
'  Priority: 128\r\n' +
'  DopplerLevel: 1\r\n' +
'  MinDistance: 1\r\n' +
'  MaxDistance: 500\r\n' +
'  Pan2D: 0\r\n' +
'  rolloffMode: 0\r\n' +
'  BypassEffects: 0\r\n' +
'  BypassListenerEffects: 0\r\n' +
'  BypassReverbZones: 0\r\n' +
'  rolloffCustomCurve:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Curve: []\r\n' +
'    m_PreInfinity: 2\r\n' +
'    m_PostInfinity: 2\r\n' +
'    m_RotationOrder: 4\r\n' +
'  panLevelCustomCurve:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Curve:\r\n' +
'    - serializedVersion: 3\r\n' +
'      time: 0\r\n' +
'      value: 1\r\n' +
'      inSlope: 0\r\n' +
'      outSlope: 0\r\n' +
'      tangentMode: 0\r\n' +
'      weightedMode: 0\r\n' +
'      inWeight: 0.33333334\r\n' +
'      outWeight: 0.33333334\r\n' +
'    m_PreInfinity: 2\r\n' +
'    m_PostInfinity: 2\r\n' +
'    m_RotationOrder: 4\r\n' +
'  spreadCustomCurve:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Curve: []\r\n' +
'    m_PreInfinity: 2\r\n' +
'    m_PostInfinity: 2\r\n' +
'    m_RotationOrder: 4\r\n' +
'  reverbZoneMixCustomCurve:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Curve: []\r\n' +
'    m_PreInfinity: 2\r\n' +
'    m_PostInfinity: 2\r\n' +
'    m_RotationOrder: 4\r\n' +
'--- !u!1 &910000110\r\n' +
'GameObject:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  serializedVersion: 6\r\n' +
'  m_Component:\r\n' +
'  - component: {fileID: 910000111}\r\n' +
'  m_Layer: 0\r\n' +
'  m_Name: Grip\r\n' +
'  m_TagString: Untagged\r\n' +
'  m_Icon: {fileID: 0}\r\n' +
'  m_NavMeshLayer: 0\r\n' +
'  m_StaticEditorFlags: 0\r\n' +
'  m_IsActive: 1\r\n' +
'--- !u!4 &910000111\r\n' +
'Transform:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000110}\r\n' +
'  serializedVersion: 2\r\n' +
'  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_LocalPosition: {x: 0, y: 0.65, z: 0}\r\n' +
'  m_LocalScale: {x: 1, y: 1, z: 1}\r\n' +
'  m_ConstrainProportionsScale: 0\r\n' +
'  m_Children: []\r\n' +
'  m_Father: {fileID: 910000101}\r\n' +
'  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n' +
'--- !u!1 &910000120\r\n' +
'GameObject:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  serializedVersion: 6\r\n' +
'  m_Component:\r\n' +
'  - component: {fileID: 910000121}\r\n' +
'  - component: {fileID: 910000122}\r\n' +
'  m_Layer: 0\r\n' +
'  m_Name: ClubHead\r\n' +
'  m_TagString: Untagged\r\n' +
'  m_Icon: {fileID: 0}\r\n' +
'  m_NavMeshLayer: 0\r\n' +
'  m_StaticEditorFlags: 0\r\n' +
'  m_IsActive: 1\r\n' +
'--- !u!4 &910000121\r\n' +
'Transform:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000120}\r\n' +
'  serializedVersion: 2\r\n' +
'  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_LocalPosition: {x: 0.04, y: 0.02, z: 0}\r\n' +
'  m_LocalScale: {x: 1, y: 1, z: 1}\r\n' +
'  m_ConstrainProportionsScale: 0\r\n' +
'  m_Children: []\r\n' +
'  m_Father: {fileID: 910000101}\r\n' +
'  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n' +
'--- !u!65 &910000122\r\n' +
'BoxCollider:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 910000120}\r\n' +
'  m_Material: {fileID: 0}\r\n' +
'  m_IncludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_ExcludeLayers:\r\n' +
'    serializedVersion: 2\r\n' +
'    m_Bits: 0\r\n' +
'  m_LayerOverridePriority: 0\r\n' +
'  m_IsTrigger: 0\r\n' +
'  m_Size: {x: 0.12, y: 0.04, z: 0.035}\r\n' +
'  m_Center: {x: 0, y: 0, z: 0}\r\n';

// 9. All 9 Holes GameObjects, Triggers, TeePoints, PlayerStands
const HOLES_DATA = [
  {
    num: 1,
    name: "Long Drive",
    par: 2,
    cupPos: [-10.0, 0.0, -4.0],
    teePos: [-10.0, 0.0, -10.0],
    playerPos: [-10.8, 0.0, -10.0]
  },
  {
    num: 2,
    name: "Chicane Slalom",
    par: 3,
    cupPos: [-10.0, 0.0, 3.0],
    teePos: [-10.0, 0.0, -1.0],
    playerPos: [-10.8, 0.0, -1.0]
  },
  {
    num: 3,
    name: "Wedge Alley",
    par: 3,
    cupPos: [-6.0, 0.0, 9.0],
    teePos: [-12.0, 0.0, 9.0],
    playerPos: [-12.0, 0.0, 9.8]
  },
  {
    num: 4,
    name: "Gate Keeper",
    par: 2,
    cupPos: [2.0, 0.0, 9.0],
    teePos: [-2.0, 0.0, 9.0],
    playerPos: [-2.0, 0.0, 9.8]
  },
  {
    num: 5,
    name: "Cross Roads",
    par: 3,
    cupPos: [10.0, 0.0, 3.0],
    teePos: [10.0, 0.0, 9.0],
    playerPos: [10.8, 0.0, 9.0]
  },
  {
    num: 6,
    name: "The Zippo Flame",
    par: 3,
    cupPos: [0.0, 0.0, 1.0],
    teePos: [4.0, 0.0, 1.0],
    playerPos: [4.0, 0.0, 1.8]
  },
  {
    num: 7,
    name: "Bumper Maze",
    par: 3,
    cupPos: [-2.0, 0.0, -4.0],
    teePos: [-8.0, 0.0, -4.0],
    playerPos: [-8.0, 0.0, -4.8]
  },
  {
    num: 8,
    name: "Windmill Gauntlet",
    par: 3,
    cupPos: [2.0, 0.0, -10.0],
    teePos: [-4.0, 0.0, -10.0],
    playerPos: [-4.0, 0.0, -10.8]
  },
  {
    num: 9,
    name: "Grand Finale: Panther's Roar",
    par: 4,
    cupPos: [10.0, 0.0, -7.0],
    teePos: [10.0, 0.0, -1.0],
    playerPos: [10.8, 0.0, -1.0]
  }
];

let holesYaml = '# --- All 9 Holes Definitions ---\r\n';
for (const h of HOLES_DATA) {
  const goId = 920000000 + h.num * 1000;
  const trId = goId + 1;
  const colId = goId + 2;
  const mbId = goId + 3;

  const teeGoId = goId + 10;
  const teeTrId = goId + 11;

  const plyGoId = goId + 20;
  const plyTrId = goId + 21;

  holesYaml +=
`--- !u!1 &${goId}\r\n` +
`GameObject:\r\n` +
`  m_ObjectHideFlags: 0\r\n` +
`  m_CorrespondingSourceObject: {fileID: 0}\r\n` +
`  m_PrefabInstance: {fileID: 0}\r\n` +
`  m_PrefabAsset: {fileID: 0}\r\n` +
`  serializedVersion: 6\r\n` +
`  m_Component:\r\n` +
`  - component: {fileID: ${trId}}\r\n` +
`  - component: {fileID: ${colId}}\r\n` +
`  - component: {fileID: ${mbId}}\r\n` +
`  m_Layer: 0\r\n` +
`  m_Name: Hole${h.num}_${h.name.replace(/[^a-zA-Z0-9]/g, '')}\r\n` +
`  m_TagString: Untagged\r\n` +
`  m_Icon: {fileID: 0}\r\n` +
`  m_NavMeshLayer: 0\r\n` +
`  m_StaticEditorFlags: 0\r\n` +
`  m_IsActive: 1\r\n` +
`--- !u!4 &${trId}\r\n` +
`Transform:\r\n` +
`  m_ObjectHideFlags: 0\r\n` +
`  m_CorrespondingSourceObject: {fileID: 0}\r\n` +
`  m_PrefabInstance: {fileID: 0}\r\n` +
`  m_PrefabAsset: {fileID: 0}\r\n` +
`  m_GameObject: {fileID: ${goId}}\r\n` +
`  serializedVersion: 2\r\n` +
`  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n` +
`  m_LocalPosition: {x: ${h.cupPos[0].toFixed(2)}, y: ${h.cupPos[1].toFixed(2)}, z: ${h.cupPos[2].toFixed(2)}}\r\n` +
`  m_LocalScale: {x: 1, y: 1, z: 1}\r\n` +
`  m_ConstrainProportionsScale: 0\r\n` +
`  m_Children: []\r\n` +
`  m_Father: {fileID: 0}\r\n` +
`  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n` +
`--- !u!135 &${colId}\r\n` +
`SphereCollider:\r\n` +
`  m_ObjectHideFlags: 0\r\n` +
`  m_CorrespondingSourceObject: {fileID: 0}\r\n` +
`  m_PrefabInstance: {fileID: 0}\r\n` +
`  m_PrefabAsset: {fileID: 0}\r\n` +
`  m_GameObject: {fileID: ${goId}}\r\n` +
`  m_Material: {fileID: 0}\r\n` +
`  m_IncludeLayers:\r\n` +
`    serializedVersion: 2\r\n` +
`    m_Bits: 0\r\n` +
`  m_ExcludeLayers:\r\n` +
`    serializedVersion: 2\r\n` +
`    m_Bits: 0\r\n` +
`  m_LayerOverridePriority: 0\r\n` +
`  m_IsTrigger: 1\r\n` +
`  m_Radius: 0.35\r\n` +
`  m_Center: {x: 0, y: 0.05, z: 0}\r\n` +
`--- !u!114 &${mbId}\r\n` +
`MonoBehaviour:\r\n` +
`  m_ObjectHideFlags: 0\r\n` +
`  m_CorrespondingSourceObject: {fileID: 0}\r\n` +
`  m_PrefabInstance: {fileID: 0}\r\n` +
`  m_PrefabAsset: {fileID: 0}\r\n` +
`  m_GameObject: {fileID: ${goId}}\r\n` +
`  m_Enabled: 1\r\n` +
`  m_EditorHideFlags: 0\r\n` +
`  m_Script: {fileID: 11500000, guid: f1840a9dfb7f40448b27f87f5227e8ba, type: 3}\r\n` +
`  m_Name: \r\n` +
`  m_EditorClassIdentifier: \r\n` +
`  holeNumber: ${h.num}\r\n` +
`  holeName: ${h.name}\r\n` +
`  par: ${h.par}\r\n` +
`  teePoint: {fileID: ${teeTrId}}\r\n` +
`  playerTeeLocation: {fileID: ${plyTrId}}\r\n` +
`  flagTransform: {fileID: 0}\r\n` +
`  celebrationParticles: {fileID: 0}\r\n` +
`  audioSource: {fileID: 0}\r\n` +
`  celebrationFanfare: {fileID: 0}\r\n` +
`--- !u!1 &${teeGoId}\r\n` +
`GameObject:\r\n` +
`  m_ObjectHideFlags: 0\r\n` +
`  m_CorrespondingSourceObject: {fileID: 0}\r\n` +
`  m_PrefabInstance: {fileID: 0}\r\n` +
`  m_PrefabAsset: {fileID: 0}\r\n` +
`  serializedVersion: 6\r\n` +
`  m_Component:\r\n` +
`  - component: {fileID: ${teeTrId}}\r\n` +
`  m_Layer: 0\r\n` +
`  m_Name: Tee_Hole${h.num}\r\n` +
`  m_TagString: Untagged\r\n` +
`  m_Icon: {fileID: 0}\r\n` +
`  m_NavMeshLayer: 0\r\n` +
`  m_StaticEditorFlags: 0\r\n` +
`  m_IsActive: 1\r\n` +
`--- !u!4 &${teeTrId}\r\n` +
`Transform:\r\n` +
`  m_ObjectHideFlags: 0\r\n` +
`  m_CorrespondingSourceObject: {fileID: 0}\r\n` +
`  m_PrefabInstance: {fileID: 0}\r\n` +
`  m_PrefabAsset: {fileID: 0}\r\n` +
`  m_GameObject: {fileID: ${teeGoId}}\r\n` +
`  serializedVersion: 2\r\n` +
`  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n` +
`  m_LocalPosition: {x: ${h.teePos[0].toFixed(2)}, y: ${h.teePos[1].toFixed(2)}, z: ${h.teePos[2].toFixed(2)}}\r\n` +
`  m_LocalScale: {x: 1, y: 1, z: 1}\r\n` +
`  m_ConstrainProportionsScale: 0\r\n` +
`  m_Children: []\r\n` +
`  m_Father: {fileID: 0}\r\n` +
`  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n` +
`--- !u!1 &${plyGoId}\r\n` +
`GameObject:\r\n` +
`  m_ObjectHideFlags: 0\r\n` +
`  m_CorrespondingSourceObject: {fileID: 0}\r\n` +
`  m_PrefabInstance: {fileID: 0}\r\n` +
`  m_PrefabAsset: {fileID: 0}\r\n` +
`  serializedVersion: 6\r\n` +
`  m_Component:\r\n` +
`  - component: {fileID: ${plyTrId}}\r\n` +
`  m_Layer: 0\r\n` +
`  m_Name: PlayerTee_Hole${h.num}\r\n` +
`  m_TagString: Untagged\r\n` +
`  m_Icon: {fileID: 0}\r\n` +
`  m_NavMeshLayer: 0\r\n` +
`  m_StaticEditorFlags: 0\r\n` +
`  m_IsActive: 1\r\n` +
`--- !u!4 &${plyTrId}\r\n` +
`Transform:\r\n` +
`  m_ObjectHideFlags: 0\r\n` +
`  m_CorrespondingSourceObject: {fileID: 0}\r\n` +
`  m_PrefabInstance: {fileID: 0}\r\n` +
`  m_PrefabAsset: {fileID: 0}\r\n` +
`  m_GameObject: {fileID: ${plyGoId}}\r\n` +
`  serializedVersion: 2\r\n` +
`  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n` +
`  m_LocalPosition: {x: ${h.playerPos[0].toFixed(2)}, y: ${h.playerPos[1].toFixed(2)}, z: ${h.playerPos[2].toFixed(2)}}\r\n` +
`  m_LocalScale: {x: 1, y: 1, z: 1}\r\n` +
`  m_ConstrainProportionsScale: 0\r\n` +
`  m_Children: []\r\n` +
`  m_Father: {fileID: 0}\r\n` +
`  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n`;
}

// 10. Scoreboard & GameManager
const scoreboardGameManagerYaml =
'# --- In-VR Scoreboard ---\r\n' +
'--- !u!1 &960000100\r\n' +
'GameObject:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  serializedVersion: 6\r\n' +
'  m_Component:\r\n' +
'  - component: {fileID: 960000101}\r\n' +
'  - component: {fileID: 960000102}\r\n' +
'  m_Layer: 0\r\n' +
'  m_Name: MiniGolfScoreboard\r\n' +
'  m_TagString: Untagged\r\n' +
'  m_Icon: {fileID: 0}\r\n' +
'  m_NavMeshLayer: 0\r\n' +
'  m_StaticEditorFlags: 0\r\n' +
'  m_IsActive: 1\r\n' +
'--- !u!4 &960000101\r\n' +
'Transform:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 960000100}\r\n' +
'  serializedVersion: 2\r\n' +
'  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_LocalPosition: {x: -12.0, y: 1.2, z: -10.0}\r\n' +
'  m_LocalScale: {x: 1, y: 1, z: 1}\r\n' +
'  m_ConstrainProportionsScale: 0\r\n' +
'  m_Children: []\r\n' +
'  m_Father: {fileID: 0}\r\n' +
'  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n' +
'--- !u!114 &960000102\r\n' +
'MonoBehaviour:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 960000100}\r\n' +
'  m_Enabled: 1\r\n' +
'  m_EditorHideFlags: 0\r\n' +
'  m_Script: {fileID: 11500000, guid: 66c7e95cc61f4f99b4b7c5de62eecd61, type: 3}\r\n' +
'  m_Name: \r\n' +
'  m_EditorClassIdentifier: \r\n' +
'  titleText: {fileID: 0}\r\n' +
'  subtitleText: {fileID: 0}\r\n' +
'  holeParTexts: []\r\n' +
'  holeStrokeTexts: []\r\n' +
'  holeStatusTexts: []\r\n' +
'  totalScoreText: {fileID: 0}\r\n' +
'  currentHoleIndicatorText: {fileID: 0}\r\n' +
'  bannerText: {fileID: 0}\r\n' +
'  bannerPanel: {fileID: 0}\r\n' +
'# --- Mini Golf Game Manager (9 Holes) ---\r\n' +
'--- !u!1 &970000100\r\n' +
'GameObject:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  serializedVersion: 6\r\n' +
'  m_Component:\r\n' +
'  - component: {fileID: 970000101}\r\n' +
'  - component: {fileID: 970000102}\r\n' +
'  m_Layer: 0\r\n' +
'  m_Name: MiniGolfGameManager\r\n' +
'  m_TagString: Untagged\r\n' +
'  m_Icon: {fileID: 0}\r\n' +
'  m_NavMeshLayer: 0\r\n' +
'  m_StaticEditorFlags: 0\r\n' +
'  m_IsActive: 1\r\n' +
'--- !u!4 &970000101\r\n' +
'Transform:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 970000100}\r\n' +
'  serializedVersion: 2\r\n' +
'  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\r\n' +
'  m_LocalPosition: {x: 0, y: 0, z: 0}\r\n' +
'  m_LocalScale: {x: 1, y: 1, z: 1}\r\n' +
'  m_ConstrainProportionsScale: 0\r\n' +
'  m_Children: []\r\n' +
'  m_Father: {fileID: 0}\r\n' +
'  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\r\n' +
'--- !u!114 &970000102\r\n' +
'MonoBehaviour:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_CorrespondingSourceObject: {fileID: 0}\r\n' +
'  m_PrefabInstance: {fileID: 0}\r\n' +
'  m_PrefabAsset: {fileID: 0}\r\n' +
'  m_GameObject: {fileID: 970000100}\r\n' +
'  m_Enabled: 1\r\n' +
'  m_EditorHideFlags: 0\r\n' +
'  m_Script: {fileID: 11500000, guid: cdd113dc7e46488ba29f3676d4913d55, type: 3}\r\n' +
'  m_Name: \r\n' +
'  m_EditorClassIdentifier: \r\n' +
'  holes:\r\n' +
'  - {fileID: 920001003}\r\n' +
'  - {fileID: 920002003}\r\n' +
'  - {fileID: 920003003}\r\n' +
'  - {fileID: 920004003}\r\n' +
'  - {fileID: 920005003}\r\n' +
'  - {fileID: 920006003}\r\n' +
'  - {fileID: 920007003}\r\n' +
'  - {fileID: 920008003}\r\n' +
'  - {fileID: 920009003}\r\n' +
'  golfBall: {fileID: 293071542}\r\n' +
'  golfPutter: {fileID: 910000103}\r\n' +
'  playerTransform: {fileID: 0}\r\n' +
'  scoreboard: {fileID: 960000102}\r\n' +
'  holeTransitionDelay: 3.5\r\n' +
'  globalAudioSource: {fileID: 0}\r\n';

// 11. SceneRoots list
const sceneRootsYaml =
'--- !u!1660057539 &9223372036854775807\r\n' +
'SceneRoots:\r\n' +
'  m_ObjectHideFlags: 0\r\n' +
'  m_Roots:\r\n' +
'  - {fileID: 1856428177}\r\n' +
'  - {fileID: 1893138394}\r\n' +
'  - {fileID: 618158414}\r\n' +
'  - {fileID: 1861812223}\r\n' +
'  - {fileID: 807809070}\r\n' +
'  - {fileID: 293071541}\r\n' +
'  - {fileID: 910000101}\r\n' +
'  - {fileID: 920001001}\r\n' +
'  - {fileID: 920002001}\r\n' +
'  - {fileID: 920003001}\r\n' +
'  - {fileID: 920004001}\r\n' +
'  - {fileID: 920005001}\r\n' +
'  - {fileID: 920006001}\r\n' +
'  - {fileID: 920007001}\r\n' +
'  - {fileID: 920008001}\r\n' +
'  - {fileID: 920009001}\r\n' +
'  - {fileID: 960000101}\r\n' +
'  - {fileID: 970000101}\r\n';

// Assemble complete scene
const completeScene = 
  header +
  envChunk +
  playerBlock +
  skySphereBlock +
  cloudSpawnerBlock +
  floorTeleportYaml +
  courseTilesYaml +
  golfBallYaml +
  golfPutterYaml +
  holesYaml +
  scoreboardGameManagerYaml +
  sceneRootsYaml;

fs.writeFileSync(outPath, completeScene, 'utf8');
console.log('Successfully generated complete MiniGolf_AlumniCourse.unity!');
console.log('Output size: ' + completeScene.length + ' bytes, ' + completeScene.split('\n').length + ' lines');

