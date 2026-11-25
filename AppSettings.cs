using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class AppSettings
{
    private static string ShortcutPath
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Codex Rings.lnk"); }
    }

    internal static bool StartWithWindows { get { return File.Exists(ShortcutPath); } }

    internal static void SetStartup(bool enabled)
    {
        if (!enabled)
        {
            if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);
            return;
        }
        object shell = null, shortcut = null;
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            shell = Activator.CreateInstance(type);
            shortcut = type.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { ShortcutPath });
            var shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Application.ExecutablePath });
            shortcutType.InvokeMember("Arguments", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "--background" });
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(Application.ExecutablePath) });
            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "Codex Rings - límites de uso" });
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
        }
        finally
        {
            if (shortcut != null) Marshal.ReleaseComObject(shortcut);
            if (shell != null) Marshal.ReleaseComObject(shell);
        }
    }

    internal static string SelectCodex(IWin32Window owner)
    {
        using (var picker = new OpenFileDialog
        {
            Title = "Selecciona el ejecutable oficial de Codex",
            Filter = "Codex (codex.exe)|codex.exe",
            CheckFileExists = true,
            Multiselect = false
        })
        {
            if (picker.ShowDialog(owner) != DialogResult.OK) return null;
            File.WriteAllText(Path.Combine(Data.Dir, "codex-path.txt"), picker.FileName);
            return picker.FileName;
        }
    }
}
