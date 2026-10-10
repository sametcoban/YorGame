"""Original stylized armor and enemy built over the CC0 MakeHuman skeleton.

blender -b --factory-startup --python-exit-code 1 --python build_dark_fantasy.py -- Human_Man.blend OUTPUT
For the roster: pass the source blend directory, OUTPUT, then --roster.
Generate the source blends with build_human_models.py first. Blender 4.3.2.
"""
import bpy, bmesh, sys, math, json
from pathlib import Path
from mathutils import Vector, Quaternion
args=sys.argv[sys.argv.index('--')+1:]; source=Path(args[0]).resolve(); out=Path(args[1]).resolve();out.mkdir(parents=True,exist_ok=True)
roster='--roster' in args
classes=['Knight','Paladin','Sorceress','Ranger','Rogue','Cleric']
variants=[(kind,gender,False) for kind in classes for gender in ['Man','Woman']] if roster else [('Knight','Man',False),('Knight','Man',True)]

def mat(name,color,metallic=0,roughness=.7,emission=0):
    material=bpy.data.materials.new(name);material.diffuse_color=(*color,1);material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(*color,1);shader.inputs['Metallic'].default_value=metallic;shader.inputs['Roughness'].default_value=roughness
    if emission:shader.inputs['Emission Color'].default_value=(*color,1);shader.inputs['Emission Strength'].default_value=emission
    return material

def bind(obj,bone,material):
    obj.parent=rig;obj.data.materials.clear();obj.data.materials.append(material)
    group=obj.vertex_groups.new(name=bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    modifier=obj.modifiers.new('Skeleton','ARMATURE');modifier.object=rig
    return obj

def mesh(name,verts,faces,bone,material):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj)
    return bind(obj,bone,material)

def box(name,center,size,bone,material,bevel=.006,rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center);obj=bpy.context.object;obj.name=name;obj.scale=size
    if rotation:obj.rotation_mode='QUATERNION';obj.rotation_quaternion=rotation
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if bevel>0:
        modifier=obj.modifiers.new('Forged edges','BEVEL');modifier.width=bevel;modifier.segments=1
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return bind(obj,bone,material)

def sphere(name,center,size,bone,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,location=center);obj=bpy.context.object;obj.name=name;obj.scale=size
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);return bind(obj,bone,material)

def rings(name,center,levels,bone,material,segments=12):
    verts=[]
    for z,rx,ry in levels:
        for i in range(segments):
            angle=math.tau*i/segments;verts.append((center[0]+rx*math.cos(angle),center[1]+ry*math.sin(angle),z))
    faces=[tuple(range(segments-1,-1,-1))]
    for j in range(len(levels)-1):
        for i in range(segments):faces.append((j*segments+i,j*segments+(i+1)%segments,(j+1)*segments+(i+1)%segments,(j+1)*segments+i))
    faces.append(tuple(range((len(levels)-1)*segments,len(levels)*segments)))
    return mesh(name,verts,faces,bone,material)

def weapon(hand,warden):
    p=rig.data.bones[hand].head_local.copy();p.y-=.035
    box('Wrapped grip',p,(.027,.027,.18),hand,leather)
    if warden:
        box('Halberd haft',p+Vector((0,0,.15)),(.032,.032,1.5),hand,leather)
        box('Halberd socket',p+Vector((0,0,.77)),(.06,.045,.12),hand,brass)
        mesh('Halberd blade',[(p.x+.015,p.y-.02,p.z+.70),(p.x+.24,p.y-.02,p.z+.86),(p.x+.16,p.y-.02,p.z+.98),(p.x+.015,p.y-.02,p.z+.90),
            (p.x+.015,p.y+.02,p.z+.70),(p.x+.24,p.y+.02,p.z+.86),(p.x+.16,p.y+.02,p.z+.98),(p.x+.015,p.y+.02,p.z+.90)],
            [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],hand,iron)
    else:
        box('Crossguard',p+Vector((0,0,.11)),(.27,.05,.04),hand,brass)
        z=p.z+.13
        mesh('Tempered blade',[(p.x-.045,p.y,z),(p.x,p.y-.015,z),(p.x+.045,p.y,z),(p.x,p.y+.015,z),
            (p.x-.032,p.y,z+.68),(p.x,p.y-.01,z+.68),(p.x+.032,p.y,z+.68),(p.x,p.y+.01,z+.68),(p.x,p.y,z+.82)],
            [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,8),(5,6,8),(6,7,8),(7,4,8)],hand,edge)
        box('Fuller',p+Vector((0,-.016,.46)),(.01,.003,.55),hand,iron,bevel=0)
        sphere('Pommel',p+Vector((0,0,-.105)),(.025,.025,.035),hand,brass)


