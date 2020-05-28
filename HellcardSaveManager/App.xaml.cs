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

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Please seek help in the Hellcard Discord Channel and mention following error:\n\n"
                     + e.Exception.GetType().ToString() + ": " + e.Exception.Message + "\n\n\n"
                     + "(This tool was not created by nor is supported by Thing Trunk, it's a community project.)", "Error");

            e.Handled = true;
        }
    }
}
