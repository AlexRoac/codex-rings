# Publicar una versión

## Publicación desde GitHub Actions

La publicación es reproducible desde el código del tag y no utiliza archivos de sesión del mantenedor.

1. Actualiza la versión en `AssemblyInfo.cs` y `package.ps1`.
2. Actualiza `CHANGELOG.md` y crea `docs/RELEASE-vX.Y.Z.md`.
3. Ejecuta `./package.ps1`. Revisa el panel y los iconos con una cuenta real.
4. Confirma los cambios en `main` y comprueba que el workflow **Build and test** pasa.
5. Crea y sube el tag correspondiente:

```powershell
git tag -a v1.0.0 -m "Codex Rings v1.0.0"
git push origin v1.0.0
```

El workflow **Publish release** compila en Windows, ejecuta las pruebas, comprueba que el tag coincide con la versión del ejecutable y publica el `.exe`, el ZIP y `SHA256SUMS.txt`.

## Publicación manual

Si Actions no está disponible, ejecuta `./package.ps1`, crea el tag y usa GitHub > Releases > Draft a new release. Elige el tag y adjunta únicamente estos archivos de `dist`:

- `CodexRings.exe`
- `CodexRings-v1.0.0-win-x64.zip`
- `SHA256SUMS.txt`

Usa `docs/RELEASE-v1.0.0.md` como descripción. Los archivos `.exe` y ZIP se adjuntan a Releases y están ignorados por Git. No subas `%LOCALAPPDATA%\CodexRings`, `.codex`, credenciales o los archivos de diagnósticos de una cuenta real.

La compilación no lleva firma Authenticode. Las sumas publicadas corresponden al binario adjunto a cada release; una recompilación local puede producir otra suma aunque el código no haya cambiado.
