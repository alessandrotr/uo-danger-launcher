using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UoDangerLauncher
{
    public class GoldProgressBar : Control
    {
        private int _value;
        private int _maximum = 100;
        private ProgressBarStyle _style = ProgressBarStyle.Continuous;
        private int _marqueeOffset;
        private System.Windows.Forms.Timer? _marqueeTimer;

        public int Value
        {
            get => _value;
            set { _value = Math.Clamp(value, 0, _maximum); Invalidate(); }
        }

        public int Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(1, value); Invalidate(); }
        }

        public ProgressBarStyle Style
        {
            get => _style;
            set
            {
                if (_style == value) return;
                _style = value;
                if (_style == ProgressBarStyle.Marquee)
                {
                    _marqueeTimer ??= new System.Windows.Forms.Timer { Interval = 30 };
                    _marqueeTimer.Tick -= MarqueeTick;
                    _marqueeTimer.Tick += MarqueeTick;
                    _marqueeTimer.Start();
                }
                else
                {
                    _marqueeTimer?.Stop();
                }
                Invalidate();
            }
        }

        private void MarqueeTick(object? sender, EventArgs e)
        {
            _marqueeOffset = (_marqueeOffset + 4) % (Width + 200);
            Invalidate();
        }

        public GoldProgressBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            Height = 16;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int radius = Height / 2;
            var bounds = new Rectangle(0, 0, Width, Height);

            // Track background
            using (var trackPath = RoundedRect(bounds, radius))
            using (var trackBrush = new SolidBrush(Color.FromArgb(40, 40, 45)))
            {
                g.FillPath(trackBrush, trackPath);
            }

            if (_style == ProgressBarStyle.Marquee)
            {
                int barWidth = 120;
                int x = _marqueeOffset - 200;
                int clampedX = Math.Max(0, x);
                int clampedW = Math.Min(barWidth, Width - clampedX);
                if (clampedW > radius * 2)
                {
                    var barRect = new Rectangle(clampedX, 0, clampedW, Height);
                    using var barPath = RoundedRect(barRect, radius);
                    using var brush = new LinearGradientBrush(
                        barRect, Color.FromArgb(212, 175, 55), Color.FromArgb(180, 140, 30), 0f);
                    g.FillPath(brush, barPath);
                }
            }
            else if (_value > 0 && _maximum > 0)
            {
                int fillWidth = (int)((long)_value * Width / _maximum);
                fillWidth = Math.Clamp(fillWidth, radius * 2, Width);
                var fillRect = new Rectangle(0, 0, fillWidth, Height);

                using var fillPath = RoundedRect(fillRect, radius);
                using var fillBrush = new LinearGradientBrush(
                    fillRect, Color.FromArgb(222, 185, 55), Color.FromArgb(180, 140, 30), 90f);
                g.FillPath(fillBrush, fillPath);

                // Subtle shine on top half
                var shineRect = new Rectangle(1, 1, fillWidth - 2, Height / 2);
                if (shineRect.Width > 0 && shineRect.Height > 0)
                {
                    using var shineBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
                    using var shinePath = RoundedRectTop(shineRect, Math.Max(1, radius - 1));
                    g.FillPath(shineBrush, shinePath);
                }
            }
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
            if (disposing)
            {
                _marqueeTimer?.Stop();
                _marqueeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
