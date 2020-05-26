using System.Diagnostics;
using System.Windows.Navigation;

namespace HellcardSaveManager
{
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

    }
}
