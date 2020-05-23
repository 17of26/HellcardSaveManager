using System.Windows;

namespace HellcardSaveManager
{
    public partial class App
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            var window = new MainView {DataContext = new MainVm()};
            window.Show();
        }
    }
}
