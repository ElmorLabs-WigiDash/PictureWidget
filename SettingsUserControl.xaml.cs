using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using HandyControl.Data;
using Color = System.Drawing.Color;
using Path = System.IO.Path;

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

            comboBoxType.Items.Add(PictureWidget.Properties.Resources.SettingsUserControl_SettingsUserControl_SingleImage);
            comboBoxType.Items.Add(PictureWidget.Properties.Resources.SettingsUserControl_SettingsUserControl_FolderSlideshow);

            comboBoxType.SelectedIndex = (int)parent.WidgetType;
            textBoxFile.Text = Path.GetFileName(parent.ImagePath);

            try {
                bgColorSelect.Content = ColorTranslator.ToHtml(parent.BackColor);
            } catch { }

            textOverlay.Text = parent.OverlayText;

            try
            {
                overlayColorSelect.Content = ColorTranslator.ToHtml(parent.OverlayColor);
            } catch { }

            try
            {
                vectorColorSelect.Content = ColorTranslator.ToHtml(parent.VectorColor);
            }
            catch { }

            vectorScaleSelect.Value = parent.VectorScale * 100;

            overlayFontSelect.Content = new FontConverter().ConvertToInvariantString(parent.OverlayFont);
            overlayFontSelect.Tag = parent.OverlayFont;

            OverlayXPos.SelectedIndex = parent.OverlayXPos;
            OverlayYPos.SelectedIndex = parent.OverlayYPos;

            OverlayXOffset.Value = parent.OverlayXOffset;
            OverlayYOffset.Value = parent.OverlayYOffset;

            globalThemeCheck.IsChecked = parent.UseGlobal;
            wordWrapChk.IsChecked = parent.OverlayWrap;
            autoScaleChk.IsChecked = parent.AutoScale;

            overlayColorSelect.IsEnabled = !parent.UseGlobal;
            overlayFontSelect.IsEnabled = !parent.UseGlobal;
            bgColorSelect.IsEnabled = !parent.UseGlobal;
        }

        private void buttonFile_Click(object sender, RoutedEventArgs e) {
            switch(comboBoxType.SelectedIndex) {
                case (int)PictureWidgetInstance.PictureWidgetType.Single:
                    //OpenFileDialog ofd = new OpenFileDialog();
                    //ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.tif;*.bmp;*.ico;*.svg";
                    //bool? result = ofd.ShowDialog();
                    //if(result != null && result != false) {
                    //    textBoxFile.Text = Path.GetFileName(ofd.FileName);
                    //    parent.ImportImage(ofd.FileName);
                    //}
                    string result = parent.WidgetObject.WidgetManager.RequestImageSelection(string.Empty);
                    if (result != null && result != string.Empty)
                    {
                        textBoxFile.Text = Path.GetFileName(result);
                        parent.ImportImage(result);
                    }
                    break;
                case (int)PictureWidgetInstance.PictureWidgetType.Folder:
                    System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                    if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                        textBoxFile.Text = fbd.SelectedPath;
                        parent.LoadFolder(fbd.SelectedPath);
                    }
                    break;
            }


            parent.SaveSettings();
            parent.UpdateSettings();

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

            parent.OverlayFont = overlayFontSelect.Tag as Font;

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void globalThemeCheck_Click(object sender, RoutedEventArgs e)
        {
            parent.UseGlobal = globalThemeCheck.IsChecked ?? false;
            overlayColorSelect.IsEnabled = !parent.UseGlobal;
            overlayFontSelect.IsEnabled = !parent.UseGlobal;
            bgColorSelect.IsEnabled = !parent.UseGlobal;

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void OverlayOffset_ValueChanged(object sender, HandyControl.Data.FunctionEventArgs<double> e)
        {
            parent.OverlayXOffset = (int)OverlayXOffset.Value;
            parent.OverlayYOffset = (int)OverlayYOffset.Value;

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void OverlayPos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OverlayXPos.SelectedIndex == -1 || OverlayYPos.SelectedIndex == -1)
                return;

            parent.OverlayXPos = OverlayXPos.SelectedIndex;
            parent.OverlayYPos = OverlayYPos.SelectedIndex;

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void textOverlay_TextChanged(object sender, TextChangedEventArgs e)
        {
            parent.OverlayText = textOverlay.Text;

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void colorSelect_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button caller)
            {
                Color defaultColor = ColorTranslator.FromHtml(caller.Content.ToString());
                Color selectedColor = parent.WidgetObject.WidgetManager.RequestColorSelection(defaultColor);
                caller.Content = ColorTranslator.ToHtml(selectedColor);
            }

            try
            {
                parent.BackColor = ColorTranslator.FromHtml(bgColorSelect.Content.ToString());
                parent.OverlayColor = ColorTranslator.FromHtml(overlayColorSelect.Content.ToString());
                parent.VectorColor = ColorTranslator.FromHtml(vectorColorSelect.Content.ToString());
            }
            catch { }

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void clearFile_Click(object sender, RoutedEventArgs e)
        {
            parent.WidgetObject.WidgetManager.RemoveFile(parent, @"Image");
            parent.ImagePath = string.Empty;
            textBoxFile.Text = string.Empty;

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void autoScaleChk_Click(object sender, RoutedEventArgs e)
        {
            parent.AutoScale = autoScaleChk.IsChecked == true;

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void wordWrapChk_Click(object sender, RoutedEventArgs e)
        {
            parent.OverlayWrap = wordWrapChk.IsChecked == true;

            parent.SaveSettings();
            parent.UpdateSettings();
        }

        private void VectorScaleSelect_OnValueChanged(object sender, FunctionEventArgs<double> e)
        {
            if (parent == null) return;
            parent.VectorScale = vectorScaleSelect.Value / 100;

            parent.SaveSettings();
            parent.UpdateSettings();
        }
    }
}
