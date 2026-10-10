"""Authored stylized combat keyframes; no motion capture or paid animation data.
blender -b --factory-startup --python-exit-code 1 --python Tools/polish_combat_motion.py -- REFERENCE_DIR ROSTER_DIR ENEMY_DIR OUTPUT
Run the geometry generators first. FBX bind pose stays neutral; root motion is off.
"""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Euler,Quaternion,Vector
reference,roster,enemies,out=map(Path,sys.argv[sys.argv.index('--')+1:][:4]);out.mkdir(parents=True,exist_ok=True)
jobs={'Rowan':reference/'Rowan.blend'}
for folder in [roster,enemies]:
    for path in sorted(folder.glob('*.blend')):jobs[path.stem]=path

def scaled(pose,factor):return {bone:tuple(v*factor for v in angles) for bone,angles in pose.items()}
def mix(a,b):return {**a,**b}
def action(name,keys,baseline,locations=None):
    act=bpy.data.actions.new(name);act.use_fake_user=True;rig.animation_data.action=act
    for frame,pose in keys:
        for bone in rig.pose.bones:
            bone.rotation_mode='QUATERNION'
            # Author in Blender world axes, then convert into each bone's rest axes.
            world=Euler(tuple(math.radians(v) for v in pose.get(bone.name,(0,0,0))),'XYZ').to_quaternion()
            rest=rig.data.bones[bone.name].matrix_local.to_quaternion()
            bone.rotation_quaternion=baseline.get(bone.name,Quaternion())@(rest.inverted()@world@rest)
            bone.location=Vector((0,0,0))
            if locations and bone.name=='pelvis':
                z=locations.get(frame,0);bone.location=rest.inverted()@Vector((0,0,z))
            bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)
            bone.keyframe_insert(data_path='location',frame=frame)
    for curve in act.fcurves:
        for key in curve.keyframe_points:key.interpolation='BEZIER';key.handle_left_type='AUTO_CLAMPED';key.handle_right_type='AUTO_CLAMPED'
    return act

