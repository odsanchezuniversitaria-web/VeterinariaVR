"""Bake static high-density bone geometry into copies of the original Blender assets."""
from pathlib import Path
import bpy, sys
root, dest = map(Path, sys.argv[sys.argv.index("--")+1:])
dest.mkdir(exist_ok=True, parents=True)
for name in ("Vertebra_2_3","Radio Cubito","Tibia_Perone","mano_perro","Lumbar_Cola","Pata1","Tibia","Femur","pelvis"):
    src=root/(name+".blend")
    bpy.ops.wm.open_mainfile(filepath=str(src),load_ui=False,use_scripts=False)
    for obj in bpy.context.scene.objects:
        if obj.type!="MESH" or obj.data.shape_keys or len(obj.data.polygons)<50000: continue
        bpy.context.view_layer.objects.active=obj
        # Keep transforms, vertex UVs, material slots and the source object names used by prefabs.
        mod=obj.modifiers.new("Web LOD","DECIMATE")
        mod.ratio=min(1.0,18000/sum(len(p.vertices)-2 for p in obj.data.polygons))
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(dest/src.name),compress=True,relative_remap=False)