def rod(name,start,end,width,bone,material):
    start,end=Vector(start),Vector(end)
    return box(name,(start+end)*.5,(width,width,(end-start).length),bone,material,width*.15,(end-start).to_track_quat('Z','Y'))

def staff(kind):
    p=rig.data.bones['hand_r'].head_local.copy();p.y-=.045
    rod('Oak staff',p+Vector((0,0,-.55)),p+Vector((0,0,.98)),.035,'hand_r',leather)
    for z in [-.12,.12,.79]:box('Staff binding',p+Vector((0,0,z)),(.055,.05,.065),'hand_r',brass)
    if kind=='Sorceress':
        sphere('Arcane crystal',p+Vector((0,0,.90)),(.085,.065,.14),'hand_r',glow)
        for side in [-1,1]:
            rod('Crystal fork',p+Vector((side*.04,0,.75)),p+Vector((side*.12,0,1.02)),.024,'hand_r',iron)
        sphere('Casting focus',rig.data.bones['hand_l'].head_local+Vector((0,-.03,.12)),(.052,.052,.052),'hand_l',glow)
    else:
        # An open, octagonal reliquary halo reads differently from the mage crystal.
        center=p+Vector((0,0,.98))
        for i in range(8):
            a,b=math.tau*i/8,math.tau*(i+1)/8
            rod('Reliquary halo',center+Vector((math.cos(a)*.14,0,math.sin(a)*.14)),center+Vector((math.cos(b)*.14,0,math.sin(b)*.14)),.027,'hand_r',brass)
        box('Light seal',center,(.042,.027,.15),'hand_r',glow)
        box('Light seal bar',center,(.11,.027,.035),'hand_r',glow)
        left=rig.data.bones['hand_l'].head_local+Vector((0,-.04,-.03))
        box('Prayer codex',left,(.15,.065,.19),'hand_l',leather)
        box('Codex clasp',left+Vector((0,-.039,0)),(.035,.012,.14),'hand_l',brass)

def bow():
    p=rig.data.bones['hand_l'].head_local.copy();p.y-=.04
    points=[p+Vector((x,0,z)) for x,z in [(-.16,-.55),(-.04,-.38),(.07,-.18),(0,0),(.07,.18),(-.04,.38),(-.16,.55)]]
    for a,b in zip(points,points[1:]):rod('Recurve limb',a,b,.038,'hand_l',leather)
    rod('Bow string',points[0],points[-1],.005,'hand_l',brass)
    box('Bow grip',p,(.055,.055,.16),'hand_l',iron)
    right=rig.data.bones['hand_r'].head_local+Vector((0,-.04,0))
    rod('Readied arrow',right+Vector((0,0,-.25)),right+Vector((0,0,.42)),.008,'hand_r',leather)
    sphere('Arrow point',right+Vector((0,0,.44)),(.02,.012,.04),'hand_r',edge)
    rings('Back quiver',(.15,.13),[(h*.48,.075,.07),(h*.74,.075,.07)],'spine_03',leather,8)
    for i in range(3):
        x=.12+i*.028
        rod('Quiver arrow',(x,.13,h*.6),(x,.13,h*.83),.007,'spine_03',leather)
        box('Arrow fletching',(x,.13,h*.82),(.023,.012,.065),'spine_03',cloth)

def daggers():
    for hand in ['hand_r','hand_l']:
        p=rig.data.bones[hand].head_local+Vector((0,-.035,0))
        box('Dagger grip',p,(.028,.03,.13),hand,leather)
        box('Dagger guard',p+Vector((0,0,.08)),(.115,.04,.028),hand,brass)
        mesh('Leaf dagger',[(p.x-.033,p.y,p.z+.095),(p.x,p.y-.018,p.z+.095),(p.x+.033,p.y,p.z+.095),(p.x,p.y+.018,p.z+.095),(p.x,p.y,p.z+.40)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(0,3,2,1)],hand,edge)

