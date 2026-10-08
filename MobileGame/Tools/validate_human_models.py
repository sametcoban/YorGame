"""Validate the exported FBX files in Blender, independently of generation.

blender -b --factory-startup --python-exit-code 1 --python validate_human_models.py -- MODEL_DIRECTORY
This does not substitute for Unity import, Play mode, or device validation.
"""
import sys, json
from pathlib import Path
import bpy
folder=Path(sys.argv[sys.argv.index('--')+1])
states={'Idle','Attack','Cast','Dodge','Parry','Hit','Death'}
reports=[]
for gender in ['Man','Woman']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    path=folder/('Human_'+gender+'.fbx');assert path.is_file(),path
    bpy.ops.import_scene.fbx(filepath=str(path))
    rigs=[obj for obj in bpy.context.scene.objects if obj.type=='ARMATURE']
    assert len(rigs)==1, 'One skeleton per human required'
    rig=rigs[0]
    assert {'head','hand_r','hand_l','pelvis'}.issubset({b.name for b in rig.data.bones})
    clips={a.name.split('|')[-1]:a for a in bpy.data.actions}
    assert states.issubset(clips), 'Missing animation states: '+str(states-set(clips))
    for state in states:
        assert clips[state].frame_range[1]>clips[state].frame_range[0], 'Empty animation: '+state
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    assert len(meshes)==3, 'Expect body and two optional robe panels'
    assert any(obj.name.startswith('HumanMesh') for obj in meshes)
    assert all(obj.vertex_groups for obj in meshes), 'Unweighted clothing/mesh'
    assert all(any(mod.type=='ARMATURE' and mod.object==rig for mod in obj.modifiers) for obj in meshes)
    roles={mat.name.split('.')[0] for obj in meshes for mat in obj.data.materials if mat}
    assert {'Skin','Lips','Cloth','Leather','Hair','Metal','EyeWhite','Iris','Pupil'}.issubset(roles),roles
    triangles=sum(len(p.vertices)-2 for obj in meshes for p in obj.data.polygons)
    assert triangles<50000, 'Prototype human exceeded mesh budget'
    reports.append({'model':path.name,'triangles':triangles,'skeletons':len(rigs),'states':sorted(states),'materials':sorted(roles)})
print('FBX_VALIDATION_PASSED '+json.dumps(reports))