report=[]
for name,path in jobs.items():
    bpy.ops.wm.open_mainfile(filepath=str(path));rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    bpy.context.scene.frame_set(1);baseline={b.name:b.rotation_quaternion.copy() for b in rig.pose.bones}
    for old in list(bpy.data.actions):bpy.data.actions.remove(old)
    rig.animation_data_create();enemy=path.parent==enemies;hound=name=='AshHound';kind=name.split('_')[0] if not enemy else 'Enemy'
    if name=='Rowan':kind='Knight'
    if hound:
        breath={'spine':(1,0,0),'tail':(0,0,5)}
        ready={'head':(-12,0,0),'jaw':(18,0,0),'front_upper_l':(16,0,0),'front_upper_r':(16,0,0),'hind_upper_l':(-12,0,0),'hind_upper_r':(-12,0,0)}
        bite={'head':(16,0,0),'jaw':(-7,0,0),'front_upper_l':(-24,0,0),'front_upper_r':(-24,0,0),'tail':(0,0,-10)}
        action('Idle',[(1,{}),(16,breath),(31,{}),(46,scaled(breath,-.7)),(61,{})],baseline)
        action('Windup',[(1,{}),(8,scaled(ready,.4)),(23,ready),(40,ready)],baseline)
        action('Attack',[(1,ready),(6,mix(ready,{'jaw':(35,0,0),'head':(-18,0,0)})),(14,bite),(17,scaled(bite,.7)),(23,{'head':(-5,0,0),'jaw':(12,0,0)}),(31,{})],baseline)
        action('Cast',[(1,{}),(8,{'head':(-20,0,0),'jaw':(30,0,0)}),(14,{'head':(8,0,0),'jaw':(12,0,0)}),(31,{})],baseline)
        action('Hit',[(1,{}),(3,{'head':(-16,0,8),'spine':(0,0,-6)}),(6,{'head':(-10,0,4)}),(14,{})],baseline)
        action('Dodge',[(1,{}),(4,{'spine':(0,0,15)}),(8,{'spine':(0,0,20),'front_upper_l':(-15,0,0)}),(19,{})],baseline)
        action('Parry',[(1,{}),(4,{'head':(-12,0,0),'jaw':(8,0,0)}),(9,{'head':(4,0,0)}),(20,{})],baseline)
        action('Death',[(1,{}),(8,{'root':(0,25,0),'head':(15,0,0)}),(19,{'root':(0,72,0),'head':(25,0,0),'jaw':(18,0,0)}),(40,{'root':(0,78,0),'head':(25,0,0),'jaw':(18,0,0)})],baseline)
        contact=14;duration=31;contacts=[14]
    else:
        breathe={'spine_03':(1,0,0),'neck_01':(-.5,0,0)}
        action('Idle',[(1,{}),(16,breathe),(31,{}),(46,scaled(breathe,-.6)),(61,{})],baseline)
        recoil={'spine_03':(-10,0,5),'neck_01':(-10,0,0),'upperarm_r':(-8,0,4),'upperarm_l':(-8,0,-4)}
        action('Hit',[(1,{}),(3,recoil),(5,scaled(recoil,.85)),(9,scaled(recoil,-.18)),(15,{})],baseline)
        dodge={'pelvis':(5,0,-12),'spine_01':(8,0,-12),'spine_03':(0,0,-8),'thigh_l':(-12,0,0),'calf_l':(20,0,0),'upperarm_l':(0,0,-10)}
        action('Dodge',[(1,{}),(4,scaled(dodge,.7)),(7,dodge),(11,scaled(dodge,.7)),(19,{})],baseline)
        block={'upperarm_l':(0,-42,-24),'lowerarm_l':(0,-45,-15),'upperarm_r':(0,20,20),'spine_03':(0,0,-8)}
        action('Parry',[(1,{}),(4,block),(7,mix(block,{'spine_03':(-5,0,-12)})),(12,scaled(block,.5)),(20,{})],baseline)
        collapse={'pelvis':(55,0,-12),'spine_01':(15,0,0),'neck_01':(20,0,0),'thigh_l':(-55,0,0),'thigh_r':(-50,0,0),'calf_l':(80,0,0),'calf_r':(75,0,0),'upperarm_l':(0,-20,15),'upperarm_r':(0,20,-15)}
        action('Death',[(1,{}),(7,scaled(collapse,.25)),(17,scaled(collapse,.75)),(29,collapse),(43,collapse)],baseline,locations={1:0,7:-.08,17:-.28,29:-.43,43:-.43})
        charge={'upperarm_r':(-15,25,22),'lowerarm_r':(-25,18,0),'upperarm_l':(-10,-20,-25),'lowerarm_l':(-20,-35,0),'spine_03':(-5,0,-5)}
        release=mix(charge,{'upperarm_r':(-30,0,30),'lowerarm_r':(-10,0,0),'upperarm_l':(-28,0,-30),'spine_03':(6,0,5)})
        cast_keys=[(1,{}),(4,scaled(charge,.4)),(8,charge),(11,release),(14,scaled(release,.75)),(23,scaled(charge,.25)),(31,{})]
        action('Cast',cast_keys,baseline)
        if not enemy and kind in ['Sorceress','Cleric']:
            keys=cast_keys;contact=11;duration=31;contacts=[11]
        elif not enemy and kind=='Ranger':
            aim={'upperarm_l':(0,-15,-26),'lowerarm_l':(0,-10,-10),'upperarm_r':(0,18,35),'lowerarm_r':(0,65,30),'spine_03':(0,0,-12)}
            loose=mix(aim,{'lowerarm_r':(0,18,12),'upperarm_r':(0,0,18),'spine_03':(0,0,-5)})
            keys=[(1,{}),(4,scaled(aim,.4)),(7,aim),(9,loose),(11,scaled(loose,.75)),(19,scaled(aim,.2)),(27,{})];contact=9;duration=27;contacts=[9]
        elif not enemy and kind=='Rogue':
            coil={'upperarm_r':(0,30,-25),'upperarm_l':(0,15,15),'lowerarm_r':(0,32,0),'spine_03':(0,0,-16),'pelvis':(0,0,-6)}
            right={'upperarm_r':(0,-20,52),'lowerarm_r':(0,-15,25),'spine_03':(5,0,18),'upperarm_l':(0,25,5)}
            left={'upperarm_l':(0,20,-52),'lowerarm_l':(0,15,-25),'spine_03':(5,0,-18),'upperarm_r':(0,25,5)}
            keys=[(1,{}),(4,coil),(7,right),(9,mix(coil,{'spine_03':(0,0,8)})),(12,left),(15,scaled(left,.7)),(21,scaled(coil,.15)),(26,{})];contact=7;duration=26;contacts=[7,12]
        else:
            heavy=kind=='Paladin' or name in ['IronWatcher','Roadkeeper']
            wind={'upperarm_r':(0,65,-18),'lowerarm_r':(0,30,-8),'spine_03':(-6,0,-16),'pelvis':(0,0,-6),'upperarm_l':(0,12,-12)}
            strike={'upperarm_r':(0,-32,44),'lowerarm_r':(0,-24,18),'spine_03':(10,0,16),'pelvis':(0,0,6),'thigh_l':(-8,0,0)}
            follow=mix(strike,{'upperarm_r':(0,-45,55),'spine_03':(12,0,20)})
            contact=14 if enemy else 10 if heavy else 8;duration=31 if enemy or heavy else 25;contacts=[contact]
            keys=[(1,wind if enemy else {}),(5,wind),(contact,strike),(contact+3,follow),(duration-7,scaled(wind,.15)),(duration,{})]
            if enemy and name in ['CinderScout','HollowMatron']:
                wind=charge;keys=[(1,charge),(6,mix(charge,{'spine_03':(-8,0,-8)})),(14,release),(17,scaled(release,.8)),(24,scaled(charge,.2)),(31,{})]
            if enemy:action('Windup',[(1,{}),(9,scaled(wind,.4)),(25,wind),(40,wind)],baseline)
        action('Attack',keys,baseline)
    # Export rest bind pose, not the active action's last keyed pose.
    rig.animation_data.action=None
    for bone in rig.pose.bones:bone.rotation_quaternion=Quaternion();bone.location=Vector((0,0,0))
    bpy.context.scene.render.fps=30;bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False)
    rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'.blend')))
    report.append({'model':name,'enemy':enemy,'contact_frames':contacts,'attack_final_frame':duration,'states':sorted(a.name for a in bpy.data.actions),'fps':30})
(out/'motion-report.json').write_text(json.dumps(report,indent=2)+'\n');print('POLISHED_COMBAT_MOTION',len(report),'models')
