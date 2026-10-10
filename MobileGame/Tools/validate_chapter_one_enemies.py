"""Independent FBX import and animation checks; does not verify Unity playback."""
import bpy,sys,json,math
from pathlib import Path
folder=Path(sys.argv[sys.argv.index('--')+1]);reports=[]
names=['LanternWarden','AshHound','CinderScout','IronWatcher','Roadkeeper','HollowMatron']
states={'Idle','Attack','Cast','Dodge','Parry','Hit','Death','Windup'}
for name in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(folder/(name+'.fbx')))
    rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(rigs)==1 and len(meshes)==1
    rig,mesh=rigs[0],meshes[0];bones={b.name for b in rig.data.bones}
    required={'root','spine','head','jaw','front_lower_l','hind_lower_r','tail'} if name=='AshHound' else {'head','hand_l','hand_r','calf_l','calf_r'}
    assert required.issubset(bones)
    assert any(m.type=='ARMATURE' and m.object==rig for m in mesh.modifiers)
    groups={g.index for g in mesh.vertex_groups if g.name in bones}
    assert all(any(g.group in groups and g.weight>0 for g in v.groups) for v in mesh.data.vertices)
    assert mesh.data.uv_layers and any(uv.uv.length>.01 for uv in mesh.data.uv_layers.active.data)
    assert len(mesh.data.materials)<=7
    roles={m.name.split('.')[0] for m in mesh.data.materials if m}
    assert not roles.intersection({'Skin','Lips','Hair','Iris','Pupil','EyeWhite'})
    assert ('Frost' if name in ['CinderScout','HollowMatron'] else 'Ember') in roles
    if name=='AshHound':assert 'Bone' in roles
    count=sum(len(f.vertices)-2 for f in mesh.data.polygons);assert count<18000
    clips={a.name.split('|')[-1]:a for a in bpy.data.actions};assert states.issubset(clips)
    for state in states:
        action=clips[state];assert action.frame_range[1]>action.frame_range[0]
        rig.animation_data_create();rig.animation_data.action=action
        bpy.context.scene.frame_set(round(sum(action.frame_range)/2));bpy.context.view_layer.update()
        # Catch invalid or exploding weighted meshes in each exported pose.
        evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        vertices=[evaluated.matrix_world@v.co for v in evaluated.data.vertices]
        assert all(math.isfinite(c) for v in vertices for c in v)
        assert max(max(v[i] for v in vertices)-min(v[i] for v in vertices) for i in range(3))<5
    reports.append({'model':name,'triangles':count,'bones':len(bones),'materials':len(mesh.data.materials),'states':len(states)})
print('CHAPTER_ONE_ENEMIES_VALIDATED',json.dumps(reports))
