using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private static Mutex _mutex = null;
        const string Appname = "MYTGS";
        MainWindow mainWindow;

        void App_Startup(object sender, StartupEventArgs e)
        {
            bool createdNew;

            _mutex = new Mutex(true, Appname, out createdNew);
            
            if (!createdNew)
            {
                
                using (NamedPipeClientStream client = new NamedPipeClientStream(Appname))

                using (StreamWriter writer = new StreamWriter(client))

                {
                    client.Connect(200);
                    writer.Write("SHOW");
                    writer.Flush();

                }

                Application.Current.Shutdown();
                return;
            }


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
            mainWindow = new MainWindow();
            if (UpdateCheck)
            {
                mainWindow.UpdateApplication();
            }

        }


    }
}