def animations():
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    # Lower the A-pose arms in world space, then express it in each bone's axes.
    baseline={}
    for name,angle in [('upperarm_l',28),('upperarm_r',-28)]:
        rest=rig.data.bones[name].matrix_local.to_quaternion()
        baseline[name]=rest.inverted()@Quaternion(Vector((0,1,0)),math.radians(angle))@rest
    for bone in rig.pose.bones:
        if bone.name.startswith(('index_','middle_','ring_','pinky_')):baseline[bone.name]=Quaternion(Vector((1,0,0)),.7)
    poses={'Idle':(60,{}),'Attack':(24,{'upperarm_r':(-15,0,-55),'lowerarm_r':(-45,0,0),'spine_03':(0,8,-18)}),
        'Cast':(30,{'upperarm_l':(-35,0,30),'lowerarm_l':(-30,0,0),'spine_03':(-8,0,0)}),
        'Dodge':(18,{'spine_01':(0,-18,-12),'thigh_l':(-15,0,0),'calf_l':(25,0,0)}),
        'Parry':(20,{'upperarm_l':(-30,0,35),'lowerarm_l':(-60,0,0)}),
        'Hit':(18,{'spine_03':(12,0,8),'neck_01':(8,0,0)}),'Death':(38,{'pelvis':(-75,0,0),'thigh_l':(25,0,0),'calf_l':(40,0,0)})}
    if roster:
        if kind=='Ranger':
            poses['Attack']=(26,{'upperarm_l':(-12,0,22),'lowerarm_l':(-20,0,0),'upperarm_r':(-22,0,-35),'lowerarm_r':(-80,0,0),'spine_03':(0,0,-12)})
        elif kind=='Rogue':
            poses['Attack']=(20,{'upperarm_r':(-25,0,-50),'lowerarm_r':(-30,0,0),'upperarm_l':(-20,0,35),'spine_03':(0,12,-20)})
        elif kind in ['Sorceress','Cleric']:
            poses['Attack']=(28,{'upperarm_r':(-35,0,-25),'lowerarm_r':(-25,0,0),'spine_03':(-6,0,-10)})
            poses['Cast']=(30,{'upperarm_r':(-30,0,-20),'lowerarm_r':(-30,0,0),'upperarm_l':(-35,0,30),'spine_03':(-8,0,0)})
    rig.animation_data_create()
    for name,(duration,pose) in poses.items():
        action=bpy.data.actions.new(name);rig.animation_data.action=action
        for frame,factor in [(1,0),(duration//2,1),(duration,1 if name=='Death' else 0)]:
            for bone in rig.pose.bones:
                bone.rotation_mode='QUATERNION';rotation=pose.get(bone.name,(0,0,0))
                if name=='Idle' and bone.name=='spine_03':rotation=(1,0,0)
                from mathutils import Euler
                delta=Euler(tuple(math.radians(v)*factor for v in rotation),'XYZ').to_quaternion()
                bone.rotation_quaternion=baseline.get(bone.name,Quaternion())@delta
                bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)
        action.use_fake_user=True
    rig.animation_data.action=None
    for bone in rig.pose.bones:bone.rotation_quaternion=Quaternion()
    return baseline

report=[]
for kind,gender,warden in variants:
    model_name=kind+'_'+gender if roster else 'LanternWarden' if warden else 'Rowan'
    blend_source=source/('Human_'+gender+'.blend') if roster else source
    bpy.ops.wm.open_mainfile(filepath=str(blend_source))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rig.name=model_name+'Skeleton'
    body=next(o for o in bpy.context.scene.objects if o.name=='HumanMesh');h=max(v.co.z for v in body.data.vertices)
    for obj in list(bpy.context.scene.objects):
        if obj!=body and obj!=rig:bpy.data.objects.remove(obj,do_unlink=True)
    # The helmet is a stylized authored silhouette; no realistic face underneath.
    bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().z>h*.832],context='FACES');bm.to_mesh(body.data);bm.free()
    bpy.context.view_layer.objects.active=body
    decimate=body.modifiers.new('Mobile silhouette','DECIMATE');decimate.ratio=.55;bpy.ops.object.modifier_apply(modifier=decimate.name)
    iron=mat('Iron',(.12,.14,.17) if not warden else (.09,.095,.10),.7,.55)
    edge=mat('Blade',(.40,.43,.46),.8,.32);brass=mat('Brass',(.42,.29,.11),.65,.6)
    leather=mat('Leather',(.055,.029,.019));cloth=mat('Wool',(.028,.045,.07) if not warden else (.075,.028,.018),roughness=.9)
    black=mat('Recess',(.009,.012,.017));glow=mat('Ember',(.95,.33,.035),emission=3)
    light_armor=roster and kind in ['Sorceress','Ranger','Rogue','Cleric']
    if roster:
        cloth.name={'Knight':'Wool','Paladin':'WineWool','Sorceress':'VioletWool','Ranger':'MossWool','Rogue':'SootWool','Cleric':'AshWool'}[kind]
        colors={'Knight':(.028,.045,.07),'Paladin':(.11,.025,.035),'Sorceress':(.065,.027,.11),'Ranger':(.035,.065,.037),'Rogue':(.025,.025,.034),'Cleric':(.18,.17,.135)}
        cloth.diffuse_color=(*colors[kind],1);cloth.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=cloth.diffuse_color
        glow.name={'Sorceress':'Arcane','Rogue':'Venom','Ranger':'Venom','Cleric':'Light','Paladin':'Light'}.get(kind,'Ember')
        glow.diffuse_color=(*{'Sorceress':(.3,.42,.8),'Rogue':(.26,.52,.14),'Ranger':(.26,.52,.14),'Cleric':(.8,.64,.31),'Paladin':(.8,.64,.31)}.get(kind,(.95,.33,.035)),1)
        glow.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=glow.diffuse_color
        glow.node_tree.nodes['Principled BSDF'].inputs['Emission Color'].default_value=glow.diffuse_color
    for i,material in enumerate(body.data.materials):
        role=material.name.split('.')[0]
        body.data.materials[i]=(cloth if light_armor else iron) if role=='Cloth' else brass if role=='Metal' else leather
    # Layered shoulder plates, forearm guards, knees, and actual boot silhouettes.
    for side in ['l','r']:
        sign=1 if side=='l' else -1
        shoulder=rig.data.bones['upperarm_'+side].head_local.copy()
        for layer in range(1 if light_armor else 3 if roster and kind=='Paladin' else 2):
            box('Layered pauldron',shoulder+Vector((sign*(.035+layer*.022),0,-layer*.04)),((.17 if light_armor else .24),.23,.09 if light_armor else .115),'upperarm_'+side,leather if light_armor else iron,bevel=.018)
        forearm=rig.data.bones['lowerarm_'+side]
        center=(forearm.head_local+forearm.tail_local)*.5
        rotation=(forearm.tail_local-forearm.head_local).to_track_quat('Z','Y')
        box('Vambrace',center,(.10,.11,forearm.length*.66),'lowerarm_'+side,iron,.012,rotation)
        knee=rig.data.bones['calf_'+side].head_local
        sphere('Knee plate',knee+Vector((0,-.045,0)),(.075,.048,.085),'calf_'+side,iron)
        ankle=rig.data.bones['calf_'+side].tail_local
        middle=(knee+ankle)*.5
        rings('Faceted greave',(middle.x,middle.y),[(ankle.z+.035,.045,.058),(middle.z,.061,.075),(knee.z-.07,.075,.068)],'calf_'+side,iron,segments=8)
        center=rig.data.bones['foot_'+side].head_local.copy();center.y-=.065;center.z=.052
        box('Armored boot',center,(.145,.29,.11),'foot_'+side,iron,.018)
        box('Boot sole',center+Vector((0,0,-.04)),(.15,.30,.025),'foot_'+side,leather,.007)
        # Rivets brighten the silhouette without a plastic finish.
        sphere('Shoulder rivet',shoulder+Vector((sign*.03,-.12,.025)),(.013,.009,.013),'upperarm_'+side,brass)
    box('Gorget',(0,-.01,h*.839),(.19,.18,.07),'neck_01',iron,.014)
    # Segmented breastplate rather than anatomy-shaped clothing.
    for i in range(1 if light_armor else 3):
        box('Chest lamella',(0,-h*(.082-i*.003),h*(.745-i*.047)),(.34-i*.035,.045,.10),'spine_03' if i==0 else 'spine_02',iron,.012)
    box('Chest insignia',(0,-h*.097,h*.742),(.06,.008,.10),'spine_03',brass,.01)
    if not light_armor:
        rings('Closed helm',(0,-h*.022),[(h*.858,h*.049,h*.045),(h*.89,h*.057,h*.05),(h*.95,h*.061,h*.055),(h*.987,h*.045,h*.044),(h*1.014,h*.008,h*.008)],'head',iron)
        box('Visor recess',(0,-h*.079,h*.945),(h*.094,.018,h*.022),'head',black,.003)
        for side in [-1,1]:box('Ember slit',(side*h*.024,-h*.09,h*.945),(h*.029,.006,h*.004),'head',glow,.001)
        box('Helm ridge',(0,-h*.08,h*.977),(.016,.016,h*.064),'head',brass,.003)
    if warden:
        for side in [-1,1]:
            box('Crown fin',(side*h*.045,0,h*1.02),(.027,.06,.16),'head',iron,.004)
    if light_armor:
        rings('Deep cowl',(0,-h*.018),[(h*.85,h*.074,h*.065),(h*.94,h*.075,h*.067),(h*1.02,h*.038,h*.037)],'head',cloth,8)
        box('Shadowed mask',(0,-h*.084,h*.929),(h*.09,.016,h*.077),'head',black,.006)
        for side in [-1,1]:box('Masked eye',(side*h*.021,-h*.094,h*.945),(h*.023,.005,h*.004),'head',glow,.001)
        if kind=='Sorceress':
            for side in [-1,1]:rod('Cowl crown',(side*h*.05,0,h*.97),(side*h*.09,0,h*1.075),.027,'head',brass)
        if kind=='Cleric':
            box('Mitre peak',(0,.025,h*1.02),(.09,.10,.14),'head',cloth,.006)
            box('Mitre seal',(0,-.031,h*1.03),(.027,.012,.09),'head',brass,.003)
    if roster and kind=='Paladin':
        for side in [-1,1]:rod('Helm wing',(side*.075,0,h*.98),(side*.18,.015,h*1.04),.027,'head',brass)
        box('Sacred chest bar',(0,-h*.1,h*.765),(.17,.013,.025),'spine_03',brass)
    if roster and kind in ['Sorceress','Cleric']:
        # Split robe panels bind to each thigh, rather than moving rigidly with the chest.
        for side in ['l','r']:
            sign=1 if side=='l' else -1
            panel=mesh('Split vestment',[(sign*.02,-.115,h*.55),(sign*.17,-.095,h*.55),(sign*.22,-.075,h*.12),(sign*.025,-.095,h*.16)],[(0,1,2,3)],'thigh_'+side,cloth)
            mod=panel.modifiers.new('Woven thickness','SOLIDIFY');mod.thickness=.008;bpy.context.view_layer.objects.active=panel;bpy.ops.object.modifier_apply(modifier=mod.name)
    if roster and kind=='Rogue':
        for i in range(3):box('Poison vial',(.13+i*.033,-.12,h*.56),(.019,.023,.075),'pelvis',glow,.003)
    # Tattered cloak: weighted to the spine, with no mobile cloth simulation.
    verts=[];cols=9
    for row,(z,width,y) in enumerate([(.80,.15,.055),(.68,.18,.07),(.48,.22,.105),(.31,.23,.13)]):
        for i in range(cols):
            x=(i/(cols-1)*2-1)*h*width
            zz=h*z-(h*.035*((i*7)%3) if row==3 else 0)
            verts.append((x,h*y+math.cos(i*1.3)*.012,zz))
    if roster and kind in ['Ranger','Rogue']:
        verts=[(x,y,z+h*.16*(1 if i>=cols*3 else 0)) for i,(x,y,z) in enumerate(verts)]
    cloak=mesh('Tattered mantle',verts,[(r*cols+i,r*cols+i+1,(r+1)*cols+i+1,(r+1)*cols+i) for r in range(3) for i in range(cols-1)],'spine_03',cloth)
    solid=cloak.modifiers.new('Cloth thickness','SOLIDIFY');solid.thickness=.008;bpy.context.view_layer.objects.active=cloak;bpy.ops.object.modifier_apply(modifier=solid.name)
    if roster and kind in ['Sorceress','Cleric']:staff(kind)
    elif roster and kind=='Ranger':bow()
    elif roster and kind=='Rogue':daggers()
    else:weapon('hand_r',warden)
    p=rig.data.bones['hand_l'].head_local.copy()
    if warden:
        center=p+Vector((0,-.04,-.22));sphere('Lantern core',center,(.058,.058,.10),'hand_l',glow)
        for side in [-1,1]:box('Lantern cage',center+Vector((side*.06,0,0)),(.014,.08,.23),'hand_l',iron,.002)
        box('Lantern base',center+Vector((0,0,-.12)),(.15,.14,.025),'hand_l',brass,.004)
        box('Lantern lid',center+Vector((0,0,.12)),(.15,.14,.025),'hand_l',brass,.004)
    elif kind in ['Knight','Paladin']:
        p.y-=.05
        verts=[(p.x-.18,p.y,p.z+.27),(p.x+.18,p.y,p.z+.27),(p.x+.16,p.y,p.z-.07),(p.x,p.y,p.z-.34),(p.x-.16,p.y,p.z-.07)]
        shield=mesh('Kite shield',verts,[(0,1,2,3,4)],'hand_l',iron)
        solid=shield.modifiers.new('Shield thickness','SOLIDIFY');solid.thickness=.035;bpy.context.view_layer.objects.active=shield;bpy.ops.object.modifier_apply(modifier=solid.name)
        box('Shield device',p+Vector((0,-.025,-.01)),(.045,.012,.34),'hand_l',brass,.005)
        box('Shield crossbar',p+Vector((0,-.028,.04)),(.20,.012,.035),'hand_l',brass,.004)
    # Consolidate a small number of palette materials and generate usable UVs.
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH':obj.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name=model_name
    if roster:
        # Joining the source body retains duplicate material slots. Collapse used
        # slots before FBX export to avoid redundant Unity submesh draws.
        old=list(body.data.materials);palette=[];lookup={};indices=[]
        for face in body.data.polygons:
            material=old[face.material_index];key=material.as_pointer()
            if key not in lookup:lookup[key]=len(palette);palette.append(material)
            indices.append(lookup[key])
        body.data.materials.clear()
        for material in palette:body.data.materials.append(material)
        for face,index in zip(body.data.polygons,indices):face.material_index=index
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
    baseline=animations();bpy.context.scene.render.fps=30
    bpy.ops.object.select_all(action='SELECT')
    name=model_name
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False)
    # Save the preview in its combat idle pose, while FBX retains the rest bind pose.
    rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'.blend')))
    triangles=sum(len(f.vertices)-2 for f in body.data.polygons)
    report.append({'model':name+'.fbx','triangles':triangles,'bones':len(rig.data.bones),'materials':sorted({m.name.split('.')[0] for m in body.data.materials if m}),'states':['Idle','Attack','Cast','Dodge','Parry','Hit','Death'],'class':kind,'gender':gender,'material_slots':len(body.data.materials)})
