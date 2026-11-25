# Codex Rings v1.0.1

Preparar la app ahora es mucho más simple: descarga el `.exe`, abre el panel y pulsa **Preparar y conectar**. Rings detecta Codex o descarga automáticamente la versión oficial, verifica el archivo y abre el inicio de sesión. No necesitas buscar un ejecutable, instalar Node.js ni usar una terminal.

## Cambios

- Preparación y reparación automáticas de la conexión.
- Descarga oficial de OpenAI con SHA-256 verificado para el ZIP y el ejecutable.
- Progreso visible, cancelación y reintento.
- Archivos guardados únicamente en tu perfil; no requiere administrador ni modifica el PATH.
- Corregidos los bordes cuadrados y azules alrededor de los botones redondeados.
- Opciones técnicas trasladadas al menú Avanzado.

La descarga de Codex solo ocurre si no se detecta una instalación, o al solicitar una reparación. En la versión probada ocupa aproximadamente 223 MB de descarga y 417 MB una vez instalada; el tamaño puede cambiar con las publicaciones de OpenAI.

## Descargar

- **CodexRings.exe**: versión portable.
- **CodexRings-v1.0.1-win-x64.zip**: ejecutable, instalador local opcional, documentación y licencia.
- **SHA256SUMS.txt**: sumas de los archivos de esta release.

Requiere Windows 11 x64, .NET Framework 4.8 e Internet. El acceso a los límites requiere una cuenta compatible de ChatGPT. La preparación y la lectura en vivo se comprobaron con Codex CLI oficial `0.172.0`.

Proyecto independiente, sin afiliación con OpenAI. Ejecutable sin firma Authenticode. No incluye tu cuenta ni credenciales. Si actualizas desde la v1.0.0, cierra Rings antes de sustituir el `.exe`.
