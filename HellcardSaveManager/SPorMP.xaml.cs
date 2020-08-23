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

namespace HellcardSaveManager
{
    /// <summary>
    /// Interaction logic for SPorMP.xaml
    /// </summary>
    public partial class SPorMP : Window
    {
        public SPorMP()
        {
            InitializeComponent();
        }

        private void SPClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void MPClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
