using WigiDashWidgetFramework;
using WigiDashWidgetFramework.WidgetUtility;
using System;
using System.Drawing;
using System.Windows.Controls;

namespace PictureWidget {
    public partial class PictureWidgetInstance : IWidgetInstance {

        public IWidgetObject WidgetObject { get; set; }

        public Guid Guid { get; set; }

        public WidgetSize WidgetSize { get; set; }

        public UserControl GetSettingsControl() {
            return new SettingsUserControl(this);
        }

        // Events
        public event WidgetUpdatedEventHandler WidgetUpdated;

    }
}
