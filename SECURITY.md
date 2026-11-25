# Seguridad y privacidad

Codex Rings no pide contraseñas, no guarda tokens propios ni incluye credenciales en sus binarios. Invoca métodos de cuenta del servidor local de Codex; el inicio de sesión se realiza en una dirección HTTPS oficial que devuelve ese servidor.

La ruta elegida de `codex.exe` es una configuración de confianza: la app ejecuta ese archivo. Selecciona el ejecutable oficial de Codex.

Los archivos de autenticación y el intercambio de credenciales los gestiona Codex. Cambiar cuenta desde Rings modifica esa sesión compartida. La app respeta el directorio y la configuración de Codex existentes, incluyendo `CODEX_HOME` si está definido.

La v1 no tiene telemetría propia ni actualización automática. Los diagnósticos se habilitan únicamente con `--diagnostics` y se guardan localmente; pueden contener porcentajes, fechas, errores y posiciones, pero no se solicitan ni guardan contraseñas o tokens. Revisa cualquier diagnóstico antes de compartirlo.

Para comunicar una vulnerabilidad, usa la pestaña **Security > Report a vulnerability** del repositorio si está habilitada. No publiques credenciales, tokens ni información personal en un issue. Si la vía privada no está habilitada, abre un issue que solicite un canal privado sin describir el fallo sensible.
