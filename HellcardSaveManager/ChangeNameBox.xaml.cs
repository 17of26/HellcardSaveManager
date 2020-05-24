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
    /// Interaction logic for ChangeNameBox.xaml
    /// </summary>
    public partial class ChangeNameBox : Window
    {
        public ChangeNameBox()
        {
            InitializeComponent();
        }

        private void OKButton(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
