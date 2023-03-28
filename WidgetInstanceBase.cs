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

        private SettingsUserControl _userControl;

        public UserControl GetSettingsControl() {
            if (_userControl == null)
            {
                _userControl = new SettingsUserControl(this);
            }
            return _userControl;
        }

        // Events
        public event WidgetUpdatedEventHandler WidgetUpdated;

    }
}
