using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// A transparent child of the taskbar, inside a slot reserved by Shell_NotifyIcon.
// Hit testing passes to the original notification icon, preserving shell gestures.
sealed class TaskbarGlyph : NativeWindow, IDisposable {
    [StructLayout(LayoutKind.Sequential)] struct POINT {public int X,Y;public POINT(int x,int y){X=x;Y=y;}}
    [StructLayout(LayoutKind.Sequential)] struct SIZE {public int W,H;public SIZE(int w,int h){W=w;H=h;}}
    [StructLayout(LayoutKind.Sequential)] struct RECT {public int L,T,R,B;}
    [StructLayout(LayoutKind.Sequential,Pack=1)] struct BLEND {public byte Op,Flags,Alpha,Format;}
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string cls,string name);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd,out RECT rect);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hwnd,ref POINT pt);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dest,IntPtr position,ref SIZE size,IntPtr source,ref POINT origin,uint key,ref BLEND blend,uint flags);
    Rectangle previous;double previousValue=double.NaN;public int Error;public bool Attached {get{return Handle!=IntPtr.Zero&&IsWindow(Handle)&&GetParent(Handle)==FindWindow("Shell_TrayWnd",null)&&IsWindowVisible(Handle);}}
    public static Bitmap Draw(WindowLimit limit){var bitmap=new Bitmap(32,32,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;var ring=new RectangleF(4,4,24,24);using(var pen=new Pen(Color.FromArgb(100,255,255,255),1.5f))g.DrawEllipse(pen,ring);if(limit!=null&&limit.Used>0)using(var pen=new Pen(Color.White,1.5f)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawArc(pen,ring,-90,(float)(limit.Used*3.6));}string number=limit==null?"—":Math.Round(limit.Used,0,MidpointRounding.AwayFromZero).ToString(System.Globalization.CultureInfo.InvariantCulture);using(var font=new Font("Segoe UI",number.Length>2?7.5f:10f,FontStyle.Bold,GraphicsUnit.Pixel))using(var format=new StringFormat {Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(number,font,Brushes.White,new RectangleF(0,0,32,32),format);}return bitmap;}
    public bool Sync(Rectangle? slot,WindowLimit limit){var parent=FindWindow("Shell_TrayWnd",null);RECT bar;if(parent==IntPtr.Zero||!GetWindowRect(parent,out bar)||!slot.HasValue||slot.Value.Width<28||!Rectangle.FromLTRB(bar.L,bar.T,bar.R,bar.B).Contains(slot.Value)){Hide();return false;}var point=new POINT(slot.Value.Left+(slot.Value.Width-32)/2,slot.Value.Top+(slot.Value.Height-32)/2);ScreenToClient(parent,ref point);bool created=false;if(Handle==IntPtr.Zero||!IsWindow(Handle)||GetParent(Handle)!=parent){if(Handle!=IntPtr.Zero){try{DestroyHandle();}catch{}}var old=SetThreadDpiAwarenessContext(GetWindowDpiAwarenessContext(parent));try{CreateHandle(new CreateParams {Caption="Codex Rings · taskbar glyph",Parent=parent,Style=unchecked((int)0x50000000),ExStyle=0x00080000|0x20|0x08000000,X=point.X,Y=point.Y,Width=32,Height=32});created=true;}catch{Error=Marshal.GetLastWin32Error();return false;}finally{SetThreadDpiAwarenessContext(old);}}var rect=new Rectangle(point.X,point.Y,32,32);double value=limit==null?-1:limit.Used;if(created||previous!=rect||previousValue!=value||!IsWindowVisible(Handle)){SetWindowPos(Handle,IntPtr.Zero,point.X,point.Y,32,32,0x10|0x40);using(var bitmap=Draw(limit)){var screen=GetDC(IntPtr.Zero);var memory=CreateCompatibleDC(screen);var hbitmap=bitmap.GetHbitmap(Color.FromArgb(0));var selected=SelectObject(memory,hbitmap);try{var size=new SIZE(32,32);var origin=new POINT(0,0);var blend=new BLEND {Alpha=255,Format=1};bool ok=UpdateLayeredWindow(Handle,screen,IntPtr.Zero,ref size,memory,ref origin,0,ref blend,2);Error=ok?0:Marshal.GetLastWin32Error();if(!ok){Hide();return false;}}finally{SelectObject(memory,selected);DeleteObject(hbitmap);DeleteDC(memory);ReleaseDC(IntPtr.Zero,screen);}}previous=rect;previousValue=value;}return Attached;}
    public void Hide(){if(Handle!=IntPtr.Zero&&IsWindow(Handle))SetWindowPos(Handle,IntPtr.Zero,0,0,0,0,0x1|0x2|0x4|0x10|0x80);}
    public object Status(){RECT r;GetWindowRect(Handle,out r);return new {attached=Attached,error=Error,parent=GetParent(Handle).ToInt64(),left=r.L,top=r.T,right=r.R,bottom=r.B,circleDiameter=24,numberPixels=10};}
    public void Dispose(){if(Handle!=IntPtr.Zero)try{DestroyHandle();}catch{}}
}
