using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WigiDashWidgetFramework.WidgetUtility;

namespace PictureWidget
{
    public static class GraphicsExtension
    {
        public static void DrawImageZoomedToFit(this Graphics graphics, Image image, double maxW, double maxH)
        {
            double hScale = (double)image.Height / (double)image.Width; // Width is 1, height is x.xx
            double wScale = (double)image.Width / (double)image.Height; // Height is 1, width is x.xx

            int drawH;
            int drawW;
            double multiplier = 1;

            if (hScale < 1)
            {
                // If width is longer (landscape photo)
                drawW = (int)Math.Round(maxW);
                drawH = (int)Math.Round(maxW * hScale);

                if (drawH > maxH)
                {
                    multiplier = maxH / drawH;
                }
            }
            else
            {
                // If height is longer (portrait photo)
                drawH = (int)Math.Round(maxH);
                drawW = (int)Math.Round(maxH * wScale);

                if (drawW > maxW)
                {
                    multiplier = maxW / drawW;
                }
            }

            drawW = (int)Math.Round(drawW * multiplier);
            drawH = (int)Math.Round(drawH * multiplier);

            double xPos = (maxW - drawW) / 2;
            double yPos = (maxH - drawH) / 2;

            graphics.DrawImage(image, (int)xPos, (int)yPos, drawW, drawH);
        }

        public static bool AutoScale { get; set; } = true;
        private const int staticXOffset = 1;
        //private const TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
        private static StringFormat stringFormat = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        public static void DrawStringAccurate(this Graphics g, string text, Font drawFont, Color color, Rectangle border, bool doWrap = false, StringFormat optFormat = null)
        {
            border.X += staticXOffset;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            StringFormat format;
            if (optFormat != null)
            {
                format = optFormat;
            }
            else
            {
                format = stringFormat;
            }

            if (!doWrap) format.FormatFlags |= StringFormatFlags.NoWrap;

            Font finalFont = drawFont;

            if (AutoScale)
            {
                int fontSize = g.GetFontSize(text, border, drawFont, format);
                finalFont = new Font(drawFont.FontFamily, fontSize);
            }

            //TextRenderer.DrawText(g, DrawStringAccurate, adjustedFont, border, color, Color.Transparent, doWrap ? flags | TextFormatFlags.WordBreak : flags);
            g.DrawString(text, finalFont, new SolidBrush(color), border, format);
        }

        private static int GetFontSize(this Graphics g, string text, RectangleF rect, Font PreferedFont, StringFormat format, int maxFontSize = 32)
        {
            while (maxFontSize > 6)
            {
                using (var font = new Font(PreferedFont.FontFamily, maxFontSize))
                {
                    var calc = g.MeasureString(text, font, (int)rect.Width, format);
                    if (calc.Height <= rect.Height && calc.Width <= rect.Width)
                    {
                        break;
                    }
                }
                maxFontSize -= 1;
            }
            return maxFontSize;
        }
    }
}
