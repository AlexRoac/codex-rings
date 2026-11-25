# Diseño de la v1

## Dirección

Una utilidad discreta de escritorio, inspirada en la claridad de Codex y ChatGPT. El producto es independiente: usa su propio nombre e identidad, sin logotipos de OpenAI ni afirmaciones de afiliación.

La barra muestra únicamente lo necesario para una lectura rápida: dos círculos blancos y el uso dentro de cada uno. El panel amplía esa información con disponibilidad, reinicio, conexión y acciones.

## Tokens

| Rol | Valor |
| --- | --- |
| Fondo | `#181818` |
| Superficie de cuota | `#212121` |
| Bordes | `#353535` |
| Texto principal | `#F2F2F2` |
| Texto secundario | `#A6A6A6` |
| Avisos | `#E5BE89` |

Tipografía: Segoe UI, disponible en Windows. Título de 20 pt; cuotas de 23 pt; controles de 9.5 pt. Los botones principales usan fondo claro y texto oscuro. Los secundarios conservan una superficie neutra y muestran foco visible.

Panel base: 448 × 620 píxeles lógicos, con escalado DPI. Márgenes de 24 px, separación de 12 px entre cuotas y radios de 9, 13 y 15 px según la función de cada elemento. Los datos se alinean a la izquierda; la disponibilidad se alinea a la derecha dentro de la misma cuota.

## Comportamiento

- El porcentaje es **usado**, tanto en los círculos como en el panel.
- El estado desconectado muestra rayas, nunca un cero supuesto.
- La acción principal conecta la cuenta o permite cambiarla.
- Si no hay una instalación de Codex, la acción se llama Preparar y conectar: descarga oficial, verificación y acceso en un mismo flujo. El usuario no necesita buscar un ejecutable.
- El progreso permite cancelar. La selección manual y el comando alternativo se reservan para Avanzado.
- Cerrar el panel conserva la app en el área de notificación.
- Escape cierra el panel; Tab recorre los controles nativos.
- No se usa animación continua ni efectos decorativos.
- Los ejemplos de documentación usan datos de muestra, sin datos de cuenta reales.

## Verificación visual

`CodexRings.exe --render-docs` genera los paneles conectado y sin conexión en `docs/images`. Estos ejemplos no consultan una cuenta. Revisa legibilidad, recortes y foco después de modificar la interfaz.
