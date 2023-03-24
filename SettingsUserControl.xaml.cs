using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;

namespace PictureWidget {
    /// <summary>
    /// Interaction logic for SettingsUserControl.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl {


        PictureWidgetInstance parent;

        public SettingsUserControl(PictureWidgetInstance widget_instance) {

            //_contentLoaded = true;
            //Utility.Utility.LoadXaml(this);

            InitializeComponent();

            parent = widget_instance;

            comboBoxType.Items.Add("Single");
            comboBoxType.Items.Add("Folder");

            comboBoxType.SelectedIndex = (int)parent.WidgetType;
            textBoxFile.Text = parent.ImagePath;
            try {
                bgColorSelect.Content = ColorTranslator.ToHtml(parent.BackColor);
            } catch { }

            textOverlay.Text = parent.OverlayText;

            try
            {
                overlayColorSelect.Content = ColorTranslator.ToHtml(parent.OverlayColor);
            } catch { }

            overlayFontSelect.Content = new FontConverter().ConvertToInvariantString(parent.OverlayFont);
            overlayFontSelect.Tag = parent.OverlayFont;
        }

        private void colorSelect_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button caller)
            {
                Color defaultColor = ColorTranslator.FromHtml(caller.Content.ToString());
                Color selectedColor = parent.WidgetObject.WidgetManager.RequestColorSelection(defaultColor);
                caller.Content = ColorTranslator.ToHtml(selectedColor);
            }
        }

        private void buttonFile_Click(object sender, RoutedEventArgs e) {
            switch(comboBoxType.SelectedIndex) {
                case (int)PictureWidgetInstance.PictureWidgetType.Single:
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.tif;*.bmp;*.ico";
                    bool? result = ofd.ShowDialog();
                    if(result != null && result != false) {
                        textBoxFile.Text = ofd.FileName;
                    }
                    break;
                case (int)PictureWidgetInstance.PictureWidgetType.Folder:
                    System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                    if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                        textBoxFile.Text = fbd.SelectedPath;
                    }
                    break;
            }
        }

        private void buttonApply_Click(object sender, RoutedEventArgs e) {

            try {
                parent.BackColor = ColorTranslator.FromHtml(bgColorSelect.Content.ToString());
                parent.OverlayColor = ColorTranslator.FromHtml(overlayColorSelect.Content.ToString());
            } catch { }

            switch(comboBoxType.SelectedIndex) {
                case (int)PictureWidgetInstance.PictureWidgetType.Single:
                    parent.LoadImage(textBoxFile.Text);
                    break;
                case (int)PictureWidgetInstance.PictureWidgetType.Folder:
                    parent.LoadFolder(textBoxFile.Text);
                    break;
            }

            parent.OverlayText = textOverlay.Text;
            parent.OverlayFont = overlayFontSelect.Tag as Font;
            parent.UseGlobal = globalThemeCheck.IsChecked ?? false;

            parent.OverlayXOffset = lastValidXOffset;
            parent.OverlayYOffset = lastValidYOffset;

            parent.RequestUpdate();
            parent.SaveSettings();
        }

        private void overlayFontSelect_Click(object sender, RoutedEventArgs e)
        {
            Font defaultFont = parent.OverlayFont;
            Font selectedFont = parent.WidgetObject.WidgetManager.RequestFontSelection(defaultFont);

            if (sender is Button caller)
            {
                caller.Content = new FontConverter().ConvertToInvariantString(selectedFont);
                caller.Tag = selectedFont;
            }
        }

        private int lastValidXOffset = 0;
        private int lastValidYOffset = 0;
        private void OverlayXOffset_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            bool result = int.TryParse(e.Text, out int lastInput);
            if (!result && e.Text != "-")
            {
                e.Handled = true;
                //OverlayXOffset.Text = lastValidXOffset.ToString();
            } else
            {
                int.TryParse(OverlayXOffset.Text, out lastValidXOffset);
            }
        }

        private void OverlayYOffset_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            bool result = int.TryParse(e.Text, out int lastInput);
            if (!result && e.Text != "-")
            {
                e.Handled = true;
                //OverlayYOffset.Text = lastValidYOffset.ToString();
            }
            else
            {
                int.TryParse(OverlayYOffset.Text, out lastValidYOffset);
            }
        }

        private void OverlayOffset_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy ||
             e.Command == ApplicationCommands.Cut ||
             e.Command == ApplicationCommands.Paste)
                {
                    e.Handled = true;
                }
        }
    }
}
