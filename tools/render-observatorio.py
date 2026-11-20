"""Bake the screen-only environment. Blender 4.3: blender -b -P this_file -- output.png [width]."""
import bpy
import math
import random
import sys
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:]
out, width = args[0], int(args[1]) if len(args) > 1 else 4096
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24 if width >= 4096 else 8
scene.cycles.use_denoising = True
scene.cycles.max_bounces = 4
prefs = bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type = 'OPTIX'
    prefs.refresh_devices()
    for device in prefs.devices:
        device.use = device.type == 'OPTIX'
    if any(d.use for d in prefs.devices):
        scene.cycles.device = 'GPU'
except (RuntimeError, TypeError):
    pass
scene.world = bpy.data.worlds.new('Ambiente')
scene.world.color = (.13,.16,.20)

def surface(name, color, metal=0.0, rough=.63, glow=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color,1)
    bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metal
    if glow:
        bsdf.inputs['Emission Color'].default_value = (*color,1)
        bsdf.inputs['Emission Strength'].default_value = glow
    return mat

wall = surface('Revestimiento mineral',(.116,.179,.21),.07,.79)
floor = surface('Suelo satinado',(.112,.147,.164),.08,.72)
ceiling = surface('Techo mate',(.085,.114,.134),.10)
trim = surface('Aluminio cepillado',(.13,.176,.194),.64,.38)
shadow = surface('Junta',(.024,.045,.055))
cove = surface('Luz indirecta',(.97,.85,.70),glow=4.0)
cyan = surface('Luz de zócalo',(.32,.72,.79),glow=1.55)
glass = surface('Cristal de observación',(.004,.011,.025),.10,.47)
glass.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.09
stars = [surface('Estrella '+str(i),col,glow=power)
         for i,(col,power) in enumerate([((.61,.76,1),2),((1,.90,.72),2.8),((.83,.90,1),4.0)])]

def mineral_finish(mat, scale):
    tree=mat.node_tree
    base=tree.nodes.get('Principled BSDF').inputs['Base Color']
    color=tuple(base.default_value)[:3]
    coords=tree.nodes.new('ShaderNodeTexCoord')
    noise=tree.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value=scale
    noise.inputs['Detail'].default_value=3.0
    tree.links.new(coords.outputs['Object'],noise.inputs['Vector'])
    ramp=tree.nodes.new('ShaderNodeValToRGB')
    for stop,factor in zip(ramp.color_ramp.elements,(.84,1.12)):
        stop.color=(*(v*factor for v in color),1)
    tree.links.new(noise.outputs['Fac'],ramp.inputs['Fac'])
    tree.links.new(ramp.outputs['Color'],base)

mineral_finish(wall,3.0)
mineral_finish(floor,2.2)

def mesh(name,vertices,faces,mat,smooth=False):
    data=bpy.data.meshes.new(name)
    data.from_pydata(vertices,[],faces)
    data.materials.append(mat)
    for poly in data.polygons:
        poly.use_smooth=smooth
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    return obj

def polar(a,r,z):
    return (r*math.sin(a), r*math.cos(a), z)

def ring(name,profile,mat):
    steps=384
    verts=[polar(i*math.tau/steps,r,z) for r,z in profile for i in range(steps)]
    faces=[]
    for j in range(len(profile)-1):
        for i in range(steps):
            k=(i+1)%steps
            faces.append((j*steps+i,j*steps+k,(j+1)*steps+k,(j+1)*steps+i))
    mesh(name,verts,faces,mat,True)

def disk(name,r,z,mat):
    verts=[polar(i*math.tau/384,r,z) for i in range(384)]
    mesh(name,verts,[tuple(range(384))],mat)

disk('Piso',6.2,0,floor)
ring('Encuentro del piso',[(5.80,0),(5.91,.025),(5.97,.085),(6,.15)],floor)
ring('Zócalo',[(6,.15),(6,.29)],trim)
ring('Ranura inferior',[(6,.29),(6,.325)],shadow)
ring('Guía luminosa',[(5.997,.299),(5.997,.311)],cyan)
ring('Pared curva',[(6,.325),(6,3.80)],wall)
ring('Cornisa',[(6,3.8),(6.03,3.825),(6.04,3.93)],trim)
ring('Foseado cálido',[(6.025,3.829),(6.025,3.886),(5.89,3.90)],cove)
ring('Ala de techo',[(5.89,3.88),(5.83,3.85),(5.76,3.847),(5.66,3.86)],ceiling)
disk('Techo',5.76,3.86,ceiling)

