"""Render square bust portraits from named Blender models; no Unity required."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];root=Path(args[0]);knights=Path(args[1]);roster=Path(args[2]);out=root/'Portraits';out.mkdir(exist_ok=True)
for group in ['Knights','Paladins','Sorceresses','Rangers','Rogues','Clerics']:
 for look in json.loads((root/'Named'/f'{group}.json').read_text())['looks']:
  bpy.ops.wm.open_mainfile(filepath=str((knights if group=='Knights' else roster)/f"Named_{look['id']}.blend"))
  scene=bpy.context.scene;scene.frame_set(1)
  scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=False
  scene.render.resolution_x=256;scene.render.resolution_y=256;scene.render.resolution_percentage=100
  scene.world=bpy.data.worlds.new('Portrait world');scene.world.color=(.045,.05,.07)
  bpy.ops.object.camera_add(location=(.25,-4,1.85));camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,1.60))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=1.05;scene.camera=camera
  for position,power,color in [((-2,-3,4),450,(.8,.87,1)),((2,-2,3),350,(1,.83,.64)),((0,2,3),550,(.65,.72,1))]:
   bpy.ops.object.light_add(type='AREA',location=position);light=bpy.context.object;light.data.energy=power;light.data.size=3;light.data.color=color;light.rotation_euler=(Vector((0,0,1.6))-light.location).to_track_quat('-Z','Y').to_euler()
  scene.render.filepath=str(out/f"{look['id']}.png");bpy.ops.render.render(write_still=True)
