using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace UoDangerLauncher
{
    public class GoldButton : Button
    {
        private float _glowIntensity;
        private readonly System.Windows.Forms.Timer _animTimer;
        private bool _hovering;
        private bool _pressing;

        public GoldButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += AnimTick;
        }

        private void AnimTick(object? sender, EventArgs e)
        {
            float target = _hovering ? 1f : 0f;
            const float step = 0.08f;
            if (Math.Abs(_glowIntensity - target) < step)
            {
                _glowIntensity = target;
                _animTimer.Stop();
            }
            else
            {
                _glowIntensity += _hovering ? step : -step;
            }
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovering = true;
            if (Enabled) _animTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovering = false;
            _pressing = false;
            if (Enabled) _animTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _pressing = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressing = false;
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled) { _glowIntensity = 0; _animTimer.Stop(); }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            const int radius = 14;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Button body — subtle lighten on hover
            Color baseColor = Enabled ? BackColor : Color.FromArgb(100, 85, 25);
            Color bodyColor = Enabled
                ? LerpColor(baseColor, Color.FromArgb(222, 185, 60), _glowIntensity * 0.2f)
                : baseColor;
            if (_pressing && Enabled)
                bodyColor = LerpColor(bodyColor, Color.FromArgb(160, 130, 25), 0.3f);

            using (var bodyPath = RoundedRect(rect, radius))
            using (var bodyBrush = new SolidBrush(bodyColor))
            {
                g.FillPath(bodyBrush, bodyPath);
            }

            // Top highlight (subtle glass effect)
            var highlightRect = new Rectangle(2, 2, Width - 5, Height / 2 - 2);
            if (highlightRect.Width > 0 && highlightRect.Height > 0)
            {
                using var highlightPath = RoundedRectTop(highlightRect, Math.Max(1, radius - 1));
                using var highlightBrush = new SolidBrush(
                    Color.FromArgb(Enabled ? 35 : 10, 255, 255, 255));
                g.FillPath(highlightBrush, highlightPath);
            }

            // Text
            var textColor = Enabled ? ForeColor : Color.FromArgb(60, 60, 60);
            TextRenderer.DrawText(g, Text, Font, rect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Max(1, radius * 2);
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath RoundedRectTop(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Max(1, radius * 2);
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _animTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
