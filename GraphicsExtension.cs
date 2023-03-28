﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}