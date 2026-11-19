"""Run with Blender -b --disable-autoexec --python-exit-code 1 -P this.py -- UNITY PUBLIC."""
import sys
from pathlib import Path

import bpy

source, output = map(Path, sys.argv[sys.argv.index("--") + 1:])
output.mkdir(parents=True, exist_ok=True)
files = {
    "craneo": "CraneoSuperioCloner2.fbx", "mandibula": "Mandibula4.fbx",
    "vertebras": "Vertebra_2_3.blend", "escapula": "Clavicula1.fbx",
    "costillas": "Costillas.blend", "radio": "Radio Cubito.blend",
    "tibia-perone": "Tibia_Perone.blend", "mano": "mano_perro.blend",
    "lumbar": "Lumbar_Cola.blend", "pie": "Pata1.blend", "tibia": "Tibia.blend",
}
for slug, filename in files.items():
    path = source / "Assets" / "Oseo 3D" / filename
    if path.suffix == ".blend":
        bpy.ops.wm.open_mainfile(filepath=str(path), load_ui=False, use_scripts=False)
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(path), use_image_search=False)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH" and len(o.data.polygons) and not o.hide_render]
    if not meshes:
        raise ValueError(f"Sin malla visible en {path}")
    bpy.ops.object.select_all(action="DESELECT")
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    for o in meshes:
        o.hide_set(False)
        o.hide_select = False
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        o.data = o.data.copy()
        if triangles > 14000:
            mod = o.modifiers.new("LOD navegador", "DECIMATE")
            mod.ratio = min(1, 14000 / triangles)
            bpy.ops.object.modifier_apply(modifier=mod.name)
        o.data.materials.clear()
        for p in o.data.polygons:
            p.use_smooth = True
    bpy.ops.export_scene.gltf(filepath=str(output / (slug + ".glb")), export_format="GLB",
        use_selection=True, export_materials="NONE", export_texcoords=False,
        export_animations=False, export_skins=False, export_morph=False, export_extras=False)
    print(f"Exportado {slug}: {triangles} triángulos de origen", flush=True)

