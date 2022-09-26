﻿using FrontierWidgetFramework;
using FrontierWidgetFramework.WidgetUtility;
using System;
using System.Drawing;
using System.Windows.Controls;

namespace PictureWidget {
    public partial class PictureWidgetInstance : IWidgetInstance {

        private PictureWidget parent;
        public IWidgetObject WidgetObject { 
            get {
                return parent;
            }
        }

        public Guid Guid { get; set; }

        public WidgetSize WidgetSize { get; set; }

        public UserControl GetSettingsControl() {
            return new SettingsUserControl(this);
        }

        // Events
        public event WidgetUpdatedEventHandler WidgetUpdated;

    }
}
