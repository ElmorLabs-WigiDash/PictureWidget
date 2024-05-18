using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Shapes;
using WigiDashWidgetFramework;
using WigiDashWidgetFramework.WidgetUtility;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;

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

        public bool OverlayWrap = true;
        public string OverlayText = string.Empty;
        public Color OverlayColor = Color.FromArgb(255, 255, 255);
        public Color VectorColor = Color.FromArgb(255, 255, 255);
        public Font OverlayFont;
        public int OverlayXPos = 0;
        public int OverlayYPos = 0;
        public int OverlayXOffset = 0;
        public int OverlayYOffset = 0;
        public double VectorScale = 0.8;
        public bool AutoScale = true;

        private Font DefaultFont = new Font("Basic Square 7 Solid", 20);
        private StringFormat OverlayStringFormat = new StringFormat(StringFormat.GenericTypographic);

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

            // Draw initial frame
            DrawFrame();

            parent.WidgetManager.GlobalThemeUpdated += WidgetManager_GlobalThemeUpdated;
        }

        private void WidgetManager_GlobalThemeUpdated()
        {
            DrawFrame();
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

                if(WidgetType != PictureWidgetType.Single || animated_gif != null) DrawFrame();

                Thread.Sleep(FrameMs);

                while (pause_task && run_task)
                {
                    Thread.Sleep(FrameMs);
                }
            }
        }

        public string CachedImagePath = "";
        public Image CachedImage = null;
        public (Color?, double?) CachedVectorArgs = (null, null);

        public void DrawFrame()
        {
            
            Image imageToDraw = null;

            if (WidgetType == PictureWidgetType.Single)
            {
                // GIF
                if (animated_gif != null)
                {
                    if (current_frame >= animated_gif.Images.Count)
                    {
                        current_frame = 0;
                    }

                    imageToDraw = animated_gif.Images[current_frame].Image;
                    FrameMs = animated_gif.Images[current_frame].Duration;

                    // Default GIF speed
                    if (FrameMs < 100) FrameMs = 100;
                    else if (FrameMs < 1) FrameMs = 250;

                    current_frame++;
                }

                // Normal Image
                else
                {
                    if (CachedImagePath == ImagePath && CachedImage != null && CachedVectorArgs == (VectorColor, VectorScale))
                    {
                        imageToDraw = CachedImage;
                    }
                    else
                    {
                        if (File.Exists(ImagePath))
                        {
                            /*if (CachedImage != null)
                            {
                                CachedImage.Dispose();
                            }*/

                            if (Path.GetExtension(ImagePath) == ".svg")
                            {
                                CachedVectorArgs = (VectorColor, VectorScale);
                                imageToDraw = GetBitmapFromSvg(ImagePath);
                            }
                            else
                            {
                                try
                                {
                                    byte[] imageBytes = File.ReadAllBytes(ImagePath);
                                    imageToDraw = Image.FromStream(new MemoryStream(imageBytes));
                                } catch { }
                            }

                            CachedImagePath = ImagePath;
                            CachedImage = imageToDraw;
                        }
                        else
                        {
                            // This already should be null, but for clarity
                            imageToDraw = null;
                        }
                    }
                }
            }
            else
            {
                if (current_frame >= FolderImages.Count)
                {
                    current_frame = 0;
                }

                if (FolderImages.Count > 0 && File.Exists(FolderImages[current_frame]))
                {
                    if (CachedImagePath == FolderImages[current_frame])
                    {
                        imageToDraw = CachedImage;
                    }
                    else
                    {
                        /*if (CachedImage != null)
                        {
                            CachedImage.Dispose();
                        }*/
                        try
                        {
                            byte[] imageBytes = File.ReadAllBytes(FolderImages[current_frame]);
                            imageToDraw = Image.FromStream(new MemoryStream(imageBytes));
                            CachedImagePath = FolderImages[current_frame];
                            CachedImage = imageToDraw;
                        } catch { }
                    }
                    FrameMs = 5000;
                }
                else
                {
                    FrameMs = 250;
                }

                current_frame++;

            }

            Color overlayColor = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryFgColor : OverlayColor;
            Font overlayFont = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryFont ?? DefaultFont : OverlayFont;

            OverlayStringFormat.Alignment = GetStringAlignment(OverlayXPos);
            OverlayStringFormat.LineAlignment = GetStringAlignment(OverlayYPos);

            Rectangle drawRect = new Rectangle(
                OverlayXOffset,
                OverlayYOffset,
                WidgetSize.ToSize().Width - OverlayXOffset,
                WidgetSize.ToSize().Height - OverlayYOffset
                );

            Color bgColor = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryBgColor : BackColor;

            if (drawing_mutex.WaitOne(mutex_timeout))
            {
                using (Graphics g = Graphics.FromImage(BitmapCurrent))
                {
                    g.Clear(bgColor);
                    if (imageToDraw != null)
                    {
                        g.DrawImageZoomedToFit(imageToDraw, WidgetSize.ToSize().Width, WidgetSize.ToSize().Height);
                    }
                    g.DrawStringAccurate(OverlayText, overlayFont, overlayColor, drawRect, OverlayWrap, OverlayStringFormat, AutoScale);
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
            try
            {
                string[] files = Directory.GetFiles(ImagePath);
                foreach (string file in files)
                {
                    if (file.Length > 3)
                    {
                        string file_end = file.Substring(file.Length - 4);
                        switch (file_end)
                        {
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
            } catch { }

            pause_task = false;

            DrawFrame();
        }

        public void ImportImage(string importPath, string fileId = "Image", bool doDraw = true)
        {
            if (doDraw) ReleaseImage();
            EnterSleep();
            WidgetObject.WidgetManager.RemoveFile(this, fileId);
            if (!WidgetObject.WidgetManager.StoreFile(this, fileId, importPath, out string outPath)) return;
            ExitSleep();
            if (doDraw) LoadImage(outPath);
        }

        public void ReleaseImage()
        {
            ImagePath = null;
            DrawFrame();
        }

        public void LoadImage(string path, bool drawFrame = true) {
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
                        } else
                        {
                            animated_gif = null;
                            current_frame = 0;
                        }
                    }
                }
            }
            catch (Exception ex) { }

            ImagePath = path;
            WidgetType = PictureWidgetType.Single;

            pause_task = false;

            DrawFrame();
        }

        public Bitmap GetBitmapFromSvg(string path)
        {

            int iconSize = Math.Min(WidgetSize.ToSize().Width, WidgetSize.ToSize().Height);
            int iconWidth = (int)(iconSize * VectorScale);
            int iconHeight = (int)(iconSize * VectorScale);

            Bitmap svgBitmap = null;

            try
            {
                var svgDocument = SvgDocument.Open(path);

                svgDocument.Color = new SvgColourServer(VectorColor);
                svgDocument.Fill = new SvgColourServer(VectorColor);


                svgBitmap = svgDocument.Draw(iconWidth, iconHeight);
                
            } catch { }

            Bitmap bitmap = new Bitmap(WidgetSize.ToSize().Width, WidgetSize.ToSize().Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                if(svgBitmap != null)
                {
                    g.DrawImage(svgBitmap, new PointF((WidgetSize.ToSize().Width - iconWidth) / 2, (WidgetSize.ToSize().Height - iconHeight) / 2));
                }
            }

            return bitmap;
        }

        public virtual void UpdateSettings()
        {
            DrawFrame();
        }

        public virtual void SaveSettings()
        {
            WidgetObject.WidgetManager.StoreSetting(this, "WidgetType", ((int)WidgetType).ToString());
            if (WidgetType == PictureWidgetType.Folder)
            {
                WidgetObject.WidgetManager.StoreSetting(this, "FolderPath", ImagePath);
            }

            WidgetObject.WidgetManager.StoreSetting(this, "BackColor", ColorTranslator.ToHtml(BackColor));
            WidgetObject.WidgetManager.StoreSetting(this, "VectorColor", ColorTranslator.ToHtml(VectorColor));
            WidgetObject.WidgetManager.StoreSetting(this, nameof(VectorScale), VectorScale.ToString(CultureInfo.InvariantCulture));
            WidgetObject.WidgetManager.StoreSetting(this, "OverlayText", OverlayText);
            WidgetObject.WidgetManager.StoreSetting(this, "OverlayColor", ColorTranslator.ToHtml(OverlayColor));
            WidgetObject.WidgetManager.StoreSetting(this, "OverlayFont", new FontConverter().ConvertToInvariantString(OverlayFont));

            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayXPos), OverlayXPos.ToString());
            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayYPos), OverlayYPos.ToString());

            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayXOffset), OverlayXOffset.ToString());
            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayYOffset), OverlayYOffset.ToString());

            WidgetObject.WidgetManager.StoreSetting(this, "AutoScale", AutoScale.ToString());
            WidgetObject.WidgetManager.StoreSetting(this, "WordWrap", OverlayWrap.ToString());
            WidgetObject.WidgetManager.StoreSetting(this, "UseGlobalTheme", UseGlobal.ToString());
        }

        public virtual void LoadSettings() {

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

            if (WidgetObject.WidgetManager.LoadSetting(this, "VectorColor", out string vecColor))
            {
                VectorColor = ColorTranslator.FromHtml(vecColor);
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, nameof(VectorScale), out string vectorScaleStr))
            {
                double.TryParse(vectorScaleStr, out VectorScale);
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

            if (WidgetObject.WidgetManager.LoadSetting(this, "AutoScale", out string autoScaleTxt))
            {
                if (bool.TryParse(autoScaleTxt, out bool tmpScale)) {
                    AutoScale = tmpScale;
                }
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, "WordWrap", out string wordWrapTxt))
            {
                if (bool.TryParse(wordWrapTxt, out bool tmpWrap))
                {
                    OverlayWrap = tmpWrap;
                }
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

            if (WidgetObject.WidgetManager.LoadSetting(this, "WidgetType", out string type))
            {
               
                int widget_type;
                if (int.TryParse(type, out widget_type))
                {
                    switch (widget_type)
                    {
                        case (int)PictureWidgetType.Single:
                            if (WidgetObject.WidgetManager.LoadFile(this, "Image", out string imagePath))
                            {
                                LoadImage(imagePath);
                            }
                            break;
                        case (int)PictureWidgetType.Folder:
                            if(WidgetObject.WidgetManager.LoadSetting(this, "FolderPath", out string folderPath)) { 
                                LoadFolder(folderPath); 
                            }
                            break;
                    }
                }
            }
            else
            {
                ImportImage(Path.Combine(_resourcePath, "icon.png"));
            }
        }
    }
}

