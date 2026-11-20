# VeterinariaVR

Adaptación web de OseoVr4 para una misma dirección de acceso desde escritorio, celular y navegadores con WebXR. La aplicación se construye a partir de la copia independiente del proyecto Unity 6000.0.20f1. El alcance comprende las 18 escenas habilitadas, sus prácticas y sus recursos audiovisuales.

## Controles en pantalla

En PC, un clic selecciona el objeto o botón bajo el cursor. Mantener el clic permite arrastrar piezas y herramientas. Arrastrar el fondo gira la vista. WASD y las flechas desplazan al usuario.

En celular, un toque selecciona y un arrastre sobre el fondo gira la vista. En vertical, la portada muestra el menú 3D completo y una lista de actividades que se abren con un toque. Dentro de la escena, la palanca de la esquina inferior izquierda permite caminar mientras se toca la escena con otro dedo. El menú **Acciones** reúne los controles que ofrece la práctica actual.

En el menú de inicio de PC y celular se puede caminar hasta 2,5 metros desde la entrada. El recorrido comprueba que haya plataforma bajo los pies y se detiene antes de atravesar el panel. El brillo del menú se obtiene del perfil de posprocesado de la escena.

Las prácticas que tienen el passthrough activo muestran la Vía Láctea en PC y celular. El panorama de 2048 × 1024 píxeles procede del mapa galáctico en alto rango dinámico de NASA/Goddard, elaborado con datos de Gaia. Las otras prácticas usan su cielo propio o la panorámica del laboratorio si tienen el fondo genérico. Al entrar en WebXR se recuperan el fondo y los ajustes de la cámara del visor. [Mapa original y créditos](https://svs.gsfc.nasa.gov/4851/)

Cuando una práctica tiene suelo físico, la posición inicial se calcula sobre él y se comprueba que haya espacio para estar de pie.

## Archivos del proyecto

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

[Abrir VeterinariaVR en GitHub Pages](https://odsanchezuniversitaria-web.github.io/VeterinariaVR/)

Nexus puede abrir esa dirección con un enlace directo. Para integrarla dentro de un iframe, el contenedor debe permitir `xr-spatial-tracking` y `fullscreen`. En un visor compatible, **Entrar en VR** inicia la sesión inmersiva.

La rama `main` contiene los adaptadores y las herramientas de compilación. La rama `gh-pages` contiene la aplicación web. El reproductor descarga los recursos a medida que se abren las escenas.

La prueba de distribución recorrió las 18 escenas en Chrome de escritorio y en pantalla táctil emulada. Una Quest 3 emulada abrió una sesión WebXR con vista estereoscópica y dos mandos. Los 15 vídeos se abrieron y empezaron a reproducirse en Chrome. La comprobación en un visor físico sigue pendiente.

