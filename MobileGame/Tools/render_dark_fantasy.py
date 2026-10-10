"""Blender art preview; not a Unity render. Pass generated asset folder and PNG path."""
import bpy,sys
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];folder=Path(args[0]);target=args[1]
roster=len(args)>2
classes=['Knight','Paladin','Sorceress','Ranger','Rogue','Cleric']
enemies=roster and args[2]=='Enemies'
if enemies:classes=['Lantern Warden','Ash Hound','Cinder Scout','Iron Watcher','Roadkeeper']
models=[(name,(i-2)*2.4) for i,name in enumerate(['LanternWarden','AshHound','CinderScout','IronWatcher','Roadkeeper'])] if enemies else [(kind+'_'+args[2],(i-2.5)*2) for i,kind in enumerate(classes)] if roster else [('Rowan',-1.2),('LanternWarden',1.2)]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for name,x in models:
    with bpy.data.libraries.load(str(folder/(name+'.blend')),link=False) as (src,dst):dst.objects=src.objects
    for obj in dst.objects:
        if obj is not None:bpy.context.collection.objects.link(obj)
    rig=next(o for o in dst.objects if o.type=='ARMATURE');rig.location.x=x
for material in bpy.data.materials:
    if material.name.split('.')[0] not in ['Iron','Brass','Blade','RustIron'] or not material.use_nodes:continue
    nodes=material.node_tree.nodes;links=material.node_tree.links;shader=nodes.get('Principled BSDF')
    texture=nodes.new('ShaderNodeTexImage');texture.image=bpy.data.images.load(str(folder/'ForgedMetal.png'),check_existing=True)
    multiply=nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1
    multiply.inputs[1].default_value=shader.inputs['Base Color'].default_value
    links.new(texture.outputs['Color'],multiply.inputs[2]);links.new(multiply.outputs[0],shader.inputs['Base Color'])
    normal=nodes.new('ShaderNodeTexImage');normal.image=bpy.data.images.load(str(folder/'ForgedMetalNormal.png'),check_existing=True);normal.image.colorspace_settings.name='Non-Color'
    mapper=nodes.new('ShaderNodeNormalMap');mapper.inputs['Strength'].default_value=.45
    links.new(normal.outputs['Color'],mapper.inputs['Color']);links.new(mapper.outputs['Normal'],shader.inputs['Normal'])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=False
scene.render.resolution_x=1600 if roster else 1000;scene.render.resolution_y=550 if roster else 640;scene.render.resolution_percentage=100
scene.world.color=(.035,.04,.055)
bpy.ops.mesh.primitive_plane_add(size=200)
material=bpy.data.materials.new('Ash floor');material.diffuse_color=(.06,.065,.075,1);bpy.context.object.data.materials.append(material)
bpy.ops.object.camera_add(location=(1,-10,4) if roster else (3,-7,3));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,1.0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=13.8 if roster else 4.9;scene.camera=camera
if roster:
    for (name,x),kind in zip(models,classes):
        bpy.ops.object.text_add(location=(x,-1.1,.06))
        text=bpy.context.object;text.data.body='Sorcerer' if kind=='Sorceress' and args[2]=='Man' else kind
        text.data.align_x='CENTER';text.data.size=.18;text.rotation_euler=camera.rotation_euler
        label=bpy.data.materials.new('Label '+name);label.diffuse_color=(.8,.74,.6,1);text.data.materials.append(label)
for pos,power,size,color in [((-3,-3,5),1100 if roster else 420,6 if roster else 4,(.66,.76,1)),((3,-2,3),900 if roster else 360,6 if roster else 3,(1,.64,.3)),((1,3,4),1400 if roster else 700,8 if roster else 3,(.55,.65,1))]:
    bpy.ops.object.light_add(type='AREA',location=pos);light=bpy.context.object;light.data.energy=power;light.data.size=size;light.data.color=color
    light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=target;bpy.ops.render.render(write_still=True)
