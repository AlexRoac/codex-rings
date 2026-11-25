using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

static class Data {
    public static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexRings");
    public static Dictionary<string,object> Obj(object o) { return o as Dictionary<string,object> ?? new Dictionary<string,object>(); }
    public static object Get(object o,string k) { object v; return Obj(o).TryGetValue(k,out v)?v:null; }
    public static double Num(object o) { return Convert.ToDouble(o,CultureInfo.InvariantCulture); }
    public static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength=4000000 }; }
}
class WindowLimit {
    public double Used; public long Reset; public int Minutes;
    public static WindowLimit Parse(object o) { if(o==null || Data.Get(o,"usedPercent")==null) return null; return new WindowLimit { Used=Math.Max(0,Math.Min(100,Data.Num(Data.Get(o,"usedPercent")))), Reset=Convert.ToInt64(Data.Get(o,"resetsAt")), Minutes=Convert.ToInt32(Data.Get(o,"windowDurationMins")) }; }
    public string ResetText() { if(Reset<=0)return "Reinicio no disponible"; return "Reinicia: "+DateTimeOffset.FromUnixTimeSeconds(Reset).ToLocalTime().ToString("ddd d MMM, HH:mm",CultureInfo.GetCultureInfo("es-MX")); }
}
sealed class CodexClient : IDisposable {
    Process proc; int next; readonly object gate=new object(); readonly Dictionary<int,TaskCompletionSource<object>> pending=new Dictionary<int,TaskCompletionSource<object>>();
    public event Action Changed;
    public static string FindCodex() {
        var cfg=Path.Combine(Data.Dir,"codex-path.txt"); if(File.Exists(cfg) && File.Exists(File.ReadAllText(cfg).Trim())) return File.ReadAllText(cfg).Trim();
        var portable=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"codex.exe");if(File.Exists(portable))return portable;
        var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".vscode","extensions");
        if(Directory.Exists(root)) foreach(var d in Directory.GetDirectories(root,"openai.chatgpt-*").OrderByDescending(x=>Directory.GetLastWriteTimeUtc(x))) { var p=Path.Combine(d,"bin","windows-x86_64","codex.exe"); if(File.Exists(p))return p; }
        foreach(var d in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')) { try { var p=Path.Combine(d,"codex.exe"); if(File.Exists(p))return p; }catch{} }
        var npm=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"npm","node_modules","@openai");
        foreach(var package in new[]{"codex","codex-win32-x64"}){var rootPackage=Path.Combine(npm,package);if(Directory.Exists(rootPackage)){var candidates=Directory.GetFiles(rootPackage,"codex.exe",SearchOption.AllDirectories);if(candidates.Length>0)return candidates[0];}}
        throw new Exception("No se encontró Codex. Instala Codex o selecciona codex.exe.");
    }
    public async Task Start() {
        proc=new Process { StartInfo=new ProcessStartInfo(FindCodex(),"app-server") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Data.Dir }, EnableRaisingEvents=true };
        proc.OutputDataReceived+=(s,e)=>{ if(e.Data==null){Fail();return;} try {var m=Data.Json().DeserializeObject(e.Data); var id=Data.Get(m,"id"); if(id!=null){TaskCompletionSource<object> t=null;lock(gate){if(pending.TryGetValue(Convert.ToInt32(id),out t))pending.Remove(Convert.ToInt32(id));} if(t!=null){if(Data.Get(m,"error")!=null)t.TrySetException(new Exception("Codex no pudo consultar la cuenta. Reintenta o inicia sesión."));else t.TrySetResult(Data.Get(m,"result"));}} else {var method=Convert.ToString(Data.Get(m,"method")); if(method=="account/login/completed"||method=="account/updated") {var h=Changed;if(h!=null)h();}} }catch{} };
        proc.ErrorDataReceived+=(s,e)=>{}; proc.Exited+=(s,e)=>Fail();proc.Start();proc.BeginOutputReadLine();proc.BeginErrorReadLine();
        await Call("initialize",new {clientInfo=new {name="codex_rings",version="1.0.0"},capabilities=new {experimentalApi=true}}); Send(new {method="initialized"});
    }
    void Fail(){TaskCompletionSource<object>[] waiting;lock(gate){waiting=pending.Values.ToArray();pending.Clear();}foreach(var t in waiting)t.TrySetException(new Exception("Se desconectó el servidor de Codex."));}
    void Send(object o) { lock(gate){proc.StandardInput.WriteLine(Data.Json().Serialize(o));proc.StandardInput.Flush();} }
    public async Task<object> Call(string method,object args) { int id=Interlocked.Increment(ref next);var t=new TaskCompletionSource<object>();lock(gate)pending[id]=t; try {Send(new {id=id,method=method,@params=args}); if(await Task.WhenAny(t.Task,Task.Delay(25000))!=t.Task)throw new TimeoutException("Codex tardó demasiado. Se reintentará automáticamente.");return await t.Task;} finally{lock(gate)pending.Remove(id);} }
    public void Dispose(){try{if(proc!=null && !proc.HasExited)proc.Kill();}catch{} if(proc!=null)proc.Dispose();Fail();}
}
static class Tests
{
    public static void Run()
    {
        var results = new List<string>();
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); results.Add("PASS " + name); };
        check(WindowLimit.Parse(null) == null, "missing window stays unavailable");
        check(WindowLimit.Parse(Data.Json().DeserializeObject("{}")) == null, "missing percentage does not become zero");
        var used = WindowLimit.Parse(Data.Json().DeserializeObject("{\"usedPercent\":70,\"windowDurationMins\":300,\"resetsAt\":1795577513}"));
        check(used.Used == 70 && used.Minutes == 300, "server usage is used directly, not inverted");
        var weekly = WindowLimit.Parse(Data.Json().DeserializeObject("{\"usedPercent\":17,\"windowDurationMins\":10080}"));
        check(weekly.Minutes == 10080 && weekly.Used == 17, "weekly window is identified by duration");
        check(WindowLimit.Parse(Data.Json().DeserializeObject("{\"usedPercent\":150}")).Used == 100, "arc bounded at 100");
        var zero = WindowLimit.Parse(Data.Json().DeserializeObject("{\"usedPercent\":0}"));
        check(zero.Used == 0, "zero usage remains valid");
        using (var bitmap = NativeRing.Draw(zero))
        {
            check(bitmap.GetPixel(0, 0).A == 0 && bitmap.GetPixel(31, 31).A == 0, "native icon has transparent background");
        }
        using (var bitmap = TaskbarGlyph.Draw(used))
        {
            check(bitmap.GetPixel(0, 0).A == 0 && bitmap.GetPixel(31, 31).A == 0, "enlarged taskbar circle has transparent background");
        }
        var oldReset = new WindowLimit { Used = 70, Minutes = 300, Reset = 1 };
        check(oldReset.Used == 70, "past reset time does not invent a new usage value");
        File.WriteAllLines(Path.Combine(Data.Dir, "test-results.txt"), results);
    }
}
