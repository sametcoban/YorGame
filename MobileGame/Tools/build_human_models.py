"""Generate clothed Ashlight humans from CC0 MPFB assets using Blender 4.3.

Usage: blender -b --factory-startup --python-exit-code 1 --python build_human_models.py -- MPFB_SRC OUTPUT
MPFB_SRC is the src directory of the pinned MPFB checkout listed in CREDITS.md.
No MPFB program code is copied into the game or the exported FBX files.
"""
import sys, os, math, json, tempfile
from pathlib import Path
import bpy, bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree

args = sys.argv[sys.argv.index('--') + 1:]
sys.path.insert(0, str(Path(args[0]).resolve()))
output = Path(args[1]).resolve(); output.mkdir(parents=True, exist_ok=True)
work = Path(tempfile.gettempdir()) / 'ashlight-mpfb-user'
work.mkdir(exist_ok=True)
# MPFB is loaded from its source checkout in this headless tool, rather than
# installed as a Blender extension. Keep its user data in our writable work dir.
original_extension_path = bpy.utils.extension_path_user
def extension_path(package, path='', create=False):
    if package != 'mpfb': return original_extension_path(package, path=path, create=create)
    target = work / path
    if create: target.mkdir(parents=True, exist_ok=True)
    return str(target)
bpy.utils.extension_path_user = extension_path
import mpfb
from mpfb._preferences import MpfbPreferences
bpy.utils.register_class(MpfbPreferences)
entry = bpy.context.preferences.addons.new(); entry.module = 'mpfb'
entry.preferences.mpfb_user_data = str(work)
mpfb.register()
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
from mpfb.services.exportservice import ExportService

def material(name, color, metallic=0, roughness=.6):
    mat = bpy.data.materials.new(name); mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    node = mat.node_tree.nodes.get('Principled BSDF')
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Metallic'].default_value = metallic
    node.inputs['Roughness'].default_value = roughness
    return mat

def weighted_mesh(name, verts, faces, weights, rig, mat):
    mesh = bpy.data.meshes.new(name); mesh.from_pydata(verts, [], faces); mesh.update()
    obj = bpy.data.objects.new(name, mesh); bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat); obj.parent = rig
    for i, vertex_weights in enumerate(weights):
        for bone, weight in vertex_weights.items():
            group = obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone)
            group.add([i], weight, 'REPLACE')
    modifier = obj.modifiers.new('Skeleton', 'ARMATURE'); modifier.object = rig
    for face in mesh.polygons: face.use_smooth = True
    return obj

def surface(body, indices, name, rig, mat, offset):
    faces = [body.data.polygons[i] for i in indices]
    used = sorted({v for face in faces for v in face.vertices}); mapping = {v:i for i,v in enumerate(used)}
    verts = [body.data.vertices[v].co + body.data.vertices[v].normal * offset for v in used]
    weights = [{body.vertex_groups[g.group].name:g.weight for g in body.data.vertices[v].groups
                if body.vertex_groups[g.group].name in rig.data.bones} for v in used]
    obj = weighted_mesh(name, verts, [[mapping[v] for v in face.vertices] for face in faces], weights, rig, mat)
    base_surface = BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(f.vertices) for f in body.data.polygons])
    # Smooth the cut edges along their own boundary loops, rather than leaving
    # staircase outlines where a height selection crosses the base topology.
    bm = bmesh.new(); bm.from_mesh(obj.data)
    boundary = [v for v in bm.verts if any(e.is_boundary for e in v.link_edges)]
    for _ in range(10):
        positions = {}
        for v in boundary:
            adjacent = [e.other_vert(v) for e in v.link_edges if e.is_boundary]
            if len(adjacent) == 2: positions[v] = v.co.lerp((adjacent[0].co+adjacent[1].co)*.5,.6)
        for v,position in positions.items(): v.co = position
    for v in boundary:
        location,normal,_,_ = base_surface.find_nearest(v.co)
        if location is not None:v.co=location+normal*offset
    if name in ['FittedBodice','SleevelessCuirass']:
        for _ in range(5): bmesh.ops.smooth_vert(bm, verts=[v for v in bm.verts if v not in boundary], factor=.55, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    bm.to_mesh(obj.data); bm.free()
    solid = obj.modifiers.new('GarmentThickness','SOLIDIFY');solid.thickness=.003;solid.offset=0
    bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=solid.name)
    return obj

def rigid_sphere(name, center, scale, rig, bone, mat, segments=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=12, location=center)
    obj=bpy.context.object; obj.name=name; obj.scale=scale
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.data.materials.append(mat); obj.parent=rig
    group=obj.vertex_groups.new(name=bone); group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    modifier=obj.modifiers.new('Skeleton','ARMATURE'); modifier.object=rig
    for face in obj.data.polygons: face.use_smooth=True
    return obj

