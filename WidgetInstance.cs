using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WigiDashWidgetFramework;
using WigiDashWidgetFramework.WidgetUtility;

namespace PictureWidget {
    public partial class PictureWidgetInstance {

        // Functionality
        public void RequestUpdate() {

            if(drawing_mutex.WaitOne(mutex_timeout)) {
            //lock(BitmapCurrent) { 
                UpdateWidget();
                drawing_mutex.ReleaseMutex();
            }
        }

        public void ClickEvent(ClickType click_type, int x, int y) {
            if(click_type == ClickType.Single) {
                //pause_task = !pause_task;
            }
        }

        public void Dispose() {
            pause_task = false;
            run_task = false;
        }

        public void EnterSleep() {
            pause_task = true;
        }

        public void ExitSleep() {
            //lock(BitmapCurrent) {
                //UpdateWidget();
            //}
            if(drawing_mutex.WaitOne(mutex_timeout)) {
                UpdateWidget();
                drawing_mutex.ReleaseMutex();
            }
            pause_task = false;
        }

        // Class specific
        private Mutex drawing_mutex = new Mutex();
        public const int mutex_timeout = 1000;

        private Thread task_thread;
        private volatile bool run_task = false;
        private volatile bool pause_task = false;
        
        public Bitmap BitmapCurrent;

        public bool UseGlobal = false;

        public Color BackColor = Color.FromArgb(35, 35, 35);

        public string OverlayText = string.Empty;
        public Color OverlayColor = Color.FromArgb(255, 255, 255);
        public Font OverlayFont;
        public int OverlayXOffset = 0;
        public int OverlayYOffset = 0;

        // https://social.microsoft.com/Forums/en-US/fcb7d14d-d15b-4336-971c-94a80e34b85e/editing-animated-gifs-in-c?forum=netfxbcl
        public class AnimatedGif {

