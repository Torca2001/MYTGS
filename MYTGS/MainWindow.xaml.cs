using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using Newtonsoft.Json;
using NLog;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        //set to use only MYTGS firefly cloud 
        Firefly.Firefly FF = new Firefly.Firefly("MYTGS");
        Dictionary<int,Firefly.FullTask> Tasks = new Dictionary<int,Firefly.FullTask>();
        DispatcherTimer TenTimer = new DispatcherTimer();
        public MainWindow()
        {
            // 10 minutes in milliseconds
            TenTimer.Interval = TimeSpan.FromMinutes(10);
            TenTimer.Tick += TenTimer_Tick;
            InitializeComponent(); //Initialize WPF Window and objects
            //test.Content = JsonConvert.SerializeObject(DateTime.Now.ToUniversalTime());
            logger.Info("Beginning Login checks");
            FF.OnLogin += FF_OnLogin;
            if (!FF.LoadKey())
            {
                FF.LoginUI();
            }
            //eprbrowser.NavigateToString("<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\"><style>table {width: 100%; border: 1px solid #333; border-collapse: collapse !important;}td {border-right: 1px solid #333; padding: 0.375rem;} tr:not(:last-child) {border-bottom: 1px solid #ccc;}</style></head><body>" + FF.EPR()+"</body></html>" );
            
            TenTimer.Start();
        }

        private void TenTimer_Tick(object sender, EventArgs e)
        {
            
        }

        //Event fired when successfully connected to Firefly
        private void FF_OnLogin(object sender, EventArgs e)
        {
            logger.Info("Login successful!");
            StatusLabel.Dispatcher.Invoke(new Action(() => {
                StatusLabel.Content = "Welcome " + FF.Name;
            }));
            //TasksBlock.Text = "";
            string TasksPath = Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["TasksPath"]);
            if (!Directory.Exists(TasksPath))
                Directory.CreateDirectory(TasksPath);
            if (TasksPath[TasksPath.Length - 1] != '\\' || TasksPath[TasksPath.Length - 1] != '/')
                TasksPath += "\\";
            foreach (Firefly.FullTask task in FF.GetAllTasksByIds(FF.GetAllIds()).Reverse())
            {
                Tasks.Add(task.id, task);
                TaskStack.Dispatcher.Invoke(new Action(() => {
                    Grid grid = new Grid()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Height = 100

                    };
                    TextBlock title = new TextBlock()
                    {
                        Text = task.title?.Replace(Environment.NewLine, string.Empty),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        TextWrapping = TextWrapping.Wrap,
                        Height = 50,
                        Margin = new Thickness(4, 0, 0, 50)
                    };
                    grid.Children.Add(title);
                    TaskStack.Items.Add(grid);
                }));
                if (!Directory.Exists(TasksPath + task.id))
                    Directory.CreateDirectory(TasksPath + task.id);
                File.WriteAllText(TasksPath + task.id +@"\Task.json",JsonConvert.SerializeObject(task, Formatting.Indented));
            }
            foreach (Firefly.FFEvent item in FF.GetEvents(DateTime.Now, DateTime.Now.AddDays(10)))
            {
                PlannerStack.Dispatcher.Invoke(new Action(() => {
                    Label lbl = new Label()
                    {
                        Content = "Start " + item.start.ToLocalTime() + " End " + item.end.ToLocalTime() + " Guid: " + item.guid + " Subject: " + item.subject + " Desc: " + item.description + " Loc: " + item.location
                    };
                    PlannerStack.Children.Add(lbl);
                }));
            }
            //Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["TasksPath"])
        }
    }
}
