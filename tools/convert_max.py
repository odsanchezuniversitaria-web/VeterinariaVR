"""Export the surgical instruments into interchange files on the separate web copy."""
from pathlib import Path
import sys
import bpy

folder, target, addon = map(Path, sys.argv[sys.argv.index("--")+1:])
sys.path.insert(0, str(addon))
import source as max_addon
max_addon.register()
target.mkdir(parents=True, exist_ok=True)
for item in sorted(folder.glob("*.max")):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.max(filepath=str(item))
    meshes=[m for m in bpy.context.scene.objects if m.type=="MESH" and len(m.data.polygons)>0]
    if not meshes:
        raise ValueError(f"Sin mallas recuperables en {item.name}")
    print("INSTRUMENTO",item.name,[(m.name,len(m.data.polygons)) for m in meshes])
    bpy.ops.export_scene.fbx(filepath=str(target/(item.stem+".fbx")), bake_anim=False,
                             add_leaf_bones=False, path_mode="RELATIVE", apply_unit_scale=True)