def window(heading,span,center_z,seed):
    a=math.radians(heading)
    half=math.radians(span)*3
    def point(x,y,rad):
        return polar(a+x/6,rad,center_z+y)
    def contour(w,h):
        corners=[(w-h,0,0,90),(-w+h,0,90,180),(-w+h,0,180,270),(w-h,0,270,360)]
        arc=[]
        for j,(cx,cy,lo,hi) in enumerate(corners):
            for n in range(19):
                t=math.radians(lo+(hi-lo)*n/18)
                arc.append((cx+h*math.cos(t),cy+h*math.sin(t)))
            if j in (0,2):
                x1,y1=arc[-1]
                for n in range(1,97):
                    arc.append((x1*(1-2*n/97),y1))
        return arc
    h=.325
    def rim(name,outer,inner,mat):
        contours=[contour(half-inset,h-inset) for inset,radius in (outer,inner)]
        count=len(contours[0])
        verts=[point(x,y,radius) for shape,(_,radius) in zip(contours,(outer,inner)) for x,y in shape]
        faces=[(i,(i+1)%count,(i+1)%count+count,i+count) for i in range(count)]
        mesh(name,verts,faces,mat,True)
    rim('Perímetro del ventanal',(0,5.984),(.026,5.928),shadow)
    rim('Bisel de aluminio',(.026,5.928),(.061,5.919),trim)
    rim('Junta del vidrio',(.061,5.919),(.093,5.956),shadow)
    w,hg=half-.093,h-.093
    def height(x):
        return math.sqrt(max(0,hg*hg-max(0,abs(x)-(w-hg))**2))
    verts=[]
    for i in range(241):
        x=w*(2*i/240-1)
        y=height(x)
        verts.extend([point(x,-y,5.955),point(x,y,5.955)])
    mesh('Cielo tras el cristal',verts,[(i,i+2,i+3,i+1) for i in range(0,480,2)],glass)
    rng=random.Random(seed)
    batches=[([],[]) for _ in stars]
    for i in range(int(span*.93)):
        x=rng.uniform(-w+.03,w-.03)
        y=rng.uniform(-height(x)+.012,height(x)-.012)
        size=rng.uniform(.0028,.007)
        verts,faces=rng.choices(batches,weights=(.65,.23,.12))[0]
        idx=len(verts)
        for k in range(8):
            t=k*math.tau/8
            verts.append(point(x+size*math.cos(t),y+size*math.sin(t),5.950))
        faces.append(tuple(range(idx,idx+8)))
    for i,(verts,faces) in enumerate(batches):
        if faces: mesh('Estrellas',verts,faces,stars[i])

window(42,58,2.90,2031)
window(-135,54,2.78,2058)

def area(name,pos,target,power,size,color):
    data=bpy.data.lights.new(name,'AREA')
    data.energy=power
    data.shape='DISK'
    data.size=size
    data.color=color
    data.specular_factor=.05
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    obj.location=pos
    obj.rotation_euler=(Vector(target)-Vector(pos)).to_track_quat('-Z','Y').to_euler()

area('Relleno cenital',(0,0,3.70),(0,0,0),590,6,(.87,.93,1))
for angle,energy in [(0,120),(65,85),(-53,150),(138,110),(-128,100),(180,90)]:
    a=math.radians(angle)
    area('Rebote sobre pared',polar(a,3.7,3.35),polar(a,6,1.80),energy,3.9,(.90,.96,1))
area('Reflejo en techo',(0,-1.5,1.65),(0,2,3.85),170,4,(.90,.94,1))
camera=bpy.data.cameras.new('Panorama 360')
camera.type='PANO'
camera.panorama_type='EQUIRECTANGULAR'
obj=bpy.data.objects.new('Cámara de horneado',camera)
scene.collection.objects.link(obj)
scene.camera=obj
obj.location=(0,-.35,1.60)
obj.rotation_euler=(math.pi/2,0,-math.pi/2)
scene.render.resolution_x=width
scene.render.resolution_y=width//2
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.image_settings.color_mode='RGB'
scene.view_settings.view_transform='AgX'
scene.view_settings.exposure=-.65
scene.render.filepath=out
bpy.ops.render.render(write_still=True)
