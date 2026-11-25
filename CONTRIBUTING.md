# Contribuir

Gracias por ayudar a mejorar Codex Rings.

1. Abre un issue para describir un error o una propuesta. Incluye versión de Windows, escala de pantalla y versión de Codex; evita correos, tokens y archivos de autenticación.
2. Crea una rama para el cambio.
3. Compila con `./build.ps1` y ejecuta `./test.ps1` en Windows.
4. Si cambias la interfaz, genera las capturas con `./bin/CodexRings.exe --render-docs` y revísalas.
5. Abre un pull request indicando problema, solución y validación.

No añadas dependencias para funciones que ya ofrecen WinForms o las API de Windows. Mantén separadas la conexión de Codex, la integración de la barra y la interfaz del panel.

El código se distribuye bajo la licencia MIT del repositorio.
