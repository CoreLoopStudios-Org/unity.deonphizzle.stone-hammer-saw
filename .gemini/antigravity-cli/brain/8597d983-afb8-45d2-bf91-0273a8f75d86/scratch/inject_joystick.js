const fs = require('fs');

const sceneFile = "C:\\Users\\User\\Documents\\GitHub\\unity.deonphizzle.stone-hammer-saw\\Assets\\Scenes\\Mob Squad 3d world scene.unity";

let content = fs.readFileSync(sceneFile, 'utf8');

// 1. Locate the Canvas RectTransform:
// --- !u!224 &1125946991
// RectTransform:
// ...
//   m_Children: []
// ...

const canvasHeader = "--- !u!224 &1125946991";
const canvasHeaderIndex = content.indexOf(canvasHeader);

if (canvasHeaderIndex === -1) {
    console.error("Error: Canvas RectTransform not found in scene!");
    process.exit(1);
}

// Find the m_Children: [] after the canvasHeader
const childrenIndex = content.indexOf("m_Children: []", canvasHeaderIndex);

if (childrenIndex === -1 || childrenIndex > canvasHeaderIndex + 1000) {
    console.error("Error: Canvas m_Children: [] not found near header!");
    process.exit(1);
}

// Replace m_Children: [] with m_Children: - {fileID: 888880002}
const originalPart = content.substring(canvasHeaderIndex, childrenIndex + "m_Children: []".length);
const replacedPart = originalPart.replace("m_Children: []", "m_Children:\n  - {fileID: 888880002}");

content = content.replace(originalPart, replacedPart);

// 2. Append the new Joystick GameObject and component YAML blocks at the end of the file:
const joystickYaml = `
--- !u!1 &888880001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 888880002}
  - component: {fileID: 888880003}
  - component: {fileID: 888880004}
  - component: {fileID: 888880005}
  m_Layer: 5
  m_Name: VirtualJoystick
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &888880002
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880001}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 888880007}
  m_Father: {fileID: 1125946991}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 150, y: 150}
  m_SizeDelta: {x: 180, y: 180}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &888880003
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880001}
  m_CullTransparentMesh: 1
--- !u!114 &888880004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 999be12ac5c27c9408ee8e2a8fa64999, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::VirtualJoystick
  container: {fileID: 888880002}
  handle: {fileID: 888880007}
--- !u!114 &888880005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0.3}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 10913, guid: 0000000000000000f000000000000000, type: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!1 &888880006
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 888880007}
  - component: {fileID: 888880008}
  - component: {fileID: 888880009}
  m_Layer: 5
  m_Name: KnobHandle
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &888880007
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880006}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 888880002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 70, y: 70}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &888880008
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880006}
  m_CullTransparentMesh: 1
--- !u!114 &888880009
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880006}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0.8}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 10913, guid: 0000000000000000f000000000000000, type: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`;

fs.appendFileSync(sceneFile, joystickYaml, 'utf8');
console.log("Joystick hierarchy and components successfully injected!");
