using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        void App_Startup(object sender, StartupEventArgs e)
        {
            // Application is running
            // Process command line args
            bool UpdateCheck = false;
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/SystemStartup")
                {
                    UpdateCheck = true;
                }
            }

            // Create main application window, starting minimized if specified
            MainWindow mainWindow = new MainWindow();
            if (UpdateCheck)
            {
                mainWindow.UpdateApplication();
            }
        }
    }
}
