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
using System.Windows.Forms;

// This form only owns a message handle; it is never displayed.
sealed class TrayMessages : Form {
    public Action<Message> Received;
    protected override void WndProc(ref Message m){if(Received!=null)Received(m);base.WndProc(ref m);}
}

sealed class NativeRing : IDisposable {
    public const int Callback=0x8001;
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct NOTIFYICONDATA {
        public uint cbSize; public IntPtr hWnd; public uint uID,uFlags,uCallbackMessage; public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string szTip;
        public uint dwState,dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)] public string szInfoTitle;
        public uint dwInfoFlags;public Guid guidItem;public IntPtr hBalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)] struct IDENTIFIER {public uint cbSize; public IntPtr hWnd; public uint uID; public Guid guidItem;}
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left,Top,Right,Bottom; }
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern bool Shell_NotifyIcon(uint action,ref NOTIFYICONDATA data);
    [DllImport("shell32.dll")] static extern int Shell_NotifyIconGetRect(ref IDENTIFIER id,out RECT rect);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    readonly IntPtr window;readonly uint id;readonly Guid guid; Icon current; bool added;
    public bool Registered {get{return added;}}
    public bool ArtworkHidden;
    DateTime visibilityChecked=DateTime.MinValue;bool userVisible;
    public bool UserVisible
    {
        get
        {
            if(DateTime.UtcNow-visibilityChecked<TimeSpan.FromSeconds(3))return userVisible;
            visibilityChecked=DateTime.UtcNow;userVisible=false;
            try
            {
                using(var settings=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings"))
                {
                    if(settings==null)return false;
                    foreach(var name in settings.GetSubKeyNames())using(var item=settings.OpenSubKey(name))
                    {
                        if(item==null)continue;
                        Guid value;
                        if(Guid.TryParse(Convert.ToString(item.GetValue("IconGuid")),out value)&&value==guid)
                        {
                            userVisible=Convert.ToInt32(item.GetValue("IsPromoted",0))==1;
                            break;
                        }
                    }
                }
            }
            catch(System.Security.SecurityException){}
            catch(UnauthorizedAccessException){}
            catch(IOException){}
            return userVisible;
        }
    }
    public Rectangle? SlotBounds(){var identifier=new IDENTIFIER {cbSize=(uint)Marshal.SizeOf(typeof(IDENTIFIER)),hWnd=window,uID=id,guidItem=guid};RECT r;if(Shell_NotifyIconGetRect(ref identifier,out r)!=0)return null;return Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom); }
    public NativeRing(IntPtr hwnd,uint number,string identity){window=hwnd;id=number;guid=new Guid(identity);}
    NOTIFYICONDATA Payload(string tip){return new NOTIFYICONDATA {cbSize=(uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),hWnd=window,uID=id,uFlags=1|2|4|32,uCallbackMessage=Callback,hIcon=current.Handle,szTip=tip,szInfo="",szInfoTitle="",guidItem=guid};}
    public static Bitmap Draw(WindowLimit limit){var b=new Bitmap(32,32,System.Drawing.Imaging.PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;var r=new RectangleF(1.5f,1.5f,29,29);using(var p=new Pen(Color.FromArgb(90,255,255,255),3f))g.DrawEllipse(p,r);if(limit!=null&&limit.Used>0)using(var p=new Pen(Color.White,3f)){p.StartCap=LineCap.Round;p.EndCap=LineCap.Round;g.DrawArc(p,r,-90,(float)(limit.Used*3.6));}string value=limit==null?"—":Math.Round(limit.Used,0,MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);using(var font=new Font("Segoe UI",value.Length>2?15:20,FontStyle.Bold,GraphicsUnit.Pixel))using(var format=new StringFormat {Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(value,font,Brushes.White,new RectangleF(0,0,32,32),format);}return b;}
    public void Update(WindowLimit limit,string tip,bool reAdd=false){Icon next;using(var b=ArtworkHidden?new Bitmap(32,32):Draw(limit)){var h=b.GetHicon();try{using(var borrowed=Icon.FromHandle(h))next=(Icon)borrowed.Clone();}finally{DestroyIcon(h);}}var previous=current;current=next;var data=Payload(tip);if(reAdd)added=false;bool ok=Shell_NotifyIcon(added?1u:0u,ref data);if(!ok&&added){added=false;ok=Shell_NotifyIcon(0,ref data);}added=ok;if(previous!=null)previous.Dispose();}
    public object Bounds(){var identifier=new IDENTIFIER {cbSize=(uint)Marshal.SizeOf(typeof(IDENTIFIER)),hWnd=window,uID=id,guidItem=guid};RECT r;int hr=Shell_NotifyIconGetRect(ref identifier,out r);return new {result=hr,left=r.Left,top=r.Top,right=r.Right,bottom=r.Bottom};}
    public void Dispose(){if(current!=null){var d=Payload("");Shell_NotifyIcon(2,ref d);current.Dispose();current=null;}added=false;}
}

sealed class Rings : ApplicationContext {
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern uint RegisterWindowMessage(string value);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    readonly TrayMessages host=new TrayMessages();readonly NativeRing five,week;readonly ContextMenuStrip menu=new ContextMenuStrip();readonly System.Windows.Forms.Timer refresh=new System.Windows.Forms.Timer {Interval=30000};readonly System.Windows.Forms.Timer freshness=new System.Windows.Forms.Timer {Interval=250};
    readonly TaskbarGlyph fiveGlyph=new TaskbarGlyph(),weekGlyph=new TaskbarGlyph();
    readonly uint taskbarCreated;readonly bool diagnostics;CodexClient client;WindowLimit primary,weekly;DateTime fetched=DateTime.MinValue;string error="Conectando con Codex…",plan="";bool busy,closing,lastFresh;UsagePanel details;
    bool Fresh {get{return fetched!=DateTime.MinValue&&DateTime.UtcNow-fetched<TimeSpan.FromMinutes(2)&&error==null;}}
    public Rings(bool diag,bool show){diagnostics=diag;taskbarCreated=RegisterWindowMessage("TaskbarCreated");var handle=host.Handle;five=new NativeRing(handle,1,"632ace1f-ae89-46f0-b2ea-1df3551c9b51");week=new NativeRing(handle,2,"632ace1f-ae89-46f0-b2ea-1df3551c9b52");
        menu.Items.Add("Ver límites de Codex",null,(s,e)=>OpenDetails());menu.Items.Add("Actualizar",null,async(s,e)=>await Fetch());menu.Items.Add("Iniciar sesión / cambiar cuenta",null,async(s,e)=>await Login());menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Configuración de iconos de Windows",null,(s,e)=>Process.Start(new ProcessStartInfo("ms-settings:taskbar"){UseShellExecute=true}));menu.Items.Add("Seleccionar codex.exe",null,(s,e)=>ChooseCodex());menu.Items.Add("Cerrar Codex Rings",null,(s,e)=>ExitThread());
        menu.Renderer=new ToolStripProfessionalRenderer(new DarkMenuColors());menu.ShowImageMargin=false;menu.BackColor=Theme.Surface;menu.ForeColor=Theme.Text;menu.Font=new Font("Segoe UI",9.5f);foreach(ToolStripItem item in menu.Items)item.Padding=new Padding(8,5,8,5);
        host.Received=m=>{if(m.Msg==NativeRing.Callback){int message=m.LParam.ToInt32();if(message==0x202||message==0x203)OpenDetails();else if(message==0x205||message==0x7b){SetForegroundWindow(host.Handle);menu.Show(Cursor.Position);}}else if((uint)m.Msg==taskbarCreated)UpdateIcons(true);};
        refresh.Tick+=async(s,e)=>await Fetch();freshness.Tick+=(s,e)=>{if(OpenSignal!=null&&OpenSignal.WaitOne(0))OpenDetails();SyncGlyphs();if(lastFresh!=Fresh){UpdateIcons();UpdateDetails();WriteDiagnostics();}};UpdateIcons();refresh.Start();freshness.Start();host.BeginInvoke(new Action(async()=>{if(show)OpenDetails();await Fetch();}));}
    static string Value(WindowLimit w){return w==null?"—":Math.Round(w.Used,0,MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);}
    void UpdateIcons(bool reAdd=false){lastFresh=Fresh;five.Update(Fresh?primary:null,"Codex · 5 horas: "+(Fresh?Value(primary)+" % usado":"sin datos actuales"),reAdd);week.Update(Fresh?weekly:null,"Codex · Semanal: "+(Fresh?Value(weekly)+" % usado":"sin datos actuales"),reAdd);SyncGlyphs();}
    void SyncGlyphs(){var a=five.SlotBounds();var b=week.SlotBounds();bool distinct=a.HasValue&&b.HasValue&&a.Value!=b.Value&&five.UserVisible&&week.UserVisible;bool showFive=distinct&&fiveGlyph.Sync(a,Fresh?primary:null);bool showWeek=distinct&&weekGlyph.Sync(b,Fresh?weekly:null);if(!distinct){fiveGlyph.Hide();weekGlyph.Hide();}if(five.ArtworkHidden!=showFive){five.ArtworkHidden=showFive;five.Update(Fresh?primary:null,"Codex | 5 horas: "+(Fresh?Value(primary)+" % usado":"sin datos actuales"));}if(week.ArtworkHidden!=showWeek){week.ArtworkHidden=showWeek;week.Update(Fresh?weekly:null,"Codex | Semanal: "+(Fresh?Value(weekly)+" % usado":"sin datos actuales"));}}
    async Task EnsureClient(){if(client!=null)return;client=new CodexClient();client.Changed+=()=>{if(!closing)try{host.BeginInvoke(new Action(async()=>await Fetch()));}catch(InvalidOperationException){}};await client.Start();}
    async Task Fetch(){if(busy||closing)return;busy=true;UpdateDetails();try{await EnsureClient();var result=await client.Call("account/rateLimits/read",new {});if(closing)return;var limits=Data.Get(Data.Get(result,"rateLimitsByLimitId"),"codex")??Data.Get(result,"rateLimits");primary=null;weekly=null;foreach(var w in new[]{WindowLimit.Parse(Data.Get(limits,"primary")),WindowLimit.Parse(Data.Get(limits,"secondary"))}){if(w==null)continue;if(w.Minutes==300)primary=w;else if(w.Minutes==10080)weekly=w;}if(primary==null&&weekly==null)throw new Exception("Esta cuenta no reporta estos límites. Inicia sesión con tu cuenta de ChatGPT.");plan=Convert.ToString(Data.Get(limits,"planType"));fetched=DateTime.UtcNow;error=null;}catch(Exception ex){error=ex.Message;if(client!=null)client.Dispose();client=null;}finally{busy=false;if(!closing){UpdateIcons();UpdateDetails();WriteDiagnostics();}}}
    void OpenDetails()
    {
        if(closing)return;
        if(details!=null&&!details.IsDisposed){details.Show();details.Activate();return;}
        details=new UsagePanel();
        details.StartWithWindows=AppSettings.StartWithWindows;
        details.RefreshRequested+=async(s,e)=>await Fetch();
        details.LoginRequested+=async(s,e)=>await Login();
        details.CodexRequested+=(s,e)=>ChooseCodex();
        details.StartupChanged+=(s,e)=>{try{AppSettings.SetStartup(details.StartWithWindows);}catch(Exception ex){MessageBox.Show(details,ex.Message,"Codex Rings");details.StartWithWindows=AppSettings.StartWithWindows;}};
        var area=Screen.FromPoint(Cursor.Position).WorkingArea;
        details.Location=new Point(Math.Max(area.Left,area.Right-details.Width-16),Math.Max(area.Top,area.Bottom-details.Height-16));
        UpdateDetails();details.Show();
        if(details.Width>area.Width-32||details.Height>area.Height-32)
        {
            details.AutoScroll=true;details.AutoScrollMinSize=details.ClientSize;
            details.Size=new Size(Math.Min(details.Width,area.Width-32),Math.Min(details.Height,area.Height-32));
        }
        details.Location=new Point(Math.Max(area.Left+8,area.Right-details.Width-16),Math.Max(area.Top+8,area.Bottom-details.Height-16));
        details.Activate();
    }
    void UpdateDetails(){if(details!=null&&!details.IsDisposed)details.UpdateUsage(primary,weekly,Fresh,plan,error,fetched,busy);}
    async void ChooseCodex()
    {
        if(busy)return;
        if(AppSettings.SelectCodex(details)==null)return;
        if(client!=null)client.Dispose();client=null;
        await Fetch();
    }
    async Task Login()
    {
        if(busy||closing)return;
        busy=true;UpdateDetails();
        try
        {
            await EnsureClient();
            var result=await client.Call("account/login/start",new {type="chatgpt"});
            if(closing)return;
            string url=Convert.ToString(Data.Get(result,"authUrl"));
            Uri uri;
            if(!Uri.TryCreate(url,UriKind.Absolute,out uri)||uri.Scheme!="https"||!(uri.Host=="auth.openai.com"||uri.Host.EndsWith(".openai.com")||uri.Host=="chatgpt.com"))
                throw new Exception("Codex no devolvi\u00f3 una direcci\u00f3n oficial de inicio de sesi\u00f3n.");
            Process.Start(new ProcessStartInfo(url){UseShellExecute=true});
            error="Completa el inicio de sesi\u00f3n en tu navegador.";
        }
        catch(Exception ex)
        {
            if(!closing)MessageBox.Show(details,ex.Message,"Codex Rings",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        finally
        {
            busy=false;
            if(!closing){UpdateDetails();UpdateIcons();}
        }
    }
    void WriteDiagnostics(){if(!diagnostics)return;try{File.WriteAllText(Path.Combine(Data.Dir,"status.json"),Data.Json().Serialize(new {connected=Fresh,primary=primary==null?(double?)null:primary.Used,weekly=weekly==null?(double?)null:weekly.Used,updatedUtc=fetched,error=error,nativeTray=true,overlayVisible=host.Visible,fiveRegistered=five.Registered,weekRegistered=week.Registered,fiveBounds=five.Bounds(),weekBounds=week.Bounds(),fiveGlyph=fiveGlyph.Status(),weekGlyph=weekGlyph.Status()}));using(var b=five.ArtworkHidden?TaskbarGlyph.Draw(Fresh?primary:null):NativeRing.Draw(Fresh?primary:null))b.Save(Path.Combine(Data.Dir,"five-hours.png"));using(var b=week.ArtworkHidden?TaskbarGlyph.Draw(Fresh?weekly:null):NativeRing.Draw(Fresh?weekly:null))b.Save(Path.Combine(Data.Dir,"weekly.png"));}catch{}}
    protected override void ExitThreadCore(){closing=true;refresh.Stop();freshness.Stop();if(client!=null)client.Dispose();fiveGlyph.Dispose();weekGlyph.Dispose();five.Dispose();week.Dispose();if(details!=null)details.Dispose();host.Dispose();menu.Dispose();refresh.Dispose();freshness.Dispose();base.ExitThreadCore();}
    internal static EventWaitHandle OpenSignal;
    [STAThread] public static void Main(string[] args)
    {
        SetProcessDPIAware();Directory.CreateDirectory(Data.Dir);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Contains("--self-test")){try{Tests.Run();}catch(Exception ex){File.WriteAllText(Path.Combine(Data.Dir,"test-results.txt"),"FAIL "+ex.Message);Environment.ExitCode=1;}return;}
        if(args.Contains("--render-docs")){RenderDocs();return;}
        bool own;
        using(var mutex=new Mutex(true,"Local\\CodexRings.Singleton",out own))
        using(OpenSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexRings.Open"))
        {
            if(!own){OpenSignal.Set();return;}
            Application.Run(new Rings(args.Contains("--diagnostics"),!args.Contains("--background")));
        }
    }
    static void RenderDocs()
    {
        var target=Path.Combine(Environment.CurrentDirectory,"docs","images");Directory.CreateDirectory(target);
        using(var panel=new UsagePanel())
        {
            panel.Location=new Point(-2000,-2000);panel.Show();Application.DoEvents();
            panel.UpdateUsage(new WindowLimit {Used=38,Minutes=300,Reset=1795604400},new WindowLimit {Used=19,Minutes=10080,Reset=1796083200},true,"plus",null,DateTime.UtcNow,false);
            panel.SaveImage(Path.Combine(target,"usage-panel.png"));
            panel.UpdateUsage(null,null,false,"","Selecciona codex.exe y conecta tu cuenta para comenzar.",DateTime.MinValue,false);
            panel.SaveImage(Path.Combine(target,"setup-panel.png"));
        }
    }
}
