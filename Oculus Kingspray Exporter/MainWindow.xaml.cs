using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Drawing;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.GZip;
using MediaDevices;
using System.Drawing.Imaging;

namespace Oculus_Kingspray_Exporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<SaveItem> saveItems;
        IEnumerable<MediaDevice> devices;
        public MainWindow()
        {
            InitializeComponent();
            devices = MediaDevice.GetDevices();
            try
            {

             
            using (var device = devices.First(d => d.Description == "Quest"))
            {
                device.Connect();
                var directories = device.GetDirectories(@"\Internal shared storage\Android\data\com.infectiousape.kingspray\files");

                bool hasKingspray = device.DirectoryExists(@"\Internal shared storage\Android\data\com.infectiousape.kingspray\files");
                Console.Write("Device has Kingspray:");
                Console.Write(hasKingspray);
                Console.Write("Fetching thumbnails...");
                List<string> saves = new List<string>();
                foreach (string d in directories)
                {
                    if (!d.Contains(@"Unity"))
                    {
                        saves.AddRange(device.GetDirectories(d));
                    }
                }
                saveItems = new List<SaveItem>();
                foreach (string save in saves)
                {
                    MemoryStream ms = new MemoryStream(65565);
                    string thumbpath = save + @"\Thumbnail.jpg";
                    device.DownloadFile(thumbpath, ms);
                    ms.Position = 0;
                    BitmapImage thumb = new BitmapImage();
                    thumb.BeginInit();
                    thumb.StreamSource = ms;
                    thumb.EndInit();
                    saveItems.Add(new SaveItem()
                    {
                        SavePath = save,
                        SaveLocation = save.Split('\\')[6],
                        SaveDateTime = save.Substring(save.LastIndexOf(@"\")).TrimStart('\\'),
                        Thumbnail = thumb
                    });
                }
                lbThumbnails.ItemsSource = saveItems;
                device.Disconnect();
            }
            }
            catch (Exception)
            {
                MessageBox.Show(@"There was an Error.  Please make sure your Quest is connected, you have Kingspray installed, and that you have granted file access permission on the quest.");
                Close();
            }
        }
        Bitmap paintImage;
        Bitmap metalImage;
        Bitmap roughnessImage;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int selectedThumbnail = lbThumbnails.SelectedIndex;

            /*
            Rooftops 4096*2048
            Top Dog Auto Repair  4096*2048
            Underpass 4096*2048
            Top Dog Garage 8192*1024
            Bullship Depot 2048*1024
            Bullship Storage 8192*1024
            Subway 8192*1024
            Cinema 4096*512
            Spraycan 1024*1024
            Baseball Cap 1024*512
            bunker 2048*2048
             */
            int height = 2048;//defaults so the compiler doesn't bitch
            int width = 2048;
            switch (saveItems[selectedThumbnail].SaveLocation)
            {
                case "Rooftops":
                case "TopDogAutoRepair":
                case "Underpass":
                
                    height = 2048;
                    width = 4096;
                    break;
                
                case "BullShippingDepot":
                    height = 1024;
                    width = 2048;
                    break;
                case "TopDogGarage": //this little fucker is reversed
                case "StorageRoom":
                case "Subway":
                    width = 8192;
                    height = 1024;
                    break;
                case "Cinema":
                    width = 4096;
                    height = 512; //this feels wrong, but okay
                    break;
                case "CustomizeSpraycan":
                    width = 1024;
                    height = 1024;
                    break;
                case "CustomizeBaseballCap":
                    width = 1024;
                    height = 512;
                    break;
                case "Bunker":
                    width = 2048;
                    height = 2048;
                    break;
                default:
                    break;
            }
            paintImage = new Bitmap(width,height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            metalImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            roughnessImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


            // Open the file for reading
            MemoryStream compressedPaintFile = new MemoryStream(width*height*4); //Why so much?  This accounts for even the remote possibility of ZERO compression
            MemoryStream compressedMetallicRougnessFile = new MemoryStream(width * height * 3);
            try
            {
                using (var device = devices.First(d => d.Description == "Quest"))
                {
                    device.Connect();
                    device.DownloadFile(saveItems[selectedThumbnail].SavePath+@"\Paint.ape", compressedPaintFile);
                    device.DownloadFile(saveItems[selectedThumbnail].SavePath + @"\Paint_Mask.ape", compressedMetallicRougnessFile);
                    device.Disconnect();
                }
            }
            catch (Exception)
            {
                MessageBox.Show(@"There was an error loading the file, maybe your quest was unplugged?  Please restart the program and try again.");
                throw;
            }
            
            compressedPaintFile.Position = 0;
            compressedMetallicRougnessFile.Position = 0;
            MemoryStream paintFile = new MemoryStream(width * height * 4);//assume 8192*1024*8bpp
            MemoryStream metallicRoughnessFile = new MemoryStream(width * height * 3);//assume 8192*1024*8bpp
            GZip.Decompress(compressedPaintFile, paintFile, false);
            GZip.Decompress(compressedMetallicRougnessFile, metallicRoughnessFile, false);
            
            //always reset your streams.
            paintFile.Position = 0;
            metallicRoughnessFile.Position = 0;


            //THE FAST WAY
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, paintImage.Width, paintImage.Height);
            BitmapData bitmapData = paintImage.LockBits(rect, ImageLockMode.ReadWrite, paintImage.PixelFormat);

            //MANGLE IT
            for (int i = 0; i < paintFile.Length; i += 4)
            {
                byte A = paintFile.GetBuffer()[i];
                byte R = paintFile.GetBuffer()[i + 1];
                byte G = paintFile.GetBuffer()[i + 2];
                byte B = paintFile.GetBuffer()[i + 3];

                paintFile.GetBuffer()[i] = B;
                paintFile.GetBuffer()[i + 1] = G;
                paintFile.GetBuffer()[i + 2] = R;
                paintFile.GetBuffer()[i + 3] = A;
            }
            IntPtr ptr = bitmapData.Scan0;
            int bytes = Math.Abs(bitmapData.Stride) * paintImage.Height;
            System.Runtime.InteropServices.Marshal.Copy(paintFile.GetBuffer(), 0, ptr, bytes);
            paintImage.UnlockBits(bitmapData);
            paintImage.RotateFlip(RotateFlipType.RotateNoneFlipY);

            //TIME TO GET FREAKY, WE HAVE 2 MAPS IN ONE IMAGE.
            //THE FAST WAY
            
            BitmapData metalData = metalImage.LockBits(rect, ImageLockMode.ReadWrite, metalImage.PixelFormat);

            //Duplicate the file buffer
            byte[] smoothValues = new byte[metallicRoughnessFile.Length];

            BitmapData smoothData = roughnessImage.LockBits(rect, ImageLockMode.ReadWrite, roughnessImage.PixelFormat);
            //MANGLE IT
            for (int i = 0; i < metallicRoughnessFile.Length; i += 3)
            {
                byte metal = metallicRoughnessFile.GetBuffer()[i];
                byte Rsmooth = metallicRoughnessFile.GetBuffer()[i + 1];
                //byte nothing = metallicRougnessFile.GetBuffer()[i + 2];

                metallicRoughnessFile.GetBuffer()[i] = metal;
                metallicRoughnessFile.GetBuffer()[i + 1] = metal;
                metallicRoughnessFile.GetBuffer()[i + 2] = metal;
                smoothValues[i] = (byte)(255-Rsmooth);
                smoothValues[i+1] = (byte)(255-Rsmooth);
                smoothValues[i+2] = (byte)(255-Rsmooth);
            }
            IntPtr ptrMet = metalData.Scan0;
            IntPtr ptrSmooth = smoothData.Scan0;
            int bytes2 = Math.Abs(metalData.Stride) * paintImage.Height;
            System.Runtime.InteropServices.Marshal.Copy(metallicRoughnessFile.GetBuffer(), 0, ptrMet, bytes2);
            System.Runtime.InteropServices.Marshal.Copy(smoothValues, 0, ptrSmooth, bytes2);
            metalImage.UnlockBits(metalData);
            roughnessImage.UnlockBits(smoothData);
            metalImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            roughnessImage.RotateFlip(RotateFlipType.RotateNoneFlipY);


            if (saveItems[selectedThumbnail].SaveLocation == "TopDogGarage")
            {
                paintImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                metalImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                roughnessImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            imgKing.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                paintImage.GetHbitmap(),
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(paintImage.Width, paintImage.Height)
            );
            
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image|*.png";
            saveFileDialog.Title = @"Save Images";
            saveFileDialog.ShowDialog();
            if(saveFileDialog.FileName != "")
            {
                try
                {
                    paintImage.Save(saveFileDialog.FileName.Split('.')[0] + "_Albedo" + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    metalImage.Save(saveFileDialog.FileName.Split('.')[0] + "_Metallic" + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    roughnessImage.Save(saveFileDialog.FileName.Split('.')[0] + "_Roughness" + ".png", System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception)
                {
                    MessageBox.Show(@"Unknown error while saving");
                    throw;
                }
                
            }
            
            //image.Save(openFiledialog.FileName+"paint.png", System.Drawing.Imaging.ImageFormat.Png);
                
            
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    public class SaveItem
    {
        public ImageSource Thumbnail { get; set; }
        public string SavePath { get; set; }
        public string SaveLocation { get; set; }
        public string SaveDateTime { get; set; }
    }
}
