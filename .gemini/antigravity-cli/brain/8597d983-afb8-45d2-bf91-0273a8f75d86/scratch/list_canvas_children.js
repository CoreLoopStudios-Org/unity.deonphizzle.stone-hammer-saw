const fs = require('fs');
const path = require('path');

function inspectCanvasChildren(scenePath) {
    console.log(`\n================ INSPECTING CANVAS: ${path.basename(scenePath)} ================`);
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

    const gameobjects = {};
    const transforms = {};

    for (const [fid, [cid, obj]] of Object.entries(objects)) {
        if (cid === 1) {
            gameobjects[fid] = {
                name: (obj['m_Name'] || 'Unnamed').replace(/"/g, ''),
                file_id: fid,
                transform_id: null,
                active: obj['m_IsActive'] !== '0'
            };
        } else if ([4, 224].includes(cid)) {
            const goMatch = (obj['m_GameObject'] || '').match(/fileID:\s*(-?\d+)/);
            const goId = goMatch ? goMatch[1] : null;
            const fatherMatch = (obj['m_Father'] || '').match(/fileID:\s*(-?\d+)/);
            const fatherId = fatherMatch ? fatherMatch[1] : '0';

            transforms[fid] = {
                file_id: fid,
                go_id: goId,
                father_id: fatherId,
                children: []
            };
        }
    }

    for (const [fid, trans] of Object.entries(transforms)) {
        const fatherId = trans.father_id;
        if (fatherId && fatherId !== '0' && transforms[fatherId]) {
            transforms[fatherId].children.push(trans.file_id);
        }
    }

    for (const [fid, trans] of Object.entries(transforms)) {
        const goId = trans.go_id;
        if (goId && gameobjects[goId]) {
            gameobjects[goId].transform_id = fid;
        }
    }

    // Find Canvas
    let canvasGoId = null;
    for (const [goId, go] of Object.entries(gameobjects)) {
        if (go.name === 'Canvas') {
            canvasGoId = goId;
            break;
        }
    }

    if (!canvasGoId) {
        console.log("Canvas not found in the scene.");
        return;
    }

    const canvasTransId = gameobjects[canvasGoId].transform_id;
    console.log(`Canvas GameObject ID: ${canvasGoId}, RectTransform ID: ${canvasTransId}`);

    function printNode(goId, indent = 0) {
        const go = gameobjects[goId];
        if (!go) return;
        const activeStr = go.active ? '' : ' (INACTIVE)';
        console.log('  '.repeat(indent) + `|- ${go.name}${activeStr} (GO_ID: ${goId}, Trans_ID: ${go.transform_id})`);
        
        const trans = transforms[go.transform_id];
        if (trans && trans.children) {
            for (const childTransId of trans.children) {
                const childTrans = transforms[childTransId];
                if (childTrans && childTrans.go_id) {
                    printNode(childTrans.go_id, indent + 1);
                }
            }
        }
    }

    printNode(canvasGoId);
}

inspectCanvasChildren("C:\\Users\\User\\Documents\\GitHub\\unity.deonphizzle.stone-hammer-saw\\Assets\\Scenes\\Mob Squad 3d world scene.unity");
