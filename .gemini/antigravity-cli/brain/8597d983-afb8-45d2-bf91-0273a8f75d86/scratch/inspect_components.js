const fs = require('fs');
const path = require('path');

const guidMap = {
    '18be12ac5c27c94408ee8e2a8fa64086': 'ThirdPersonCharacterController',
    'd8d33d9865f5ae844945d3b8b78c60eb': 'ThirdPersonCameraController',
    '474017d5642d59e40bcf8e46a9e6150f': 'SceneOrientationController'
};

function inspectScene(scenePath) {
    console.log(`\n================ INSPECTING: ${path.basename(scenePath)} ================`);
    const fileContent = fs.readFileSync(scenePath, 'utf8');
    const lines = fileContent.split(/\r?\n/);
    
    const docHeaderRe = /^---\s+!u!(\d+)\s+&(-?\d+)/;
    const objects = {};
    let currentObj = null;
    let currentObjId = null;
    let currentClassId = null;

    for (const line of lines) {
        const headerMatch = line.match(docHeaderRe);
        if (headerMatch) {
            if (currentObj !== null) {
                objects[currentObjId] = [currentClassId, currentObj];
            }
            currentClassId = parseInt(headerMatch[1], 10);
            currentObjId = headerMatch[2];
            currentObj = { _class_id: currentClassId, _file_id: currentObjId, _fields: [] };
            continue;
        }

        if (currentObj !== null) {
            currentObj._fields.push(line);
            const parts = line.split(':');
            if (parts.length >= 2) {
                const key = parts[0].trim();
                const val = parts.slice(1).join(':').trim();
                currentObj[key] = val;
            }
        }
    }
    if (currentObj !== null) {
        objects[currentObjId] = [currentClassId, currentObj];
    }

    // Look for GameObjects to map IDs to Names
    const goNames = {};
    for (const [fid, [cid, obj]] of Object.entries(objects)) {
        if (cid === 1) {
            goNames[fid] = (obj['m_Name'] || 'Unnamed').replace(/"/g, '');
        }
    }

    // Look for Transform to map IDs to GameObject Names
    const transToGoName = {};
    for (const [fid, [cid, obj]] of Object.entries(objects)) {
        if ([4, 224].includes(cid)) {
            const goMatch = (obj['m_GameObject'] || '').match(/fileID:\s*(-?\d+)/);
            if (goMatch) {
                const goId = goMatch[1];
                transToGoName[fid] = goNames[goId] || `GO_${goId}`;
            }
        }
    }

    // Look for MonoBehaviours
    for (const [fid, [cid, obj]] of Object.entries(objects)) {
        if (cid === 114) {
            const scriptGuidMatch = (obj['m_Script'] || '').match(/guid:\s*([a-fA-F0-9]{32})/);
            const scriptGuid = scriptGuidMatch ? scriptGuidMatch[1] : null;
            const scriptName = scriptGuid ? guidMap[scriptGuid] : null;
            
            if (scriptName) {
                const goMatch = (obj['m_GameObject'] || '').match(/fileID:\s*(-?\d+)/);
                const goName = goMatch ? (goNames[goMatch[1]] || 'Unknown') : 'Unknown';
                console.log(`\nScript: ${scriptName} (FileID: ${fid}) attached to GameObject: "${goName}"`);
                
                // Print all serialized fields of this behaviour
                console.log("Fields:");
                for (const fLine of obj._fields) {
                    if (fLine.trim() && !fLine.startsWith('MonoBehaviour:') && !fLine.startsWith('  m_ObjectHideFlags:') && !fLine.startsWith('  m_CorrespondingSourceObject:') && !fLine.startsWith('  m_PrefabInstance:') && !fLine.startsWith('  m_PrefabAsset:') && !fLine.startsWith('  m_GameObject:') && !fLine.startsWith('  m_Enabled:') && !fLine.startsWith('  m_Script:') && !fLine.startsWith('  m_EditorClassIdentifier:')) {
                        let formattedLine = fLine;
                        // Replace transform/gameobject fileIDs with names if possible
                        const fileIdMatch = fLine.match(/fileID:\s*(-?\d+)/);
                        if (fileIdMatch) {
                            const refId = fileIdMatch[1];
                            const nameRef = goNames[refId] || transToGoName[refId];
                            if (nameRef) {
                                formattedLine = `${fLine}  -> (Resolves to: "${nameRef}")`;
                            }
                        }
                        console.log(formattedLine);
                    }
                }
            }
        }
    }
}

inspectScene("C:\\Users\\User\\Documents\\GitHub\\unity.deonphizzle.stone-hammer-saw\\Assets\\Scenes\\Mob Squad 3d world scene.unity");
inspectScene("C:\\Users\\User\\Documents\\GitHub\\unity.deonphizzle.stone-hammer-saw\\Assets\\Mov Squad 3d world scene.unity");
