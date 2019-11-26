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
using System.Windows.Shapes;

namespace Oculus_Kingspray_Exporter
{
    /// <summary>
    /// Interaction logic for Thumbnail_Selection.xaml
    /// </summary>
    public partial class Thumbnail_Selection : Window
    {
        // Prep stuff needed to remove close button on window
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        public bool customThumb;
        public string thumbPath;
        public Thumbnail_Selection()
        {
            InitializeComponent();

        }

        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            // Code to remove close box from window
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = @"JPG Image | *.jpg";
            ofd.ShowDialog();
            if(ofd.FileName != "")
            {
                Bitmap thumb = new Bitmap(ofd.FileName);
                if(thumb.Width == 640 && thumb.Height == 360)
                {
                    customThumb = true;
                    thumbPath = ofd.FileName;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid file dimensions!  640x360 required.\nYour Image: " + thumb.Width + "x" + thumb.Height, "Invalid file");
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            customThumb = false;
            this.Close();
        }
    }
}
