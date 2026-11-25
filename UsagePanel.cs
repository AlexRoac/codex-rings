using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class Theme
{
    internal static readonly Color Background = Color.FromArgb(24, 24, 24);
    internal static readonly Color Surface = Color.FromArgb(33, 33, 33);
    internal static readonly Color Border = Color.FromArgb(53, 53, 53);
    internal static readonly Color Text = Color.FromArgb(242, 242, 242);
    internal static readonly Color Muted = Color.FromArgb(166, 166, 166);

    internal static GraphicsPath Round(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class DarkMenuColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground { get { return Theme.Surface; } }
    public override Color ImageMarginGradientBegin { get { return Theme.Surface; } }
    public override Color ImageMarginGradientMiddle { get { return Theme.Surface; } }
    public override Color ImageMarginGradientEnd { get { return Theme.Surface; } }
    public override Color MenuBorder { get { return Theme.Border; } }
    public override Color MenuItemBorder { get { return Color.FromArgb(70, 70, 70); } }
    public override Color MenuItemSelected { get { return Color.FromArgb(49, 49, 49); } }
    public override Color SeparatorDark { get { return Theme.Border; } }
    public override Color SeparatorLight { get { return Theme.Border; } }
}

internal sealed class QuietButton : Button
{
    internal bool Primary;
    private bool hover;

    internal QuietButton(string text, bool primary = false)
    {
        Text = text;
        Primary = primary;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Segoe UI", 9.5f);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        AccessibleName = text;
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(1, 1, Width - 2, Height - 2);
        using (var shape = Theme.Round(rect, 9))
        using (var fill = new SolidBrush(Primary ? (hover ? Color.FromArgb(220, 220, 220) : Theme.Text) : (hover ? Color.FromArgb(49, 49, 49) : Theme.Surface)))
        using (var border = new Pen(Focused ? Theme.Muted : Theme.Border))
        {
            g.FillPath(fill, shape);
            if (!Primary || Focused) g.DrawPath(border, shape);
        }
        TextRenderer.DrawText(g, Text, Font, ClientRectangle,
            !Enabled ? Color.FromArgb(116, 116, 116) : Primary ? Theme.Background : Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

internal sealed class QuotaCard : Control
{
    internal WindowLimit Limit;
    internal string Caption;
    internal bool Current;

    internal QuotaCard(string caption)
    {
        Caption = caption;
        AccessibleRole = AccessibleRole.StaticText;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        float scale = Width / 400f;
        var g = e.Graphics;
        g.ScaleTransform(scale, scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using (var shape = Theme.Round(new RectangleF(.5f, .5f, 399, 139), 13))
        using (var fill = new SolidBrush(Theme.Surface))
        using (var border = new Pen(Theme.Border))
        {
            g.FillPath(fill, shape);
            g.DrawPath(border, shape);
        }
        using (var title = new Font("Segoe UI", 10, FontStyle.Bold))
        using (var value = new Font("Segoe UI", 23, FontStyle.Regular))
        using (var small = new Font("Segoe UI", 9))
        using (var text = new SolidBrush(Theme.Text))
        using (var muted = new SolidBrush(Theme.Muted))
        using (var right = new StringFormat { Alignment = StringAlignment.Far })
        {
            g.DrawString(Caption, title, text, 18, 14);
            string number = Current && Limit != null ? Math.Round(Limit.Used, 0, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture) + "%" : "—";
            g.DrawString(number, value, text, 15, 35);
            g.DrawString("usado", small, muted, 19, 78);
            string remaining = Current && Limit != null ? Math.Round(100 - Limit.Used).ToString(CultureInfo.InvariantCulture) + "% disponible" : "Sin lectura actual";
            g.DrawString(remaining, small, muted, new RectangleF(130, 63, 251, 24), right);
            using (var track = new SolidBrush(Color.FromArgb(60, 60, 60)))
            using (var path = Theme.Round(new RectangleF(18, 102, 364, 4), 2)) g.FillPath(track, path);
            if (Current && Limit != null && Limit.Used > 0)
            {
                using (var pen = new Pen(Theme.Text, 4))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 20, 104, 20 + (float)(360 * Limit.Used / 100), 104);
                }
            }
            string reset = Limit == null ? "Conecta tu cuenta para consultar este límite" : Limit.ResetText();
            g.DrawString(reset, small, muted, new RectangleF(18, 115, 366, 22));
        }
    }
}

internal sealed class UsagePanel : Form
{
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int length);
    private readonly QuotaCard five = new QuotaCard("Límite de 5 horas");
    private readonly QuotaCard week = new QuotaCard("Límite semanal");
    private readonly Label account;
    private readonly Label status;
    private readonly Label notice;
    private readonly QuietButton refresh;
    private readonly QuietButton login;
    private readonly CheckBox startup;
    private bool updating;
    internal event EventHandler RefreshRequested;
    internal event EventHandler LoginRequested;
    internal event EventHandler CodexRequested;
    internal event EventHandler StartupChanged;
    internal bool StartWithWindows { get { return startup.Checked; } set { updating = true; startup.Checked = value; updating = false; } }

    internal UsagePanel()
    {
        Text = "Codex Rings";
        AccessibleName = "Codex Rings, límites de uso";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(448, 620);
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;
        KeyPreview = true;
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        var title = LabelAt("Codex Rings", 24, 24, 340, 35, 20, Theme.Text, FontStyle.Bold);
        title.MouseDown += DragHeader;
        LabelAt("Tus límites, de un vistazo.", 26, 66, 360, 23, 10, Theme.Muted).MouseDown += DragHeader;
        var close = new QuietButton("×") { Bounds = new Rectangle(392, 22, 32, 32), Font = new Font("Segoe UI", 16), AccessibleName = "Cerrar panel" };
        close.Click += (s, e) => Close();
        Controls.Add(close);
        account = LabelAt("Conectando con Codex…", 26, 108, 380, 22, 9, Theme.Muted);
        five.Bounds = new Rectangle(24, 144, 400, 140);
        week.Bounds = new Rectangle(24, 296, 400, 140);
        Controls.Add(five);
        Controls.Add(week);
        status = LabelAt("Consultando tu cuenta…", 26, 452, 396, 20, 9, Theme.Muted);
        notice = LabelAt("", 26, 478, 396, 32, 8.5f, Theme.Muted);
        notice.AutoEllipsis = true;
        refresh = new QuietButton("Actualizar") { Bounds = new Rectangle(24, 520, 124, 40) };
        login = new QuietButton("Conectar cuenta", true) { Bounds = new Rectangle(160, 520, 264, 40) };
        refresh.Click += (s, e) => { if (RefreshRequested != null) RefreshRequested(this, EventArgs.Empty); };
        login.Click += (s, e) => { if (LoginRequested != null) LoginRequested(this, EventArgs.Empty); };
        Controls.Add(refresh);
        Controls.Add(login);
        startup = new CheckBox { Text = "Iniciar con Windows", Bounds = new Rectangle(26, 580, 182, 22), ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand, AutoSize = false };
        startup.CheckedChanged += (s, e) => { if (!updating && StartupChanged != null) StartupChanged(this, EventArgs.Empty); };
        Controls.Add(startup);
        var settings = new LinkLabel { Text = "Configurar Codex", LinkColor = Theme.Muted, ActiveLinkColor = Theme.Text, VisitedLinkColor = Theme.Muted, Bounds = new Rectangle(252, 581, 172, 22), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9) };
        settings.LinkClicked += (s, e) => { if (CodexRequested != null) CodexRequested(this, EventArgs.Empty); };
        Controls.Add(settings);
        MouseDown += DragHeader;
    }

    private Label LabelAt(string text, int x, int y, int width, int height, float size, Color color, FontStyle weight = FontStyle.Regular)
    {
        var label = new Label { Text = text, Bounds = new Rectangle(x, y, width, height), Font = new Font("Segoe UI", size, weight), ForeColor = color, BackColor = Theme.Background };
        Controls.Add(label);
        return label;
    }

    private void DragHeader(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int dark = 1, corners = 2;
        DwmSetWindowAttribute(Handle, 20, ref dark, 4);
        DwmSetWindowAttribute(Handle, 33, ref corners, 4);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theme.Round(new RectangleF(.5f, .5f, ClientSize.Width - 1, ClientSize.Height - 1), 15))
        using (var pen = new Pen(Theme.Border)) e.Graphics.DrawPath(pen, path);
    }

    internal void UpdateUsage(WindowLimit primary, WindowLimit weekly, bool current, string plan, string error, DateTime updated, bool busy)
    {
        five.Limit = primary;
        five.Current = current;
        five.AccessibleName = "5 horas: " + (current && primary != null ? primary.Used + " por ciento usado" : "sin datos actuales");
        week.Limit = weekly;
        week.Current = current;
        week.AccessibleName = "Semanal: " + (current && weekly != null ? weekly.Used + " por ciento usado" : "sin datos actuales");
        five.Invalidate();
        week.Invalidate();
        account.Text = current ? "Plan " + (string.IsNullOrEmpty(plan) ? "ChatGPT" : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plan)) + "  ·  Cuenta conectada" : "Conecta tu cuenta de ChatGPT";
        status.Text = busy ? "Actualizando límites…" : current ? "Actualizado a las " + updated.ToLocalTime().ToString("HH:mm:ss") + "  ·  Cada 30 s" : "Sin datos actuales";
        notice.Text = error ?? "Los círculos y las barras muestran el porcentaje usado.";
        notice.ForeColor = error == null ? Theme.Muted : Color.FromArgb(229, 190, 137);
        refresh.Enabled = !busy;
        refresh.Text = busy ? "Actualizando…" : "Actualizar";
        login.Enabled = !busy;
        login.Text = current ? "Cambiar cuenta" : "Conectar cuenta";
    }

    internal void SaveImage(string path)
    {
        using (var bitmap = new Bitmap(Width, Height))
        {
            DrawToBitmap(bitmap, new Rectangle(Point.Empty, Size));
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
