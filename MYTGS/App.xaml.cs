using NLog;
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
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static Mutex _mutex = null;
        const string Appname = "MYTGS";
        MainWindow mainWindow;

        void App_Startup(object sender, StartupEventArgs e)
       {
            bool createdNew;

            _mutex = new Mutex(true, Appname, out createdNew);
            bool kill = false;

            if (!createdNew)
            {
                try
                {
                    using (NamedPipeClientStream client = new NamedPipeClientStream(Appname))

                    using (StreamWriter writer = new StreamWriter(client))

                    {
                        try
                        {
                            client.Connect(200);
                            writer.Write("SHOW");
                            writer.Flush();

                            byte[] buffer = new byte[10];
                            bool waitflag = false;
                            var k = client.BeginRead(buffer, 0, 10, new AsyncCallback(ar => { waitflag = true; }), null);
                            Thread.Sleep(3000);
                            client.EndRead(k);
                            //Check if it works
                            if (waitflag && buffer[2] - buffer[5] == buffer[3])
                            {
                                Application.Current.Shutdown();
                                return;
                            }
                            else
                            {
                                Console.WriteLine("Other instance failed to respond!");
                                var result = MessageBox.Show("Currently running program failed to respond!, Do you wish to create a new instance?", "Instance Failure", MessageBoxButton.YesNo);
                                if (result != MessageBoxResult.Yes)
                                {
                                    //terminate program
                                    kill = true;
                                    Application.Current.Shutdown();
                                    return;
                                }
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Other instance failed to respond!");
                            var result = MessageBox.Show("Currently running program failed to respond!, Do you wish to create a new instance?", "Instance Failure", MessageBoxButton.YesNo);
                            if (result != MessageBoxResult.Yes)
                            {
                                //terminate program
                                kill = true;
                                Application.Current.Shutdown();
                                return;
                            }
                        }
                    }
                }
                catch(InvalidOperationException ed)
                {
                    //failed due to expected edge case
                    //safely end
                    if (kill == true)
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                }
                catch
                {
                    //do nothing open up
                }
                
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
            SetupExceptionHandling();

            // Create main application window, starting minimized if specified
            mainWindow = new MainWindow();
            mainWindow.mutex = _mutex;
            if (UpdateCheck)
            {
                mainWindow.UpdateApplication();
            }
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                _logger.Error(exception, message);
                _logger.Error(exception);
            }
        }

    }
}
