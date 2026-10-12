"""Blender contact sheet for a named hero class; not a Unity/device render. Optional final argument selects a palette group."""
import bpy,sys,json
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];folder=Path(args[0]);root=Path(args[1]);target=Path(args[2])
looks=json.loads((root/('Named/'+(args[3] if len(args)>3 else 'Knights')+'.json')).read_text())['looks']
bpy.ops.wm.read_factory_settings(use_empty=True)
for i,look in enumerate(looks):
    with bpy.data.libraries.load(str(folder/('Named_'+look['id']+'.blend')),link=False) as (source,data):data.objects=source.objects
    for obj in data.objects:
        if obj is not None:bpy.context.collection.objects.link(obj)
    rig=next(o for o in data.objects if o.type=='ARMATURE')
    anchor=bpy.data.objects.new(look['name']+' layout',None);bpy.context.collection.objects.link(anchor);anchor.location.x=(i-2)*2.4;rig.parent=anchor
    bpy.ops.object.text_add(location=((i-2)*2.4,-1.25,.04));text=bpy.context.object;text.data.body=look['name'];text.data.align_x='CENTER';text.data.size=.23
    text.rotation_euler=(1.32,0,0)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=False;scene.render.resolution_x=1600;scene.render.resolution_y=650;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Preview world');scene.world.color=(.04,.045,.06)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;material=bpy.data.materials.new('Preview floor');material.diffuse_color=(.055,.06,.075,1);floor.data.materials.append(material)
bpy.ops.object.camera_add(location=(.2,-13,4));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,1))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=12.5;scene.camera=camera
for position,power,color in [((-4,-5,6),1500,(.8,.87,1)),((5,-3,4),1200,(1,.83,.64)),((0,4,5),1700,(.65,.72,1))]:
    bpy.ops.object.light_add(type='AREA',location=position);light=bpy.context.object;light.data.energy=power;light.data.size=7;light.data.color=color;light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(target);bpy.ops.render.render(write_still=True)
