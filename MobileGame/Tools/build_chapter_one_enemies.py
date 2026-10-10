"""Original chapter-one enemy art, using existing CC0 humanoid outputs and an original beast.
blender -b --factory-startup --python-exit-code 1 --python Tools/build_chapter_one_enemies.py -- REFERENCE_DIR ROSTER_DIR OUTPUT
Run build_dark_fantasy.py for the reference and roster first. Blender 4.3.2.
"""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Vector, Euler, Quaternion
args=sys.argv[sys.argv.index('--')+1:]
reference,roster,out=map(Path,args[:3]);out.mkdir(parents=True,exist_ok=True)
states=['Idle','Attack','Cast','Dodge','Parry','Hit','Death','Windup']

def material(name,color,metal=0,emission=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    s=m.node_tree.nodes['Principled BSDF'];s.inputs['Base Color'].default_value=m.diffuse_color
    s.inputs['Metallic'].default_value=metal;s.inputs['Roughness'].default_value=.7
    s.inputs['Emission Color'].default_value=(*color,1);s.inputs['Emission Strength'].default_value=emission
    return m

def bind(obj,bone,mat):
    obj.parent=rig;obj.data.materials.clear();obj.data.materials.append(mat)
    group=obj.vertex_groups.new(name=bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    obj.modifiers.new('Skeleton','ARMATURE').object=rig
    return obj

def box(name,p,size,bone,mat,rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.scale=size
    if rotation:o.rotation_mode='QUATERNION';o.rotation_quaternion=rotation
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bevel=o.modifiers.new('Chipped edges','BEVEL');bevel.width=.008;bevel.segments=1;bpy.ops.object.modifier_apply(modifier=bevel.name)
    return bind(o,bone,mat)

def rod(name,a,b,width,bone,mat):
    a,b=Vector(a),Vector(b)
    return box(name,(a+b)*.5,(width,width,(b-a).length),bone,mat,(b-a).to_track_quat('Z','Y'))

def sphere(name,p,size,bone,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10,ring_count=5,location=p);o=bpy.context.object;o.name=name;o.scale=size
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);return bind(o,bone,mat)

def spike(name,a,b,radius,bone,mat):
    a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cone_add(vertices=6,radius1=radius,radius2=0,depth=(b-a).length,location=(a+b)*.5)
    o=bpy.context.object;o.name=name;o.rotation_mode='QUATERNION';o.rotation_quaternion=(b-a).to_track_quat('Z','Y')
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);return bind(o,bone,mat)

def pose_action(name,duration,pose,baseline=None,peak=None,death=False):
    existing=bpy.data.actions.get(name)
    if existing:bpy.data.actions.remove(existing)
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data_create();rig.animation_data.action=action
    for frame,factor in [(1,0),(peak or duration//2,1),(duration,1 if death else 0)]:
        for bone in rig.pose.bones:
            bone.rotation_mode='QUATERNION'
            delta=Euler(tuple(math.radians(v)*factor for v in pose.get(bone.name,(0,0,0))),'XYZ').to_quaternion()
            bone.rotation_quaternion=(baseline or {}).get(bone.name,Quaternion())@delta
            bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)
    return action

def load_human(path):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    skeleton=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    body=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    bpy.context.scene.frame_set(1)
    baseline={b.name:b.rotation_quaternion.copy() for b in skeleton.pose.bones}
    skeleton.animation_data.action=None
    for bone in skeleton.pose.bones:bone.rotation_quaternion=Quaternion()
    return skeleton,body,baseline

reports=[]
for name in ['LanternWarden','AshHound','CinderScout','IronWatcher','Roadkeeper','HollowMatron']:
    if name=='AshHound':
        bpy.ops.wm.read_factory_settings(use_empty=True)
        data=bpy.data.armatures.new('AshHoundSkeleton');rig=bpy.data.objects.new('AshHoundSkeleton',data);bpy.context.collection.objects.link(rig)
        bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
        bone_specs=[('root',(0,0,0),(0,0,.2),None),('spine',(0,.35,.8),(0,-.3,.88),'root'),('head',(0,-.3,.9),(0,-.8,1.02),'spine'),('jaw',(0,-.65,.98),(0,-.95,.87),'head'),('tail',(0,.4,.82),(0,.82,1.04),'spine'),('tail_tip',(0,.82,1.04),(0,1.12,1.02),'tail')]
        for side,sign in [('l',1),('r',-1)]:
            for end,y in [('front',-.30),('hind',.34)]:
                upper=end+'_upper_'+side;lower=end+'_lower_'+side
                bone_specs.extend([(upper,(sign*.19,y,.83),(sign*.22,y+.06,.40),'spine'),(lower,(sign*.22,y+.06,.40),(sign*.22,y-.04,.09),upper)])
        for bone_name,a,b,parent in bone_specs:
            bone=data.edit_bones.new(bone_name);bone.head=a;bone.tail=b
            if parent:bone.parent=data.edit_bones[parent]
        bpy.ops.object.mode_set(mode='OBJECT')
        iron=material('Iron',(.07,.08,.09),.45);bone_mat=material('Bone',(.30,.28,.22));recess=material('Recess',(.012,.012,.016));ember=material('Ember',(.9,.27,.025),emission=3)
        body=sphere('Charred ribcage',(0,.03,.82),(.24,.52,.22),'spine',recess)
        rod('Exposed spine',(0,-.36,1.02),(0,.46,1.01),.055,'spine',bone_mat)
        # Open angular ribs and a broken plated hide, rather than a furry pet silhouette.
        for y in [-.29,-.15,0,.15,.29,.42]:
            for side in [-1,1]:
                pts=[(0,y,1.02),(side*.22,y,.95),(side*.27,y,.78),(side*.16,y,.64)]
                for a,b in zip(pts,pts[1:]):rod('Exposed rib',a,b,.028,'spine',bone_mat)
            spike('Dorsal thorn',(0,y,1.01),(0,y+.06,1.18),.045,'spine',iron)
        sphere('Wolf skull',(0,-.63,1.04),(.19,.26,.16),'head',bone_mat)
        box('Nasal bridge',(0,-.86,1.035),(.16,.32,.12),'head',bone_mat)
        box('Nose',(0,-1.03,1.035),(.15,.05,.08),'head',recess)
        box('Lower jaw',(0,-.83,.90),(.16,.31,.045),'jaw',bone_mat)
        for side in [-1,1]:
            sphere('Eye hollow',(side*.153,-.72,1.10),(.046,.052,.045),'head',recess)
            sphere('Burning eye',(side*.179,-.73,1.10),(.018,.026,.023),'head',ember)
            spike('Torn ear',(side*.12,-.51,1.15),(side*.16,-.46,1.40),.06,'head',iron)
            spike('Skull fang',(side*.067,-.89,.99),(side*.067,-.91,.86),.025,'head',bone_mat)
        for side,sign in [('l',1),('r',-1)]:
            for end,y in [('front',-.30),('hind',.34)]:
                upper=end+'_upper_'+side;lower=end+'_lower_'+side
                rod('Upper leg',(sign*.19,y,.83),(sign*.22,y+.06,.4),.075,upper,iron)
                rod('Shin bone',(sign*.22,y+.06,.4),(sign*.22,y-.04,.09),.047,lower,bone_mat)
                sphere('Joint',(sign*.22,y+.06,.4),(.055,.055,.055),lower,iron)
                box('Paw',(sign*.22,y-.095,.075),(.12,.19,.09),lower,iron)
                for i in [-1,0,1]:spike('Claw',(sign*.22+i*.038,y-.17,.08),(sign*.22+i*.038,y-.23,.03),.016,lower,bone_mat)
        rod('Tail',(0,.4,.82),(0,.82,1.04),.065,'tail',iron);spike('Tail thorn',(0,.82,1.04),(0,1.12,1.02),.07,'tail_tip',bone_mat)
        motions={'Idle':(60,{'spine':(1,0,0),'tail':(0,0,5)}),'Attack':(26,{'head':(18,0,0),'jaw':(28,0,0),'front_upper_l':(-20,0,0),'front_upper_r':(-20,0,0)}),'Cast':(26,{'head':(-10,0,0),'jaw':(20,0,0)}),'Dodge':(20,{'spine':(0,0,12)}),'Parry':(20,{'head':(-12,0,0)}),'Hit':(18,{'head':(-15,0,5),'spine':(0,0,-8)}),'Death':(38,{'root':(0,0,78),'head':(20,0,0)}),'Windup':(36,{'head':(-15,0,0),'jaw':(15,0,0),'front_upper_l':(15,0,0),'front_upper_r':(15,0,0)})}
        for state,(duration,pose) in motions.items():pose_action(state,duration,pose,death=state=='Death')
    else:
        source=reference/'LanternWarden.blend' if name in ['LanternWarden','Roadkeeper'] else roster/('Sorceress_Woman.blend' if name=='HollowMatron' else 'Sorceress_Man.blend' if name=='CinderScout' else 'Knight_Man.blend')
        rig,body,baseline=load_human(source)
        palette={m.name.split('.')[0]:m for m in body.data.materials if m}
        iron,brass=palette['Iron'],palette['Brass']
        ember=palette.get('Ember') or palette.get('Arcane')
        h=max(v.co.z for v in body.data.vertices)
        if name in ['CinderScout','HollowMatron']:
            frost=material('Frost',(.15,.42,.65),emission=2)
            ash=material('SootWool',(.035,.04,.045))
            for i,m in enumerate(body.data.materials):
                if m.name.split('.')[0]=='Arcane':body.data.materials[i]=frost
                if m.name.split('.')[0]=='VioletWool':body.data.materials[i]=ash
            # Ragged shoulder wrappings and hanging talismans distinguish the enemy caster.
            for side in [-1,1]:
                rod('Bone talisman',(side*.12,-.17,h*.76),(side*.14,-.18,h*.67),.025,'spine_03',brass)
            pose_action('Attack',28,{'upperarm_r':(-30,0,-25),'lowerarm_r':(-30,0,0),'upperarm_l':(-35,0,25),'spine_03':(-8,0,-8)},baseline,peak=13)
            pose_action('Windup',36,{'upperarm_l':(-20,0,15),'lowerarm_l':(-35,0,0),'spine_03':(-5,0,0)},baseline)
        elif name=='IronWatcher':
            rust=material('RustIron',(.12,.075,.05),metal=.55)
            for i,m in enumerate(body.data.materials):
                if m.name.split('.')[0]=='Iron':body.data.materials[i]=rust
            for side in ['l','r']:
                p=rig.data.bones['upperarm_'+side].head_local
                sign=1 if side=='l' else -1
                for i in range(3):spike('Broken shoulder thorn',p+Vector((sign*.06,0,.02)),p+Vector((sign*(.16+i*.04),i*.025,.12+i*.04)),.025,'upperarm_'+side,rust)
            # Extend only the blade vertices rigidly attached above the sword hand.
            hand=rig.data.bones['hand_r'].head_local;group=body.vertex_groups['hand_r'].index
            for v in body.data.vertices:
                if v.co.z>hand.z+.20 and any(g.group==group and g.weight>.99 for g in v.groups):v.co.z=hand.z+(v.co.z-hand.z)*1.25
            pose_action('Attack',30,{'upperarm_r':(-30,0,-65),'lowerarm_r':(-40,0,0),'spine_03':(6,12,-20)},baseline,peak=13)
            pose_action('Windup',36,{'upperarm_r':(-15,0,-35),'lowerarm_r':(-35,0,0),'spine_03':(0,0,-12)},baseline)
        else:
            for side in ['l','r']:
                p=rig.data.bones['upperarm_'+side].head_local;sign=1 if side=='l' else -1
                if name=='Roadkeeper':
                    box('Boss mantle plate',p+Vector((sign*.055,0,-.01)),(.34,.29,.13),'upperarm_'+side,iron)
                    for i in range(3):spike('Mantle spikes',p+Vector((sign*.1,.02+i*.04,.07)),p+Vector((sign*.20,.03+i*.04,.26+i*.025)),.035,'upperarm_'+side,brass)
            if name=='Roadkeeper':
                for i in range(5):
                    x=(i-2)*.045;spike('Broken crown',(x,.045,h*.91),(x*1.4,.045,h*1.02+(.07 if i==2 else 0)),.027,'head',brass)
                box('Boss breastplate',(0,-.19,h*.70),(.38,.05,.12),'spine_02',iron)
                pose_action('Attack',30,{'upperarm_r':(-45,0,-65),'lowerarm_r':(-55,0,0),'spine_03':(14,0,-15)},baseline,peak=13)
                pose_action('Windup',36,{'upperarm_r':(-45,0,-30),'lowerarm_r':(-50,0,0),'spine_03':(-10,0,-12)},baseline)
            else:
                pose_action('Attack',28,{'upperarm_r':(-25,0,-50),'lowerarm_r':(-35,0,0),'spine_03':(3,10,-18)},baseline,peak=13)
                pose_action('Windup',36,{'upperarm_r':(-15,0,-25),'spine_03':(0,0,-8)},baseline)
    # Reset to bind pose before consolidating/exporting.
    rig.animation_data.action=None
    for bone in rig.pose.bones:bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH':obj.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name=name;rig.name=name+'Skeleton'
    old=list(body.data.materials);palette=[];lookup={};indices=[]
    for face in body.data.polygons:
        m=old[face.material_index];key=m.as_pointer()
        if key not in lookup:lookup[key]=len(palette);palette.append(m)
        indices.append(lookup[key])
    body.data.materials.clear()
    for m in palette:body.data.materials.append(m)
    for face,index in zip(body.data.polygons,indices):face.material_index=index
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.scene.render.fps=30;bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False)
    rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'.blend')))
    reports.append({'model':name+'.fbx','triangles':sum(len(f.vertices)-2 for f in body.data.polygons),'bones':len(rig.data.bones),'material_slots':len(palette),'materials':[m.name.split('.')[0] for m in palette],'states':states})
(out/'enemy-report.json').write_text(json.dumps(reports,indent=2)+'\n');print('CHAPTER_ONE_ENEMIES_BUILT',json.dumps(reports))
