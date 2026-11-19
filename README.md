# VeterinariaVR

Adaptación web de OseoVr4 para una misma dirección de acceso desde escritorio, celular y navegadores con WebXR. La aplicación se construye a partir de la copia independiente del proyecto Unity 6000.0.20f1. El alcance comprende las 18 escenas habilitadas, sus prácticas y sus recursos audiovisuales.

## Estructura

- `adapter/` contiene el reproductor de escenas, la entrada de punteros y mandos, y la integración con los componentes de la práctica.
- `template/` contiene la página de arranque para GitHub Pages.
- `tools/` contiene los pasos de preparación, empaquetado y comprobación. Los modelos originales y las dependencias de terceros se mantienen en la copia de autoría de Unity.

## Compilación sobre la copia de autoría

En la carpeta de trabajo, `Unity/` contiene Assets, Packages y ProjectSettings de la copia. La copia requiere el módulo WebGL de Unity, WebXR Export y su integración con XR Management. Las escenas generadas se guardan en `Assets/WebPortGenerated/`. Los quince vídeos H.264 se preparan con `tools/prepare-media.ps1 -CopyRoot <carpeta-de-trabajo>` y se sirven desde `Delivery/Media/<guid>.mp4`. Cada GUID corresponde al archivo meta original.

Durante la compilación, el adaptador reduce las mallas estáticas de alta densidad con UnityMeshSimplifier 3.1.1, conserva los submateriales y omite las mallas animadas. Los recursos que usan las mismas actividades se agrupan para permitir descargas parciales al cambiar de escena. [UnityMeshSimplifier](https://github.com/Whinarn/UnityMeshSimplifier/tree/v3.1.1)

Para reconstruir desde las fuentes de OseoVr4, los cinco instrumentos de 3ds Max se convierten con Blender y `tools/convert_max.py`. El script recibe la carpeta de los archivos MAX, el destino `ModelosWeb/Instrumentos` y la carpeta del importador `io_scene_max`. El instalador conserva los GUID y archiva las fuentes en `FuentesMax`. La escena de elementos de protección también utiliza el prefab Teleport de Magic Effects FREE. Si falta, `python tools/restore_teleport.py <Unity/Assets> <Magic-Effects-FREE.unitypackage>` recupera las dependencias desde una copia local del paquete. [Importador Autodesk MAX de Blender](https://extensions.blender.org/add-ons/io-scene-max/)

1. Instalar los adaptadores y la plantilla con `tools/install_adapter.ps1 -CopyRoot <carpeta-de-trabajo> -SourceRoot <este-repositorio>`.
2. En Unity, seleccionar **Veterinaria Web → Compilar las escenas habilitadas** con WebGL activo. El resultado queda en `Delivery/`, junto a `Unity/`.
3. Empaquetar con `tools/package-pages.ps1 -CopyRoot <carpeta-de-trabajo> -Destination <carpeta-nueva>`. Se comprueban los 18 paquetes de escenas, el cargador y los límites de distribución.
4. Servir esa carpeta en la ruta `/VeterinariaVR/` y ejecutar `node tools/verify-pages.cjs <URL> <informe.json>` con Playwright y Chromium disponibles. La comprobación recorre las 18 escenas en escritorio y con pantalla táctil emulada. Verificar también las prácticas y los mandos en un visor físico antes de dar por validada la experiencia inmersiva.

## Nexus

La ruta de publicación prevista es `https://odsanchezuniversitaria-web.github.io/VeterinariaVR/`. Un enlace de Nexus puede abrirla como página de nivel superior. Para integrarla en un iframe, el contenedor debe permitir `xr-spatial-tracking` y `fullscreen`. El acceso inmersivo empieza con un gesto del usuario sobre **Entrar en VR**.

La rama de desarrollo contiene la integración. La distribución web se publica por separado al completar la validación.

