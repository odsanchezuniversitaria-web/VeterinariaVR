# VeterinariaVR

Adaptación web de OseoVr4 para una misma dirección de acceso desde escritorio, celular y navegadores con WebXR. La aplicación se construye a partir de la copia independiente del proyecto Unity 6000.0.20f1. El alcance comprende las 18 escenas habilitadas, sus prácticas y sus recursos audiovisuales.

## Estructura

- `adapter/` contiene el reproductor de escenas, la entrada de punteros y mandos, y la integración con los componentes de la práctica.
- `template/` contiene la página de arranque para GitHub Pages.
- `tools/` contiene los pasos de preparación, empaquetado y comprobación. Los modelos originales y las dependencias de terceros se mantienen en la copia de autoría de Unity.

## Compilación sobre la copia de autoría

En la carpeta de trabajo, `Unity/` contiene Assets, Packages y ProjectSettings del proyecto. La copia requiere el módulo WebGL de Unity, el proveedor WebXR Export y su integración con XR Management. El adaptador busca el proyecto de autoría por sus escenas y mantiene las versiones de destino en `Assets/WebPortGenerated/`. Los quince vídeos H.264 preparados para la web se encuentran en `Delivery/Media/<guid>.mp4`; el GUID proviene del archivo meta original.

1. Instalar los adaptadores y la plantilla con `tools/install_adapter.ps1 -CopyRoot <carpeta-de-trabajo> -SourceRoot <este-repositorio>`.
2. En Unity, seleccionar **Veterinaria Web → Compilar las escenas habilitadas** con WebGL activo. El resultado queda en `Delivery/`, junto a `Unity/`.
3. Empaquetar con `tools/package-pages.ps1 -CopyRoot <carpeta-de-trabajo> -Destination <carpeta-nueva>`. Se comprueban los 18 paquetes de escenas, el cargador y los límites de distribución.
4. Servir esa carpeta en la ruta `/VeterinariaVR/` y ejecutar `node tools/verify-pages.cjs <URL> <informe.json>` con Playwright y Chromium disponibles. La comprobación recorre las 18 escenas en escritorio y con pantalla táctil emulada. Verificar también las prácticas y los mandos en un visor físico antes de dar por validada la experiencia inmersiva.

## Nexus

La ruta de publicación prevista es `https://odsanchezuniversitaria-web.github.io/VeterinariaVR/`. Un enlace de Nexus puede abrirla como página de nivel superior. Para integrarla en un iframe, el contenedor debe permitir `xr-spatial-tracking` y `fullscreen`. El acceso inmersivo empieza con un gesto del usuario sobre **Entrar en VR**.

La rama de desarrollo contiene la integración. La distribución web se publica por separado al completar la validación.