            private List<AnimatedGifFrame> mImages = new List<AnimatedGifFrame>();
            PropertyItem mTimes;
            public AnimatedGif(Image img, int frames, int width, int height) {
                //Image img = Image.FromFile(path);
               // int frames = img.GetFrameCount(FrameDimension.Time);
                if(frames <= 1) throw new ArgumentException("Image not animated");
                byte[] times = img.GetPropertyItem(0x5100).Value;
                int frame = 0;
                for(; ; ) {
                    int dur = BitConverter.ToInt32(times, 4 * frame) * 10;
                    Bitmap new_bmp = new Bitmap(width, height);
                    using(Graphics g = Graphics.FromImage(new_bmp)) {
                        g.DrawImage(img, 0, 0, width, height);
                    }
                    mImages.Add(new AnimatedGifFrame(new_bmp, dur));
                    if(++frame >= frames) break;
                    img.SelectActiveFrame(FrameDimension.Time, frame);
                }
                img.Dispose();
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

        public int current_frame;

        public string ImagePath = "";

        public enum PictureWidgetType : int { Single, Folder };

        public PictureWidgetType WidgetType;

        private List<string> FolderImages;

        public PictureWidgetInstance(IWidgetObject parent, WidgetSize widget_size, Guid instance_guid)
        {
            Initialize(parent, widget_size, instance_guid);
            LoadSettings();
        }

        public void Initialize(IWidgetObject parent, WidgetSize widget_size, Guid instance_guid)
        {
            this.WidgetObject = parent;
            this.Guid = instance_guid;

            this.WidgetSize = widget_size;

            Size Size = widget_size.ToSize();

            BitmapCurrent = new Bitmap(Size.Width, Size.Height);

            BlankWidget();
        }

        private void UpdateWidget() {
            WidgetUpdatedEventArgs e = new WidgetUpdatedEventArgs();
            e.Offset = Point.Empty;
            if(animated_gif != null) {
                e.WaitMax = animated_gif.Images[current_frame].Duration;
            } else {
                e.WaitMax = mutex_timeout;
            }
            e.WidgetBitmap = BitmapCurrent;

            WidgetUpdated?.Invoke(this, e);
        }

        private void BlankWidget()
        {
            BitmapCurrent = new Bitmap(WidgetSize.ToSize().Width, WidgetSize.ToSize().Height);
            if (drawing_mutex.WaitOne(mutex_timeout))
            {
                using (Graphics g = Graphics.FromImage(BitmapCurrent))
                {
                    Color clearColor = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryBgColor : BackColor;
                    g.Clear(clearColor);
                }
                drawing_mutex.ReleaseMutex();
            }
            UpdateWidget();
        }

        private void UpdateTask() {

            while(run_task) {

                while(pause_task) {
                    Thread.Sleep(100);
                }

                if(WidgetType == PictureWidgetType.Single) {
                    try {
                        // Show next frame
                        if(animated_gif != null && drawing_mutex.WaitOne(mutex_timeout)) {
                            //lock(BitmapCurrent) {
                            using (Graphics g = Graphics.FromImage(BitmapCurrent)) {
                                g.Clear(BackColor);
                                g.DrawImageZoomedToFit(animated_gif.Images[current_frame].Image, WidgetSize.ToSize().Width, WidgetSize.ToSize().Height);
                            }
                            DrawOverlay();
                            //}

                            drawing_mutex.ReleaseMutex();
                            UpdateWidget();
                        }
                    } catch(Exception ex) { }

                } else if(WidgetType == PictureWidgetType.Folder) {
                    // Clear animated gif
                    animated_gif = null;

                    // Show next picture

                    // Load image
                    Bitmap img = null;

                    try {
                        img = new Bitmap(FolderImages[current_frame]);
                    } catch(Exception ex) {

                    }

                    if(img != null) {

                        if(drawing_mutex.WaitOne(mutex_timeout)) {
                        //lock(BitmapCurrent) { 
                            try {
                                lock(BitmapCurrent) {
                                    if(img.Width > 0 && img.Height > 0) {
                                        // Draw image
                                        using (Graphics g = Graphics.FromImage(BitmapCurrent)) {
                                            g.Clear(BackColor);
                                            g.DrawImageZoomedToFit(img, WidgetSize.ToSize().Width, WidgetSize.ToSize().Height);
                                        }
                                        DrawOverlay();
                                    }
                                }
                            } catch(Exception ex) {
                            }

                            drawing_mutex.ReleaseMutex();
                            UpdateWidget();
                        }
                    }

                    
                }

                if (animated_gif == null)
                {
                    Thread.Sleep(5000);
                    current_frame++;
                    if (current_frame == FolderImages.Count)
                    {
                        current_frame = 0;
                    }
                }
                else
                {
                    if (animated_gif.Images[current_frame].Duration < 0) Thread.Sleep(200);
                    else Thread.Sleep(animated_gif.Images[current_frame].Duration);

                    current_frame++;
                    if (current_frame == animated_gif.Images.Count && animated_gif != null)
                    {
                        current_frame = 0;
                    }
                }
            }

        }

        public void DrawOverlay()
        {
            using (Graphics g = Graphics.FromImage(BitmapCurrent))
            {
                Color overlayColor = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryFgColor : OverlayColor;
                Brush overlayBrush = new SolidBrush(overlayColor);

                Font overlayFont = UseGlobal ? WidgetObject.WidgetManager.GlobalWidgetTheme.PrimaryFont : OverlayFont;

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                StringFormat format = new StringFormat(StringFormat.GenericTypographic);

                g.DrawString(OverlayText, overlayFont, overlayBrush, OverlayXOffset, OverlayYOffset, format);
            }
        }

        public void LoadFolder(string path) {

            run_task = false;

            if(task_thread != null) {
                task_thread.Join(500);
            }

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

            if(FolderImages.Count > 0) {
                pause_task = false;
                run_task = true;

                ThreadStart thread_start = new ThreadStart(UpdateTask);
                task_thread = new Thread(thread_start);
                task_thread.IsBackground = true;
                task_thread.Start();
            }
        }

        public void LoadImage(string path) {

            run_task = false;

            if(task_thread != null && task_thread.IsAlive) {
                pause_task = false;
                task_thread.Join(500);
            }

            Image img = null;

            try {
                // Load image
                if (File.Exists(path)) img = Image.FromFile(path);
            } catch(Exception ex) {
                //MessageBox.Show(ex.Message);
            }

            if(img != null) {
                //lock(BitmapCurrent) {
                if(drawing_mutex.WaitOne(mutex_timeout)) {
                    // Draw image
                    //lock(BitmapCurrent) {
                    using(Graphics g = Graphics.FromImage(BitmapCurrent)) {
                        g.Clear(BackColor);
                        if(animated_gif == null) {
                            g.Clear(BackColor);
                            g.DrawImageZoomedToFit(img, WidgetSize.ToSize().Width, WidgetSize.ToSize().Height);
                        }
                    }
                    DrawOverlay();
                    UpdateWidget();
                    drawing_mutex.ReleaseMutex();
                }

                // Handle gif
                animated_gif = null;
                try {
                    int frames = img.GetFrameCount(FrameDimension.Time);
                    if(frames > 1) {
                        animated_gif = new AnimatedGif(img, frames, img.Width, img.Height);
                        current_frame = 0;
                    }
                } catch(Exception ex) { }

                img.Dispose();

                ImagePath = path;
                WidgetType = PictureWidgetType.Single;

                if(animated_gif != null) {
                    pause_task = false;
                    run_task = true;

                    ThreadStart thread_start = UpdateTask;
                    task_thread = new Thread(thread_start);
                    task_thread.IsBackground = true;
                    task_thread.Start();
                }
            }
        }
        
        public void UpdateSettings()
        {
            LoadImage(ImagePath);
        }

        public void SaveSettings() {
            WidgetObject.WidgetManager.StoreSetting(this, "ImagePath", ImagePath);
            WidgetObject.WidgetManager.StoreSetting(this, "WidgetType", ((int)WidgetType).ToString());
            WidgetObject.WidgetManager.StoreSetting(this, "BackColor", ColorTranslator.ToHtml(BackColor));

            WidgetObject.WidgetManager.StoreSetting(this, "OverlayText", OverlayText);
            WidgetObject.WidgetManager.StoreSetting(this, "OverlayColor", ColorTranslator.ToHtml(OverlayColor));
            WidgetObject.WidgetManager.StoreSetting(this, "OverlayFont", new FontConverter().ConvertToInvariantString(OverlayFont));
            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayXOffset), OverlayXOffset.ToString());
            WidgetObject.WidgetManager.StoreSetting(this, nameof(OverlayYOffset), OverlayYOffset.ToString());

            WidgetObject.WidgetManager.StoreSetting(this, "UseGlobalTheme", UseGlobal.ToString());

            if (ImagePath == "")
            {
                BlankWidget();
            }
        }

        public void LoadSettings() {
            string path, type;
            if(WidgetObject.WidgetManager.LoadSetting(this, "ImagePath", out path)) {
                if(WidgetObject.WidgetManager.LoadSetting(this, "WidgetType", out type)) {
                    int widget_type;
                    if(int.TryParse(type, out widget_type)) {
                        switch(widget_type) {
                            case (int)PictureWidgetType.Single:
                                LoadImage(path); break;
                            case (int)PictureWidgetType.Folder:
                                LoadFolder(path); break;
                        }
                    }
                }
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
            }

            if (WidgetObject.WidgetManager.LoadSetting(this, "BackColor", out string bgColor))
            {
                BackColor = ColorTranslator.FromHtml(bgColor);
                BlankWidget();
            }
        }
    }
}

