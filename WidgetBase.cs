﻿using System;
using System.Collections.Generic;
using System.Drawing;
using WigiDashWidgetFramework;
using WigiDashWidgetFramework.WidgetUtility;

namespace PictureWidget
{
    public partial class PictureWidgetServer : IWidgetObject {

        // Identity
        public Guid Guid {
            get {
                return new Guid(GetType().Assembly.GetName().Name);
            }
        }
        public string Name {
            get {
                return PictureWidget.Properties.Resources.PictureWidgetServer_Name_Picture;
            }
        }
        public string Description {
            get {
                return PictureWidget.Properties.Resources.PictureWidgetServer_Description_ShowPicturesIncludingGIFs;
            }
        }
        public string Author {
            get {
                return "ElmorLabs";
            }
        }
        public string Website {
            get {
                return "https://elmorlabs.com/";
            }
        }
        public Version Version {
            get {
                return new Version(1,0,9);
            }
        }

        // Capabilities
        public SdkVersion TargetSdk {
            get {
                return WidgetUtility.CurrentSdkVersion;
            }
        }

        public List<WidgetSize> SupportedSizes {
            get {
                //return new List<WidgetSize>() { WidgetSize.SIZE_5X4 };
                List<WidgetSize> widget_size_list = new List<WidgetSize>();
                for(int y = 1; y < 5; y++) {
                    for(int x = 1; x < 6; x++) {
                        widget_size_list.Add(new WidgetSize(x, y));
                    }
                }
                return widget_size_list;
                //return new List<WidgetSize>() { WidgetSize.SIZE_1X1, WidgetSize.SIZE_2X1, WidgetSize.SIZE_2X2, WidgetSize.SIZE_4X3 };
            }
        }

        // Functionality
        public IWidgetManager WidgetManager { get; set; }

        // Error handling
        public string LastErrorMessage { get; set; }

        public Bitmap PreviewImage {
            get {
                return GetWidgetPreview(new WidgetSize(1, 1));
            }
        }
    }

}
