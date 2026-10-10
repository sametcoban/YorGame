"""Blender art preview; not a Unity render. Pass generated asset folder and PNG path."""
import bpy,sys
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];folder=Path(args[0]);target=args[1]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for name,x in [('Rowan',-1.2),('LanternWarden',1.2)]:
    with bpy.data.libraries.load(str(folder/(name+'.blend')),link=False) as (src,dst):dst.objects=src.objects
    for obj in dst.objects:
        if obj is not None:bpy.context.collection.objects.link(obj)
    rig=next(o for o in dst.objects if o.type=='ARMATURE');rig.location.x=x
for material in bpy.data.materials:
    if material.name.split('.')[0] not in ['Iron','Brass','Blade'] or not material.use_nodes:continue
    nodes=material.node_tree.nodes;links=material.node_tree.links;shader=nodes.get('Principled BSDF')
    texture=nodes.new('ShaderNodeTexImage');texture.image=bpy.data.images.load(str(folder/'ForgedMetal.png'),check_existing=True)
    multiply=nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1
    multiply.inputs[1].default_value=shader.inputs['Base Color'].default_value
    links.new(texture.outputs['Color'],multiply.inputs[2]);links.new(multiply.outputs[0],shader.inputs['Base Color'])
    normal=nodes.new('ShaderNodeTexImage');normal.image=bpy.data.images.load(str(folder/'ForgedMetalNormal.png'),check_existing=True);normal.image.colorspace_settings.name='Non-Color'
    mapper=nodes.new('ShaderNodeNormalMap');mapper.inputs['Strength'].default_value=.45
    links.new(normal.outputs['Color'],mapper.inputs['Color']);links.new(mapper.outputs['Normal'],shader.inputs['Normal'])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=False
scene.render.resolution_x=1000;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.world.color=(.035,.04,.055)
bpy.ops.mesh.primitive_plane_add(size=200)
material=bpy.data.materials.new('Ash floor');material.diffuse_color=(.06,.065,.075,1);bpy.context.object.data.materials.append(material)
bpy.ops.object.camera_add(location=(3,-7,3));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,1.0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=4.9;scene.camera=camera
for pos,power,size,color in [((-3,-3,5),420,4,(.66,.76,1)),((3,-2,3),360,3,(1,.64,.3)),((1,3,4),700,3,(.55,.65,1))]:
    bpy.ops.object.light_add(type='AREA',location=pos);light=bpy.context.object;light.data.energy=power;light.data.size=size;light.data.color=color
    light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=target;bpy.ops.render.render(write_still=True)
