using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
            
            
            try
            {

             devices = MediaDevice.GetDevices();
                
                using (var device = devices.First(d => d.Description == "Quest"))
                {
                    
                    device.Connect();
 
                    bool hasKingspray = device.DirectoryExists(@"\Internal shared storage\Android\data\com.infectiousape.kingspray\files");
                    Console.Write("Device has Kingspray:");
                    Console.Write(hasKingspray);
                    Console.Write("Fetching thumbnails...");

                    var directories = device.GetDirectories(@"\Internal shared storage\Android\data\com.infectiousape.kingspray\files");
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
                        if (device.FileExists(thumbpath))
                        {
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
                        else { Console.Write("Missing thumbnail in " + thumbpath); }
                    }
                    lbThumbnails.ItemsSource = saveItems;
                    device.Disconnect();
                    /**/
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream("Oculus_Kingspray_Exporter.Template.jpg");
            MemoryStream thumbnailStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(thumbnailStream);
            Bitmap thumbnailOverlay = new Bitmap(stream);

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = @"PNG Image | *.png";
            ofd.ShowDialog();
            if(ofd.FileName != "")
            {
                try
                {
                    Bitmap importFile = new Bitmap(ofd.FileName);
                    imgKing.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        importFile.GetHbitmap(),
                        IntPtr.Zero,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(importFile.Width, importFile.Height)
                    );
                    if (!Bitmap.IsCanonicalPixelFormat(System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        throw new Exception(@"Wrong Pixel Format");
                    //Check Image Dimensions to get valid import targets
                    /*
                    0x1 Rooftops 4096*2048
                    0x2 Top Dog Auto Repair  4096*2048
                    0x4 Underpass 4096*2048
                    0x8 Top Dog Garage 8192*1024
                    0x10 Bullship Depot 2048*1024
                    0x20 Bullship Storage 8192*1024
                    0x40 Subway 8192*1024
                    0x80 Cinema 4096*512
                    0x100 Spraycan 1024*1024
                    0x200 Baseball Cap 1024*512
                    0x400 bunker 2048*2048
                     */
                    bool isValidImport = true;
                    bool isGarage = false;
                    UInt16 locationMask = 0x0;
                    Scene_Selection dialog = new Scene_Selection();
                    
                    switch (importFile.Width)
                    {
                        case 4096:
                            switch (importFile.Height)
                            {
                                case 2048:
                                    //Rooftops, autorepair, underpass
                                    locationMask &= 0x7;
                                    dialog.btnAutoRepair.IsEnabled = true;
                                    dialog.btnRoof.IsEnabled = true;
                                    dialog.btnUnderpass.IsEnabled = true;
                                    break;
                                case 512:
                                    //cinema
                                    locationMask &= 0x80;
                                    dialog.btnCinema.IsEnabled = true;
                                    break;
                                default:
                                    //invalid
                                    isValidImport = false;
                                    break;
                            }
                            break;
                        case 8192:
                            switch (importFile.Height)
                            {
                                case 1024:
                                    //garage, storage, subway
                                    locationMask &= 0x68;
                                    dialog.btnGarage.IsEnabled = true;
                                    dialog.btnStorage.IsEnabled = true;
                                    dialog.btnSubway.IsEnabled = true;
                                    break;
                                default:
                                    //invalid
                                    isValidImport = false;
                                    break;
                            }
                            break;
                        case 2048:
                            switch (importFile.Height)
                            {
                                case 1024:
                                    //depot
                                    locationMask &= 0x10;
                                    dialog.btnDepot.IsEnabled = true;
                                    break;
                                case 2048:
                                    //bunker
                                    locationMask &= 0x400;
                                    dialog.btnBunker.IsEnabled = true;
                                    break;
                                default:
                                    //invalid
                                    isValidImport = false;
                                    break;
                            }
                            break;
                        case 1024:
                            switch (importFile.Height)
                            {
                                case 1024:
                                    //Spraycan
                                    locationMask &= 0x100;
                                    dialog.btnSpraycan.IsEnabled = true;
                                    break;
                                case 512:
                                    //ballcap
                                    locationMask &= 0x200;
                                    dialog.btnBaseballCap.IsEnabled = true;
                                    break;
                                default:
                                    //invalid
                                    isValidImport = false;
                                    break;
                            }
                            break;
                        default:
                            //invalid
                            isValidImport = false;
                            break;
                    }
                    if(isValidImport)
                    {
                        byte[] paintBuffer = new byte[importFile.Width * importFile.Height * 4];
                        byte[] maskBuffer = new byte[importFile.Width * importFile.Height * 3];
                        MemoryStream paintData = new MemoryStream(paintBuffer,0,paintBuffer.Length, true, true);
                        MemoryStream maskData = new MemoryStream(maskBuffer,0,maskBuffer.Length, true, true);

                        //flip image along Y
                        importFile.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        if (isGarage)
                            importFile.RotateFlip(RotateFlipType.RotateNoneFlipX);

                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, importFile.Width, importFile.Height);
                        importFile.MakeTransparent(System.Drawing.Color.FromArgb(0, 255, 255, 255));
                        BitmapData bitmapData = importFile.LockBits(rect, ImageLockMode.ReadWrite, importFile.PixelFormat);
                        IntPtr ptr = bitmapData.Scan0;
                        int bytes = Math.Abs(bitmapData.Stride) * importFile.Height;
                        System.Runtime.InteropServices.Marshal.Copy(ptr, paintData.GetBuffer(), 0, bytes);
                        importFile.UnlockBits(bitmapData);

                        //MANGLE IT
                        for (int i = 0; i < paintData.Length; i += 4)
                        {
                            byte B = paintData.GetBuffer()[i];
                            byte G = paintData.GetBuffer()[i + 1];
                            byte R = paintData.GetBuffer()[i + 2];
                            byte A = paintData.GetBuffer()[i + 3];
                            
                            paintData.GetBuffer()[i] = A;
                            paintData.GetBuffer()[i + 1] = R;
                            paintData.GetBuffer()[i + 2] = G;
                            paintData.GetBuffer()[i + 3] = B;
                        }
                        //piantData now contains the correct order bytes.

                        //Now the masks
                        //Bitmap maskImage = importFile.Clone(rect, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        Bitmap maskImage = new Bitmap(importFile.Width, importFile.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        BitmapData maskBits = maskImage.LockBits(rect, ImageLockMode.ReadWrite, maskImage.PixelFormat);
                        IntPtr ptr2 = maskBits.Scan0;
                        int bytes2 = Math.Abs(maskBits.Stride) * importFile.Height;
                        System.Runtime.InteropServices.Marshal.Copy(ptr2, maskData.GetBuffer(), 0, bytes2);
                        maskImage.UnlockBits(maskBits);

                        for (int i = 0; i < paintData.Length/4; i++)
                        {
                            byte A = paintData.GetBuffer()[i*4];
                            byte R = paintData.GetBuffer()[i*4 + 1];
                            byte G = paintData.GetBuffer()[i*4 + 2];
                            byte B = paintData.GetBuffer()[i*4 + 3];

                            byte rgb = (byte)(R + G + B);
                            maskData.GetBuffer()[i*3] = 0;//Since we have no metallic data to go off of, set it to 0
                            maskData.GetBuffer()[i*3 + 1] = rgb > 0? (byte)12 : (byte)0;//Smoothness: anywhere there is paint is 12
                            maskData.GetBuffer()[i*3 + 2] = 0;//unknown value so just set to 0
                            
                        }
                        //Now we have our Paint_Mask

                        //Make our streams for compression
                        MemoryStream compressedPaint = new MemoryStream();
                        MemoryStream compressedMask = new MemoryStream();
                        maskData.Position = 0;
                        paintData.Position = 0;
                        GZip.Compress(paintData, compressedPaint, false);
                        GZip.Compress(maskData, compressedMask, false);

                        DateTime dt = DateTime.Now;
                        string date = dt.ToString("yyyy-MM-dd_HH-mm-ss-tt");
                        compressedMask.Position = 0;
                        compressedPaint.Position = 0;
                        /*
                        using (FileStream file = new FileStream("Paint.ape", FileMode.Create, System.IO.FileAccess.Write))
                            compressedPaint.CopyTo(file);
                        using (FileStream file = new FileStream("Paint_Mask.ape", FileMode.Create, System.IO.FileAccess.Write))
                            compressedMask.CopyTo(file);
                            */
                        
                        dialog.ShowDialog();
                        string savePath = @"\Internal shared storage\Android\data\com.infectiousape.kingspray\files\" + dialog.Path + "\\"+date;
                        try
                        {
                            using (var device = devices.First(d => d.Description == "Quest"))
                            {
                                device.Connect();
                                if (!device.DirectoryExists(@"\Internal shared storage\Android\data\com.infectiousape.kingspray\files\" + dialog.Path))
                                    device.CreateDirectory(@"\Internal shared storage\Android\data\com.infectiousape.kingspray\files\" + dialog.Path);
                                device.CreateDirectory(savePath);
                                device.UploadFile(compressedMask, savePath + @"\Paint_Mask.ape");
                                device.UploadFile(compressedPaint, savePath + @"\Paint.ape");
                                stream.Position = 0;
                                device.UploadFile(stream, savePath + @"\Thumbnail.jpg");
                                device.Disconnect();
                            }
                        } catch (Exception)
                        {
                            MessageBox.Show(@"Error saving to Quest.  Please make sure you have your quest connected and file permission granted.");
                        }
                    }

                    else
                    {
                        MessageBox.Show("Invalid image dimensions for import.\nValid image sizes are:\n8192x1024\n4096x2048\n4096x512\n2048x2048\n2048x1024\n1024x1024\n1024x512\n\nYour Image Size:\n" + importFile.Width + "x" + importFile.Height, @"Invalid Image Size");
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
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
