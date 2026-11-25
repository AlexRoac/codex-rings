# Codex Rings v1.0.0

Tus límites de Codex, directamente en la barra de tareas de Windows.

Esta primera versión muestra el porcentaje usado de cinco horas y de la semana con dos círculos blancos, y abre un panel oscuro con disponibilidad y reinicios.

## Descargar

- **CodexRings.exe**: portable; guárdalo en una carpeta estable y ejecútalo.
- **CodexRings-v1.0.0-win-x64.zip**: ejecutable, instalador local opcional, README y licencia.
- **SHA256SUMS.txt**: sumas para comprobar las descargas.

## Primer uso

Requiere Windows 11 x64, .NET Framework 4.8 y Codex instalado. Rings no incluye el binario de Codex ni una sesión de cuenta.

1. Abre `CodexRings.exe`.
2. Si no detecta Codex, usa **Configurar Codex** para seleccionar el `codex.exe` oficial.
3. Pulsa **Conectar cuenta** para iniciar sesión en el navegador, o usa tu sesión existente.
4. Si los círculos quedan ocultos, arrástralos desde el menú de la flecha al área visible de la barra.

## Incluye

- Cuotas reales, consultadas cada 30 segundos sin generar conversaciones.
- Círculos de 24 px, fondo transparente y número de 10 px.
- Panel renovado, acceso a cuenta e inicio automático opcional.
- Recuperación de iconos al reiniciar Explorer.
- Una única instancia; abrir otra vez el ejecutable muestra el panel existente.

## Límites de esta versión

La app es independiente y no está afiliada con OpenAI. No incluye firma Authenticode ni actualización automática. El dibujo ampliado depende de la barra de Windows; si no puede adjuntarse, utiliza iconos pequeños normales. Windows controla el orden y la visibilidad de los iconos.

Se comprobó la conexión en vivo con Codex CLI `0.171.0-alpha.13.2`. La interfaz de `app-server` puede variar entre versiones; no se garantiza compatibilidad con todas las versiones de Codex o personalizaciones de Explorer.
