using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Shapes;
using WigiDashWidgetFramework;
using WigiDashWidgetFramework.WidgetUtility;
using Path = System.IO.Path;

namespace PictureWidget {
    public partial class PictureWidgetInstance {

        // Functionality
        public void RequestUpdate() {
            if (drawing_mutex.WaitOne(mutex_timeout))
            {
                UpdateWidget();
                drawing_mutex.ReleaseMutex();
            }
        }

        public virtual void ClickEvent(ClickType click_type, int x, int y) {
            if(click_type == ClickType.Single) {
            }
        }

        public void Dispose() {
            pause_task = true;
            run_task = false;
        }

        public void EnterSleep() {
            pause_task = true;
        }

        public void ExitSleep() {
            pause_task = false;
        }

        // Class specific
        private Mutex drawing_mutex = new Mutex();
        public const int mutex_timeout = 100;

        private Thread task_thread;
        private volatile bool run_task = false;
        private volatile bool pause_task = false;
        
        public Bitmap BitmapCurrent;

        public bool UseGlobal = false;

        public Color BackColor;

        public string OverlayText = string.Empty;
        public Color OverlayColor = Color.FromArgb(255, 255, 255);
        public Font OverlayFont;
        public int OverlayXPos = 0;
        public int OverlayYPos = 0;
        public int OverlayXOffset = 0;
        public int OverlayYOffset = 0;

        // https://social.microsoft.com/Forums/en-US/fcb7d14d-d15b-4336-971c-94a80e34b85e/editing-animated-gifs-in-c?forum=netfxbcl
        public class AnimatedGif {

            private List<AnimatedGifFrame> mImages = new List<AnimatedGifFrame>();
            PropertyItem mTimes;
            public AnimatedGif(Image img, int frames, int width, int height) {
                if (frames == 1)
                {
                    // Not animated
                    mImages.Add(new AnimatedGifFrame(new Bitmap(img), 0));
                }
                else if (frames < 1)
                {
                    // No frames
                    mImages.Add(new AnimatedGifFrame(new Bitmap(width, height, PixelFormat.Format16bppRgb565), 0));
                }
                else
                {
                    byte[] times = img.GetPropertyItem(0x5100).Value;
                    int frame = 0;
                    for (; ; )
                    {
                        int dur = BitConverter.ToInt32(times, 4 * frame) * 10;
                        Bitmap new_bmp = new Bitmap(width, height, PixelFormat.Format16bppRgb565);
                        using (Graphics g = Graphics.FromImage(new_bmp))
                        {
                            g.DrawImage(img, 0, 0, width, height);
                        }
                        mImages.Add(new AnimatedGifFrame(new_bmp, dur));
                        if (++frame >= frames) break;
                        img.SelectActiveFrame(FrameDimension.Time, frame);
                    }
                }
            }
            public List<AnimatedGifFrame> Images { get { return mImages; } }
        }

        public class AnimatedGifFrame {
            private int mDuration;
            private Image mImage;
            internal AnimatedGifFrame(Image img, int duration) {
                mImage = img; mDuration = duration;
            }
            public Image Image { get { return mImage; } }
            public int Duration { get { return mDuration; } }
        }

        public AnimatedGif animated_gif;

        public volatile int current_frame;

        public string ImagePath = "";

        public enum PictureWidgetType : int { Single, Folder };

        public PictureWidgetType WidgetType;

        private List<string> FolderImages = new List<string>();

        private string _resourcePath;

        public PictureWidgetInstance(IWidgetObject parent, WidgetSize widget_size, Guid instance_guid, string resourcePath)
        {
            Initialize(parent, widget_size, instance_guid, resourcePath);
            LoadSettings();
            StartTask();
        }
         
        public void Initialize(IWidgetObject parent, WidgetSize widget_size, Guid instance_guid, string resourcePath)
        {
            this.WidgetObject = parent;
            this.Guid = instance_guid;

            this._resourcePath = resourcePath;

            this.WidgetSize = widget_size;

            Size Size = widget_size.ToSize();

            BitmapCurrent = new Bitmap(Size.Width, Size.Height, PixelFormat.Format16bppRgb565);
            OverlayFont = new Font("Basic Square 7 Solid", 20);
        }

        public void StartTask()
        {
            pause_task = false;
            run_task = true;

            ThreadStart thread_start = new ThreadStart(UpdateTask);
            task_thread = new Thread(thread_start);
            task_thread.IsBackground = true;
            task_thread.Start();
        }

        private void UpdateWidget() {
            //if (drawing_mutex.WaitOne(mutex_timeout))
            //{
                if (BitmapCurrent != null)
                {
                    WidgetUpdatedEventArgs e = new WidgetUpdatedEventArgs();
                    e.WaitMax = 1000;
                    e.WidgetBitmap = BitmapCurrent;

                    WidgetUpdated?.Invoke(this, e);
                }

                //drawing_mutex.ReleaseMutex();
            //}
        }

        private int FrameMs = 250;

