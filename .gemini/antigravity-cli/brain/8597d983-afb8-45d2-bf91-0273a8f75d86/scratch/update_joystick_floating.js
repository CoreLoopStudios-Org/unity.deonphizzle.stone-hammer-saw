const fs = require('fs');

const sceneFile = "C:\\Users\\User\\Documents\\GitHub\\unity.deonphizzle.stone-hammer-saw\\Assets\\Scenes\\Mob Squad 3d world scene.unity";

let content = fs.readFileSync(sceneFile, 'utf8');

// Truncate the previously appended joystick block
const cutIndex = content.indexOf("--- !u!1 &888880001");
if (cutIndex !== -1) {
    content = content.substring(0, cutIndex);
}

// Ensure the Canvas child list is correct
// Canvas RectTransform is at 1125946991. Make sure it points to the touch zone (888880002)
// In the Canvas RectTransform definition, it has m_Children:
const canvasHeader = "--- !u!224 &1125946991";
const canvasHeaderIndex = content.indexOf(canvasHeader);

if (canvasHeaderIndex === -1) {
    console.error("Error: Canvas RectTransform not found in scene!");
    process.exit(1);
}

// Find m_Children in Canvas RectTransform
const childrenIndex = content.indexOf("m_Children:", canvasHeaderIndex);
if (childrenIndex !== -1 && childrenIndex < canvasHeaderIndex + 500) {
    // Look for the child line(s)
    const nextLineIndex = content.indexOf("\n", childrenIndex);
    const afterChildrenIndex = content.indexOf("m_Father:", childrenIndex);
    
    // We replace the entire child block with a single child: 888880002 (JoystickTouchZone)
    const originalChildrenPart = content.substring(childrenIndex, afterChildrenIndex);
    content = content.replace(originalChildrenPart, "m_Children:\n  - {fileID: 888880002}\n  ");
}

// Write the new YAML block for the floating joystick structure:
// 888880001: JoystickTouchZone (GameObject)
// 888880002: JoystickTouchZone (RectTransform) - covers 500x500 area
// 888880003: JoystickTouchZone (CanvasRenderer)
// 888880004: VirtualJoystick component
// 888880005: Image component (almost transparent touch detector)
//
// 888880010: JoystickBgCircle (GameObject) - the visual 180x180 circle
// 888880011: JoystickBgCircle (RectTransform) - parent set to 888880002
// 888880012: JoystickBgCircle (CanvasRenderer)
// 888880013: Image component (visual background sprite, alpha 0.3)
//
// 888880006: KnobHandle (GameObject) - parent set to 888880011
// 888880007: KnobHandle (RectTransform) - anchored at center
// 888880008: KnobHandle (CanvasRenderer)
// 888880009: Image component (solid knob sprite, alpha 0.8)

const floatingJoystickYaml = `--- !u!1 &888880001
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
  m_Name: JoystickTouchZone
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
  - {fileID: 888880011}
  m_Father: {fileID: 1125946991}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 250, y: 250}
  m_SizeDelta: {x: 500, y: 500}
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
  container: {fileID: 888880011}
  handle: {fileID: 888880007}
  touchZone: {fileID: 888880002}
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
  m_Color: {r: 1, g: 1, b: 1, a: 0.01}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!1 &888880010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 888880011}
  - component: {fileID: 88888012}
  - component: {fileID: 88888013}
  m_Layer: 5
  m_Name: JoystickBgCircle
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &888880011
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880010}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 888880007}
  m_Father: {fileID: 888880002}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: -100, y: -100}
  m_SizeDelta: {x: 180, y: 180}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &888880012
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880010}
  m_CullTransparentMesh: 1
--- !u!114 &888880013
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 888880010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0.3}
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
  m_Father: {fileID: 888880011}
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

// Append the new floating joystick YAML
content += floatingJoystickYaml;

fs.writeFileSync(sceneFile, content, 'utf8');
console.log("Floating Joystick setup successfully updated in the scene!");