def ribbon(name, points, widths, rig, bone, mat):
    verts=[]
    for p,w in zip(points,widths): verts.extend([(p[0]-w,p[1],p[2]),(p[0]+w,p[1],p[2])])
    faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)]
    obj=weighted_mesh(name,verts,faces,[{bone:1} for _ in verts],rig,mat)
    solid=obj.modifiers.new('Thickness','SOLIDIFY');solid.thickness=.004
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.modifier_apply(modifier=solid.name)
    return obj

def animate(rig):
    states={'Idle':(48,{}), 'Attack':(20,{'upperarm_r':(25,0,-75),'lowerarm_r':(-50,0,0),'spine_03':(0,12,-15)}),
        'Cast':(28,{'upperarm_l':(-55,0,30),'upperarm_r':(-55,0,-30),'lowerarm_l':(-30,0,0),'lowerarm_r':(-30,0,0)}),
        'Dodge':(18,{'spine_01':(0,-18,-15),'spine_03':(0,-12,-10),'thigh_l':(-15,0,0),'calf_l':(25,0,0)}),
        'Parry':(22,{'upperarm_l':(-35,0,45),'lowerarm_l':(-65,0,0),'spine_03':(0,0,10)}),
        'Hit':(16,{'spine_03':(15,0,8),'neck_01':(10,0,0)}),
        'Death':(38,{'pelvis':(-75,0,0),'thigh_l':(25,0,0),'thigh_r':(20,0,0),'calf_l':(40,0,0),'calf_r':(35,0,0)})}
    rig.animation_data_create()
    for name,(length,pose) in states.items():
        action=bpy.data.actions.new(name);rig.animation_data.action=action
        for frame,factor in [(1,0),(length//2,1),(length,1 if name=='Death' else 0)]:
            for bone in rig.pose.bones:
                bone.rotation_mode='XYZ';rotation=pose.get(bone.name,(0,0,0))
                if name=='Idle' and bone.name=='spine_03':rotation=(1.5,0,0)
                bone.rotation_euler=tuple(math.radians(v)*factor for v in rotation)
                bone.keyframe_insert(data_path='rotation_euler',frame=frame)
        action.use_fake_user=True
    rig.animation_data.action=None
    for bone in rig.pose.bones:bone.rotation_euler=(0,0,0)
    bpy.context.scene.frame_set(1)

report=[]
for woman in [False,True]:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    gender='Woman' if woman else 'Man'
    macro=TargetService.get_default_macro_info_dict()
    macro.update(gender=0.0 if woman else 1.0, age=.5, height=.6, muscle=.55 if woman else .65, weight=.5)
    body=HumanService.create_human(macro_detail_dict=macro)
    TargetService.bake_targets(body)
    eye_centers=[]
    for name in ['joint-l-eye','joint-r-eye']:
        group=body.vertex_groups[name]
        points=[v.co.copy() for v in body.data.vertices if any(g.group==group.index for g in v.groups)]
        eye_centers.append(sum(points,Vector())/len(points))
    rig=HumanService.add_builtin_rig(body,'game_engine');rig.name='HumanSkeleton'
    ExportService.bake_modifiers_remove_helpers(body,remove_helpers=True)
    body.name='Skin';height=max(v.co.z for v in body.data.vertices)
    skin=material('Skin',(.57,.34,.23) if not woman else (.66,.43,.32),roughness=.65)
    lips=material('Lips',(.37,.13,.11),roughness=.55)
    cloth=material('Cloth',(.22,.13,.32),roughness=.8)
    leather=material('Leather',(.09,.055,.035),roughness=.8)
    hair=material('Hair',(.055,.025,.014),roughness=.65)
    metal=material('Metal',(.6,.46,.2),metallic=.65,roughness=.32)
    eye_white=material('EyeWhite',(.78,.76,.69),roughness=.25)
    iris=material('Iris',(.06,.16,.13),roughness=.28)
    pupil=material('Pupil',(.008,.009,.008),roughness=.2)
    body.data.materials.clear();body.data.materials.append(skin);body.data.materials.append(lips)
    for face in body.data.polygons:
        face.use_smooth=True
        if all(any(body.vertex_groups[g.group].name=='lips' for g in body.data.vertices[v].groups) for v in face.vertices):face.material_index=1
    covered=set();top=[];shorts=[];boots=[];belt=[];scalp=[]
    for face in body.data.polygons:
        x,y,z=face.center/height
        def has(name):return any(any(body.vertex_groups[g.group].name==name and g.weight>.4 for g in body.data.vertices[v].groups) for v in face.vertices)
        arm_groups=('upperarm_','clavicle_') if woman else ('upperarm_',)
        arm_weight=sum(g.weight for v in face.vertices for g in body.data.vertices[v].groups
            if body.vertex_groups[g.group].name.startswith(arm_groups))/len(face.vertices)
        torso=abs(x)<.125 and .56<z<(.82 if woman else .84) and arm_weight<.3
        neckline=(.76+.3*abs(x) if y<-.015 else .80) if woman else (.81+.15*abs(x) if y<-.015 else .825)
        if torso and (.63 if woman else .57)<z<neckline:top.append(face.index);covered.add(face.index)
        if .435<z<.575 and abs(x)<.17 or has('genitals') or has('nipple') or has('nippleTip'):
            if z<.6:shorts.append(face.index);covered.add(face.index)
            elif face.index not in top:top.append(face.index);covered.add(face.index)
        if z<(.275 if woman else .435):boots.append(face.index);covered.add(face.index)
        if .565<z<.585 and abs(x)<.15:belt.append(face.index)
        if has('scalp'):scalp.append(face.index)
    for faces,name,mat in [(top,'FittedBodice' if woman else 'SleevelessCuirass',cloth),(shorts,'Shorts',leather),(boots,'Boots',leather),(belt,'Belt',metal),(scalp,'HairCap',hair)]:
        surface(body,faces,name,rig,mat,.008 if name in ['FittedBodice','SleevelessCuirass'] else .014 if name=='Belt' else .0045)
    # Keep skin at garment boundaries so smoothing does not introduce gaps.
    # Anatomy beneath opaque garments is removed from the exported body.
    neighbors={v.index:set() for v in body.data.vertices}
    for face in body.data.polygons:
        for v in face.vertices:neighbors[v].add(face.index)
    hidden = []
    for face in body.data.polygons:
        names={body.vertex_groups[g.group].name for v in face.vertices for g in body.data.vertices[v].groups}
        z=face.center.z/height
        interior=face.index in covered and all(neighbors[v].issubset(covered) for v in face.vertices)
        if interior or names.intersection({'genitals','nipple','nippleTip'}) or .46<z<.55 and abs(face.center.x)/height<.17:hidden.append(face.index)
    bm=bmesh.new();bm.from_mesh(body.data);bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i in hidden],context='FACES');bm.to_mesh(body.data);bm.free()
    for i,center in enumerate(eye_centers):
        rigid_sphere('Eye'+str(i),center,(height*.009,height*.009,height*.009),rig,'head',eye_white)
        rigid_sphere('Iris'+str(i),center+Vector((0,-height*.0084,0)),(height*.0036,height*.001,height*.0036),rig,'head',iris)
        rigid_sphere('Pupil'+str(i),center+Vector((0,-height*.0093,0)),(height*.0018,height*.0006,height*.0018),rig,'head',pupil)
    # Original overlapping hair ribbons, attached to the head bone.
    for i in range(19 if woman else 13):
        angle=math.pi*(.05+.9*i/(18 if woman else 12));x=math.cos(angle)*height*.043
        y=math.sin(angle)*height*.036;end=.72 if woman else .88
        ribbon('HairStrand',[(x,y-.005,height*.965),(x*1.07,y+.005,height*.91),(x*1.15,y+.015,height*(end+.05)),(x*.97,y+.016,height*end)],
            [height*.011,height*.012,height*.01,height*.003],rig,'head',hair)
    # Optional split skirt panels, enabled for Sorceress/Cleric prefabs only.
    for back in [False,True]:
        y=height*(.075 if back else -.115)
        obj=ribbon('RobeBack' if back else 'RobeFront',[(0,y,height*.57),(0,y*1.03,height*.46),(0,y*1.12,height*.34)],
            [height*.10,height*.12,height*.145],rig,'pelvis',cloth)
        obj['optional_robe']=True
    # Join material-compatible mesh pieces to keep renderer count small.
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.get('optional_robe')]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name='HumanMesh'
    animate(rig)
    bpy.context.scene.render.fps=30
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:obj.select_set(True)
    path=output/('Human_'+gender+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},
        add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.5,use_mesh_modifiers=True)
    triangles=sum(len(p.vertices)-2 for o in bpy.context.scene.objects if o.type=='MESH' for p in o.data.polygons)
    report.append({'model':path.name,'triangles':triangles,'bones':len(rig.data.bones),'height':height,'states':['Idle','Attack','Cast','Dodge','Parry','Hit','Death']})
    bpy.ops.wm.save_as_mainfile(filepath=str(output/('Human_'+gender+'.blend')))
(output/'model-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('ASHLIGHT_MODELS',json.dumps(report))
