"""Blender FBX round-trip validation, not Unity/device verification.
blender -b --factory-startup --python-exit-code 1 --python validate_dark_fantasy.py -- MODEL_DIRECTORY
"""
import bpy,sys,json
from pathlib import Path
folder=Path(sys.argv[sys.argv.index('--')+1]);reports=[]
states={'Idle','Attack','Cast','Dodge','Parry','Hit','Death'}
for name in ['Rowan','LanternWarden']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    bpy.ops.import_scene.fbx(filepath=str(folder/(name+'.fbx')))
    rigs=[obj for obj in bpy.context.scene.objects if obj.type=='ARMATURE']
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    assert len(rigs)==1 and len(meshes)==1, 'Expected consolidated weighted mesh and one rig'
    rig=rigs[0];mesh=meshes[0]
    bones={b.name for b in rig.data.bones};assert {'head','hand_r','hand_l','calf_l','calf_r'}.issubset(bones)
    assert any(m.type=='ARMATURE' and m.object==rig for m in mesh.modifiers)
    groups={g.index for g in mesh.vertex_groups if g.name in bones}
    assert all(any(g.group in groups and g.weight>0 for g in v.groups) for v in mesh.data.vertices), 'Unweighted armor vertices'
    clips={a.name.split('|')[-1]:a for a in bpy.data.actions};assert states.issubset(clips)
    assert all(clips[name].frame_range[1]>clips[name].frame_range[0] for name in states)
    assert mesh.data.uv_layers and any(uv.uv.length>.01 for uv in mesh.data.uv_layers.active.data), 'Missing UVs for surface maps'
    roles={m.name.split('.')[0] for m in mesh.data.materials if m}
    assert {'Iron','Brass','Leather','Wool','Recess','Ember'}.issubset(roles)
    assert not roles.intersection({'Skin','Lips','Hair','EyeWhite','Iris','Pupil'}), 'Realistic face materials leaked into stylized reference'
    count=sum(len(f.vertices)-2 for f in mesh.data.polygons);assert count<25000, 'Exceeded reference mesh budget'
    reports.append({'model':name,'triangles':count,'states':sorted(states),'weighted_vertices':len(mesh.data.vertices)})
print('DARK_FANTASY_VALIDATION_PASSED '+json.dumps(reports))