        private void UpdateTask() {
            while(run_task) {

                DrawFrame();
                current_frame++;

                Thread.Sleep(FrameMs);

                while (pause_task && run_task)
                {
                    Thread.Sleep(FrameMs);
                }
            }
        }

        string cachedImagePath = "";
        Image cachedImage = null;

        private void DrawFrame()
        {
            if (drawing_mutex.WaitOne(mutex_timeout))
            {
                using (Graphics g = Graphics.FromImage(BitmapCurrent))
                {
                    Color bgColor = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryBgColor : BackColor;
                    g.Clear(bgColor);
                    Image imageToDraw = null;

                    if (WidgetType == PictureWidgetType.Single)
                    {
                        // GIF
                        if (animated_gif != null)
                        {
                            if (current_frame > animated_gif.Images.Count - 1)
                            {
                                current_frame = 0;
                            }

                            imageToDraw = animated_gif.Images[current_frame].Image;
                            FrameMs = animated_gif.Images[current_frame].Duration;

                            // Default GIF speed
                            if (FrameMs < 100) FrameMs = 100;
                            else if (FrameMs < 1) FrameMs = 250;
                        }

                        // Normal Image
                        else
                        {
                            if (cachedImagePath == ImagePath)
                            {
                                imageToDraw = cachedImage;
                            }
                            else
                            {
                                if (File.Exists(ImagePath))
                                {
                                    if (cachedImage != null)
                                    {
                                        cachedImage.Dispose();
                                    }

                                    if (Path.GetExtension(ImagePath) == ".svg")
                                    {
                                        imageToDraw = GetBitmapFromSvg(ImagePath);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            byte[] imageBytes = File.ReadAllBytes(ImagePath);
                                            imageToDraw = Image.FromStream(new MemoryStream(imageBytes));
                                            cachedImagePath = ImagePath;
                                            cachedImage = imageToDraw;
                                        } catch { }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (current_frame > FolderImages.Count - 1)
                        {
                            return;
                        }

                        if (File.Exists(FolderImages[current_frame]))
                        {
                            if (cachedImagePath == FolderImages[current_frame])
                            {
                                imageToDraw = cachedImage;
                            }
                            else
                            {
                                if (cachedImage != null)
                                {
                                    cachedImage.Dispose();
                                }
                                try
                                {
                                    byte[] imageBytes = File.ReadAllBytes(FolderImages[current_frame]);
                                    imageToDraw = Image.FromStream(new MemoryStream(imageBytes));
                                    cachedImagePath = FolderImages[current_frame];
                                    cachedImage = imageToDraw;
                                } catch { }
                            }
                            FrameMs = 5000;
                        }
                        else
                        {
                            FrameMs = 250;
                        }
                    }

                    if (imageToDraw != null)
                    {
                        g.DrawImageZoomedToFit(imageToDraw, WidgetSize.ToSize().Width, WidgetSize.ToSize().Height);
                    }

                    Color overlayColor = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryFgColor : OverlayColor;
                    Font overlayFont = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryFont ?? new Font("Basic Square 7 Solid", 20) : OverlayFont;
                    
                    StringFormat overlayFormat = new StringFormat(StringFormat.GenericTypographic);
                    overlayFormat.Alignment = GetStringAlignment(OverlayXPos);
                    overlayFormat.LineAlignment = GetStringAlignment(OverlayYPos);
                    overlayFormat.FormatFlags = overlayFormat.FormatFlags | StringFormatFlags.NoWrap;

                    g.DrawOverlay(OverlayText, overlayColor, overlayFont, WidgetSize.ToSize().Width, WidgetSize.ToSize().Height, OverlayXOffset, OverlayYOffset, overlayFormat);
                }

                UpdateWidget();

                drawing_mutex.ReleaseMutex();
            }
        }

        public StringAlignment GetStringAlignment(int index)
        {
            switch(index)
            {
                default:
                case 0:
                    return StringAlignment.Center;

                case 1:
                    return StringAlignment.Near;

                case 2:
                    return StringAlignment.Far;
            }
        }

        public void LoadFolder(string path) {
            if (!Directory.Exists(path)) return;
            pause_task = true;

            current_frame = 0;
            ImagePath = path;
            WidgetType = PictureWidgetType.Folder;

            // Find files in folder
            FolderImages = new List<string>();
            string[] files = Directory.GetFiles(ImagePath);
            foreach(string file in files) {
                if(file.Length > 3) {
                    string file_end = file.Substring(file.Length - 4);
                    switch(file_end) {
                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                        case ".gif":
                        case ".tif":
                        case ".bmp":
                        case ".ico":
                            FolderImages.Add(file); break;
                    }
                }
            }

            pause_task = false;
        }

        public void ImportImage(string importPath)
        {
            ReleaseImage();
            EnterSleep();
            WidgetObject.WidgetManager.RemoveFile(this, "Image");
            if (!WidgetObject.WidgetManager.StoreFile(this, "Image", importPath, out string outPath)) return;
            ExitSleep();
            LoadImage(outPath);
        }

        public void ReleaseImage()
        {
            ImagePath = null;
            DrawFrame();
        }

        public void LoadImage(string path) {
            if (!File.Exists(path)) return;

            pause_task = true;

            animated_gif = null;

            // Handle gif
            try
            {
                if (Path.GetExtension(path) == ".gif")
                {
                    using (Image img = Image.FromFile(path))
                    {
                        int frames = img.GetFrameCount(FrameDimension.Time);
                        if (frames > 1)
                        {
                            animated_gif = new AnimatedGif(img, frames, img.Width, img.Height);
                            current_frame = 0;
                        }
                    }
                }
            }
            catch (Exception ex) { }

            ImagePath = path;
            WidgetType = PictureWidgetType.Single;

            pause_task = false;
        }

        private Bitmap GetBitmapFromSvg(string path)
        {

            Bitmap bitmap = new Bitmap(WidgetSize.ToSize().Width, WidgetSize.ToSize().Height, PixelFormat.Format16bppRgb565);
            Color bgColor = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryBgColor : BackColor;
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(bgColor);
            }

            try
            {
                var svgDocument = SvgDocument.Open(path);

                svgDocument.Color = new SvgColourServer(OverlayColor);
                svgDocument.Fill = new SvgColourServer(OverlayColor);

                var padding = 0;
                var iconSize = Math.Min(bitmap.Width, bitmap.Height);
                var scale = 0.8;
                int iconWidth = (int)((iconSize - (padding * 2)) * scale);
                int iconHeight = (int)((iconSize - (padding * 2)) * scale);

                Bitmap svgBitmap = svgDocument.Draw(iconWidth, iconHeight);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(svgBitmap, new PointF((WidgetSize.ToSize().Width - iconWidth) / 2, (WidgetSize.ToSize().Height - iconHeight) / 2));
                }
            } catch { }

            return bitmap;
        }

        public void UpdateSettings()
        {
            DrawFrame();
        }

        public virtual void SaveSettings() {
            WidgetObject.WidgetManager.StoreSetting(this, "WidgetFirstRun", string.Empty);
            WidgetObject.WidgetManager.StoreSetting(this, "WidgetType", ((int)WidgetType).ToString());
            WidgetObject.WidgetManager.StoreSetting(this, "BackColor", ColorTranslator.ToHtml(BackColor));

            WidgetObject.WidgetManager.StoreSetting(this, "OverlayText", OverlayText);
            WidgetObject.WidgetManager.StoreSetting(this, "OverlayColor", ColorTranslator.ToHtml(OverlayColor));
            WidgetObject.WidgetManager.StoreSetting(this, "OverlayFont", new FontConverter().ConvertToInvariantString(OverlayFont));

            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayXPos), OverlayXPos.ToString());
            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayYPos), OverlayYPos.ToString());

            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayXOffset), OverlayXOffset.ToString());
            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayYOffset), OverlayYOffset.ToString());

            WidgetObject.WidgetManager.StoreSetting(this, "UseGlobalTheme", UseGlobal.ToString());
        }

        public virtual void LoadSettings() {
            if (WidgetObject.WidgetManager.LoadSetting(this, "WidgetType", out string type))
            {
                if (WidgetObject.WidgetManager.LoadFile(this, "Image", out string imagePath))
                {
                    int widget_type;
                    if (int.TryParse(type, out widget_type))
                    {
                        switch (widget_type)
                        {
                            case (int)PictureWidgetType.Single:
                                LoadImage(imagePath); break;
                            case (int)PictureWidgetType.Folder:
                                LoadFolder(imagePath); break;
                        }
                    }
                }
            }
            else
            {
                ImportImage(Path.Combine(_resourcePath, "icon.png"));
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, "OverlayText", out string overlayText))
            {
                OverlayText = overlayText;
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, "OverlayFont", out var strOverlayFont))
            {
                OverlayFont = new FontConverter().ConvertFromInvariantString(strOverlayFont) as Font;
            }
            else
            {
                OverlayFont = new Font("Basic Square 7 Solid", 20);
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, "OverlayColor", out string fgColor))
            {
                OverlayColor = ColorTranslator.FromHtml(fgColor);
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, nameof(OverlayXPos), out string overlayXPosStr))
            {
                int.TryParse(overlayXPosStr, out OverlayXPos);
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, nameof(OverlayYPos), out string overlayYPosStr))
            {
                int.TryParse(overlayYPosStr, out OverlayYPos);
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, nameof(OverlayXOffset), out string overlayXOffsetStr))
            {
                int.TryParse(overlayXOffsetStr, out OverlayXOffset);
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, nameof(OverlayYOffset), out string overlayYOffsetStr))
            {
                int.TryParse(overlayYOffsetStr, out OverlayYOffset);
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, "UseGlobalTheme", out string globalTheme))
            {
                bool.TryParse(globalTheme, out UseGlobal);
            } else
            {
                UseGlobal = WidgetObject.WidgetManager.PreferGlobalTheme;
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, "BackColor", out string bgColor))
            {
                BackColor = ColorTranslator.FromHtml(bgColor);
            } else
            {
                BackColor = WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryBgColor;
            }
        }
    }
}

