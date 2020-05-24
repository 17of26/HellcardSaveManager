using System.Windows;

namespace HellcardSaveManager
{
    /// <summary>
    /// Interaction logic for MessageCheckBox.xaml
    /// </summary>
    public partial class MessageCheckBox : Window
    {
        public MessageCheckBox()
        {
            InitializeComponent();
        }

        private void OKClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }


    }
}