(out/'model-report.json').write_text(json.dumps(report,indent=2)+'\n');print('DARK_FANTASY_MODELS',json.dumps(report))

if roster:sys.exit(0)

# Original tiled surface maps, baked from Blender shader nodes, not downloaded art.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.mesh.primitive_plane_add(size=2);plane=bpy.context.object
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
for name,scale in [('ForgedMetal',12),('CourtyardStone',22)]:
    material=mat(name,(.8,.8,.8));plane.data.materials.clear();plane.data.materials.append(material)
    nodes=material.node_tree.nodes;links=material.node_tree.links;shader=nodes.get('Principled BSDF')
    coordinates=nodes.new('ShaderNodeTexCoord');noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=scale;noise.inputs['Detail'].default_value=3
    links.new(coordinates.outputs['UV'],noise.inputs['Vector'])
    ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.64,.64,.64,1);ramp.color_ramp.elements[1].color=(.94,.94,.94,1)
    links.new(noise.outputs['Fac'],ramp.inputs[0]);links.new(ramp.outputs['Color'],shader.inputs['Base Color'])
    image=bpy.data.images.new(name,512,512);target=nodes.new('ShaderNodeTexImage');target.image=image;nodes.active=target
    scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
    bpy.ops.object.bake(type='DIFFUSE');image.filepath_raw=str(out/(name+'.png'));image.file_format='PNG';image.save()
    if name=='ForgedMetal':
        detail=nodes.new('ShaderNodeTexNoise');detail.inputs['Scale'].default_value=150;detail.inputs['Detail'].default_value=2;links.new(coordinates.outputs['UV'],detail.inputs['Vector'])
        bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.3;bump.inputs['Distance'].default_value=.004
        links.new(detail.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],shader.inputs['Normal'])
        image=bpy.data.images.new('ForgedMetalNormal',512,512);image.colorspace_settings.name='Non-Color';target.image=image;nodes.active=target
        bpy.ops.object.bake(type='NORMAL');image.filepath_raw=str(out/'ForgedMetalNormal.png');image.file_format='PNG';image.save()
