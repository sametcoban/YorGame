"""Reuse the bundled CC0-derived rigs and animations; author named Knight armor details.
blender -b --factory-startup --python-exit-code 1 --python Tools/build_named_knights.py -- Assets/Art/DarkFantasy /tmp/knight-preview
"""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; root=Path(args[0]).resolve(); preview=Path(args[1]).resolve();preview.mkdir(parents=True,exist_ok=True)
looks=json.loads((root/'Named/Knights.json').read_text())['looks'];reports=[]
def color(value): return tuple(int(value[i:i+2],16)/255 for i in (1,3,5))
for look in looks:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(root/'Models'/('Knight_'+look['gender']+'.fbx')))
    for action in bpy.data.actions: action.name=action.name.split('|')[-1]
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    rig.animation_data.action=None
    materials={m.name.split('.')[0]:m for m in body.data.materials}
    for role,material in materials.items():
        value=look['armor'] if role in ('Iron','Blade') else look['cloth'] if role.endswith('Wool') else look['trim'] if role=='Brass' else look['glow'] if role=='Ember' else None
        if value:
            material.diffuse_color=(*color(value),1)
            if material.use_nodes:
                shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=material.diffuse_color
                if role=='Ember': shader.inputs['Emission Color'].default_value=material.diffuse_color;shader.inputs['Emission Strength'].default_value=2
    def part(name,position,size,bone,role='Brass',rotation=0):
        bpy.ops.mesh.primitive_cube_add(size=1,location=position)
        obj=bpy.context.object;obj.name=name;obj.scale=size;obj.rotation_euler.y=rotation
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        obj.parent=rig;obj.data.materials.append(materials[role]);group=obj.vertex_groups.new(name=bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        modifier=obj.modifiers.new('Named armor rig','ARMATURE');modifier.object=rig
        bevel=obj.modifiers.new('Forged edge','BEVEL');bevel.width=.006;bevel.segments=1;bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=bevel.name)
        return obj
    head=rig.data.bones['head'].head_local; feature=look['feature']
    if feature=='ember':
        part('Ember brow',head+Vector((0,-.125,.14)),(.13,.018,.04),'head','Brass')
    elif feature=='crest':
        part('Veteran helmet crest',head+Vector((0,0,.29)),(.055,.20,.19),'head','Brass')
    else:
        for side in (-1,1):
            if feature=='wing':
                for feather in range(3): part('Winged crown',head+Vector((side*(.105+feather*.028),.01,.21+feather*.02)),(.025,.085,.12+feather*.025),'head','Brass',side*(.45+feather*.18))
            else:
                part('Thorn crest' if feature=='thorn' else 'Dread crest',head+Vector((side*.12,0,.23)),(.035,.055,.20 if feature=='dread' else .12),'head','Iron',side*.4)
    for side,label in [(-1,'r'),(1,'l')]:
        shoulder=rig.data.bones['upperarm_'+label].head_local
        if feature in ('thorn','dread'):
            for spike in range(2 if feature=='thorn' else 3): part('Shoulder thorn',shoulder+Vector((side*(.06+spike*.045),0,.065)),(.035,.045,.14),'upperarm_'+label,'Brass',side*(.45+spike*.18))
        elif feature=='wing':
            for feather in range(3): part('Wing pauldron',shoulder+Vector((side*(.06+feather*.045),0,.03)),(.045,.13,.10+feather*.025),'upperarm_'+label,'Brass',side*(.2+feather*.2))
        elif feature=='crest': part('Bronze mantle edge',shoulder+Vector((side*.1,-.025,.02)),(.12,.24,.05),'upperarm_'+label,'Brass')
    chest=rig.data.bones['spine_03'].head_local
    part('Named chest rune',chest+Vector((0,-.165,.03)),(.055,.025,.075),'spine_03','Ember')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes: obj.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
    # Consolidated mesh and shared rig retain the seven original animation actions.
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
    rig.name=look['id']+'Skeleton'
    bpy.ops.object.select_all(action='SELECT')
    name='Named_'+look['id'];path=root/'Models'/(name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False)
    idle=next(a for a in bpy.data.actions if a.name.split('|')[-1]=='Idle');rig.animation_data.action=idle;bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(preview/(name+'.blend')))
    reports.append({'id':look['id'],'gender':look['gender'],'feature':feature,'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'states':sorted(a.name.split('|')[-1] for a in bpy.data.actions)})
(root/'Named/MODEL_REPORT.json').write_text(json.dumps(reports,indent=2)+'\n')
print('NAMED_KNIGHTS',json.dumps(reports))
