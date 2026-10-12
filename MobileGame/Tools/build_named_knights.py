"""Reuse the bundled CC0-derived rigs and animations; author named hero armor details.
blender -b --factory-startup --python-exit-code 1 --python Tools/build_named_knights.py -- Assets/Art/DarkFantasy /tmp/knight-preview
Pass --all-new for the 25 other heroes, or --paladins for five Paladins.
"""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; root=Path(args[0]).resolve(); preview=Path(args[1]).resolve();preview.mkdir(parents=True,exist_ok=True)
groups=['Paladins','Sorceresses','Rangers','Rogues','Clerics'] if '--all-new' in args else ['Paladins'] if '--paladins' in args else ['Knights']
looks=[look for group in groups for look in json.loads((root/('Named/'+group+'.json')).read_text())['looks']];reports=[]
def color(value): return tuple(int(value[i:i+2],16)/255 for i in (1,3,5))
for look in looks:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(root/'Models'/(look['id'].split('_')[0]+'_'+look['gender']+'.fbx')))
    for action in bpy.data.actions: action.name=action.name.split('|')[-1]
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    rig.animation_data.action=None
    materials={m.name.split('.')[0]:m for m in body.data.materials}
    for role,material in materials.items():
        value=look['armor'] if role in ('Iron','Blade') else look['cloth'] if role.endswith('Wool') else look['trim'] if role=='Brass' else look['glow'] if role in ('Ember','Light','Arcane','Venom','Frost') else None
        if value:
            material.diffuse_color=(*color(value),1)
            if material.use_nodes:
                shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=material.diffuse_color
                if role in ('Ember','Light','Arcane','Venom','Frost'): shader.inputs['Emission Color'].default_value=material.diffuse_color;shader.inputs['Emission Strength'].default_value=2
    def part(name,position,size,bone,role='Brass',rotation=0):
        bpy.ops.mesh.primitive_cube_add(size=1,location=position)
        obj=bpy.context.object;obj.name=name;obj.scale=size;obj.rotation_euler.y=rotation
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        obj.parent=rig;obj.data.materials.append(materials[role]);group=obj.vertex_groups.new(name=bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        modifier=obj.modifiers.new('Named armor rig','ARMATURE');modifier.object=rig
        bevel=obj.modifiers.new('Forged edge','BEVEL');bevel.width=.006;bevel.segments=1;bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=bevel.name)
        return obj
    head=rig.data.bones['head'].head_local; feature=look['feature']
    if feature in ('cross','sun','bastion','wingsun','halo','crescent','crystal','stars','crown','feather','antler','mask'):
        if feature=='mask':
            part('Shadow visor',head+Vector((0,-.13,.12)),(.17,.02,.05),'head','Brass')
        elif feature in ('cross','bastion','crystal'):
            part('Crest finial',head+Vector((0,0,.27)),(.045,.075,.18),'head','Brass')
            if feature=='cross':part('Crest crossbar',head+Vector((0,0,.30)),(.15,.045,.035),'head','Brass')
            if feature=='bastion':part('Crest crown plate',head+Vector((0,0,.21)),(.21,.08,.045),'head','Iron')
        elif feature in ('sun','halo','crescent','stars'):
            count=12 if feature!='stars' else 5
            for i in range(count):
                angle=(i/max(1,count-1)*math.pi if feature=='crescent' else i/count*math.pi*2)
                part('Radiant crown',head+Vector((math.cos(angle)*.19,.07,.21+math.sin(angle)*.19)),(.045,.035,.065),'head','Brass',angle)
        else:
            for side in (-1,1):
                for feather in range(2 if feature in ('antler','feather') else 3):
                    part('Named crown plume',head+Vector((side*(.10+feather*.035),.015,.22+feather*.035)),(.025,.075,.12+feather*.025),'head','Brass',side*(.3+feather*.22))
    elif feature=='ember':
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
        if feature in ('cross','sun','bastion','wingsun','halo','crescent','crystal','stars','crown','feather','antler','mask'):
            for layer in range(1 if feature in ('mask','feather') else 2):
                part('Named shoulder edge',shoulder+Vector((side*(.07+layer*.04),0,.04+layer*.04)),(.055,.17,.09),'upperarm_'+label,'Brass',side*(.15+layer*.25))
        elif feature in ('thorn','dread'):
            for spike in range(2 if feature=='thorn' else 3): part('Shoulder thorn',shoulder+Vector((side*(.06+spike*.045),0,.065)),(.035,.045,.14),'upperarm_'+label,'Brass',side*(.45+spike*.18))
        elif feature=='wing':
            for feather in range(3): part('Wing pauldron',shoulder+Vector((side*(.06+feather*.045),0,.03)),(.045,.13,.10+feather*.025),'upperarm_'+label,'Brass',side*(.2+feather*.2))
        elif feature=='crest': part('Bronze mantle edge',shoulder+Vector((side*.1,-.025,.02)),(.12,.24,.05),'upperarm_'+label,'Brass')
    chest=rig.data.bones['spine_03'].head_local
    glow_role=next(role for role in ('Ember','Light','Arcane','Venom','Frost') if role in materials)
    part('Named chest rune',chest+Vector((0,-.165,.03)),(.055,.025,.075),'spine_03',glow_role)
    hand=rig.data.bones['hand_l'].head_local
    if look['id'].startswith('Paladin_'):
        seal=hand+Vector((0,-.095,-.015))
        if feature in ('sun','halo','wingsun'):
            for i in range(8):
                angle=i*math.pi/4;part('Shield sun seal',seal+Vector((math.cos(angle)*.09,0,math.sin(angle)*.09)),(.03,.02,.04),'hand_l','Brass',angle)
        else:
            part('Shield holy seal',seal,(.06,.025,.22),'hand_l',glow_role)
            part('Shield holy cross',seal,(.18,.025,.035),'hand_l','Brass')
        if feature=='bastion':
            for side in (-1,1):part('Tower shield rim',hand+Vector((side*.14,-.065,-.01)),(.03,.035,.48),'hand_l','Iron')
    elif look['id'].startswith('Ranger_'):
        for i in range(2):part('Quiver feather',chest+Vector((.17+i*.04,.13,.24)),(.025,.04,.13),'spine_03','Brass',.25)
    elif look['id'].startswith(('Sorceress_','Cleric_')):
        part('Focus hand seal',hand+Vector((0,-.06,.06)),(.075,.035,.11),'hand_l',glow_role)
    elif look['id'].startswith('Rogue_'):
        part('Shadow shoulder rune',chest+Vector((.13,-.13,.06)),(.04,.025,.075),'spine_03',glow_role)
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
(root/('Named/ROSTER_MODEL_REPORT.json' if '--all-new' in args else 'Named/PALADIN_MODEL_REPORT.json' if '--paladins' in args else 'Named/MODEL_REPORT.json')).write_text(json.dumps(reports,indent=2)+'\n')
print('NAMED_HERO_MODELS',json.dumps(reports))
