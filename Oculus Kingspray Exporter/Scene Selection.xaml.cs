using System;
using System.Collections.Generic;
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
    /// Interaction logic for Scene_Selection.xaml
    /// </summary>
    public partial class Scene_Selection : Window
    {
        public Scene_Selection()
        {
            InitializeComponent();
        }

        public string Path
        {
            get;
            set;
        }

        private void BtnRoof_Click(object sender, RoutedEventArgs e)
        {
            Path = @"Rooftops";
            this.Close();
        }

        private void BtnStorage_Click(object sender, RoutedEventArgs e)
        {
            Path = @"StorageRoom";
            this.Close();
        }

        private void BtnAutoRepair_Click(object sender, RoutedEventArgs e)
        {
            Path = @"TopDogAutoRepair";
            this.Close();
        }

        private void BtnGarage_Click(object sender, RoutedEventArgs e)
        {
            Path = @"TopDogGarage";
            this.Close();
        }

        private void BtnSubway_Click(object sender, RoutedEventArgs e)
        {
            Path = @"Subway";
            this.Close();
        }

        private void BtnCinema_Click(object sender, RoutedEventArgs e)
        {
            Path = @"Cinema";
            this.Close();
        }

        private void BtnUnderpass_Click(object sender, RoutedEventArgs e)
        {
            Path = @"Underpass";
            this.Close();
        }

        private void BtnSpraycan_Click(object sender, RoutedEventArgs e)
        {
            Path = @"CustomizeSpraycan";
            this.Close();
        }

        private void BtnDepot_Click(object sender, RoutedEventArgs e)
        {
            Path = @"BullShippingDepot";
            this.Close();
        }

        private void BtnBaseballCap_Click(object sender, RoutedEventArgs e)
        {
            Path = @"CustomizeBaseballCap";
            this.Close();
        }

        private void BtnBunker_Click(object sender, RoutedEventArgs e)
        {
            Path = @"StartScreen";
            this.Close();
        }
    }
}
