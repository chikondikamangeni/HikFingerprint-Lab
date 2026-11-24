using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HikFingerprintLab;

// Decorative illustration only: no real fingerprint data is rendered.
internal sealed class FingerprintGraphic : Control
{
    private float _angle;
    private bool _scanning;
    internal int Result { get; set; }

    internal FingerprintGraphic()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = false;
        AccessibleName = "Decorative fingerprint scan status";
        BackColor = Color.FromArgb(27, 32, 43);
    }

    internal void Advance(bool busy)
    {
        bool changed = busy != _scanning;
        _scanning = busy;
        if (busy) _angle = (_angle + 12f) % 360f;
        if (busy || changed) Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        float size = Math.Min(168, Math.Min(Width - 12, Height - 8));
        if (size <= 0) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform((Width - size) / 2, (Height - size) / 2);
        g.ScaleTransform(size / 150f, size / 150f);
        Color ink = _scanning ? Color.FromArgb(96, 165, 250) : Result < 0 ? Color.FromArgb(251, 169, 126) : Result > 0 ? Color.FromArgb(74, 222, 170) : Color.FromArgb(140, 176, 223);
        using var wash = new SolidBrush(Color.FromArgb(30, ink));
        using var ring = new Pen(Color.FromArgb(85, ink), 1.4f);
        using var stroke = new Pen(ink, 3.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.FillEllipse(wash, 7, 7, 136, 136);
        g.DrawEllipse(ring, 7, 7, 136, 136);
        if (_scanning) g.DrawArc(stroke, 7, 7, 136, 136, _angle, 75);
        g.DrawBezier(stroke, 35, 76, 35, 14, 115, 14, 115, 76);
        g.DrawBezier(stroke, 43, 64, 43, 25, 107, 25, 107, 64);
        using var p = new GraphicsPath();
        p.AddBezier(42, 95, 52, 82, 52, 74, 52, 64);
        p.AddBezier(52, 64, 52, 33, 98, 33, 98, 64);
        p.AddBezier(98, 64, 98, 86, 103, 95, 113, 103);
        g.DrawPath(stroke, p);
        p.Reset();
        p.AddBezier(50, 111, 62, 94, 62, 77, 62, 65);
        p.AddBezier(62, 65, 62, 46, 88, 46, 88, 65);
        p.AddBezier(88, 65, 88, 100, 96, 109, 104, 117);
        g.DrawPath(stroke, p);
        g.DrawBezier(stroke, 63, 119, 74, 100, 74, 80, 74, 64);
        g.DrawBezier(stroke, 81, 86, 81, 108, 86, 118, 92, 125);
        g.DrawLine(stroke, 72, 126, 76, 115);
        g.DrawBezier(stroke, 105, 76, 106, 83, 110, 88, 115, 91);
    }
}
