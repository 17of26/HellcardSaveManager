using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;

namespace HellcardSaveManager
{
    public partial class App
    {
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!args.Name.Contains("SimpleCrypt"))
                return null;
            
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HellcardSaveManager.Resources.SimpleCrypt.dll"))
            {
                if (stream == null) 
                    return null;

                var assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }

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
