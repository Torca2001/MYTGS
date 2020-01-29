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
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Input;
using System.Net;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        //set to use only MYTGS firefly cloud 
        Firefly.Firefly FF { set; get; } = new Firefly.Firefly("MYTGS");
        Dictionary<int,Firefly.FullTask> Tasks = new Dictionary<int,Firefly.FullTask>();
        DispatcherTimer TenTimer = new DispatcherTimer();
        TimetableClock ClockWindow = new TimetableClock();
        DateTime PlannerDate = DateTime.Now;
        bool PlannerCurrentDay = true;

        public List<TimetablePeriod> ClockSchedule { get => ClockWindow.Schedule; }

        public event PropertyChangedEventHandler PropertyChanged;
        System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
        Settings settings = new Settings();
        bool safeclose = false;
        bool offlineMode = false;

        private string TasksPath = Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]) + "Tasks\\";

        public MainWindow()
        {
            if (!Directory.Exists(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"])))
            {
                Directory.CreateDirectory(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]));
            }
            settings.Initalize();

            //Hook into program terminating to start safe shutdown
            Application.Current.SessionEnding += Current_SessionEnding;
            ClockWindow.PropertyChanged += ClockWindow_PropertyChanged;

            InitializeEventDB("Trinity");
            //Loads data about last time DB was updated
            LoadEventInfo("Trinity");
            LoadSettings();
            
            FF.OnLogin += FF_OnLogin;

            //InitializeTasksDB("Trinity");

            // 10 minutes in milliseconds
            TenTimer.Interval = TimeSpan.FromMinutes(10);
            TenTimer.Tick += TenTimer_Tick;
            InitializeComponent(); //Initialize WPF Window and objects

            eprbrowser.NavigateToString("<p>EPR Not Loaded </p>");

            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                UpdateVerLabel.Content = "Updates V: " + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            this.DataContext = this;
            bool Firsttime = settings.GetSettings("FirstTime") == "";


            //test.Content = JsonConvert.SerializeObject(DateTime.Now.ToUniversalTime());
            ClockWindow.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            ClockWindow.Show();
            ClockWindow.Left = System.Windows.SystemParameters.WorkArea.Width - ClockWindow.Width;
            ClockWindow.Top = System.Windows.SystemParameters.WorkArea.Height - ClockWindow.Height;
            
            if (StartMinimized && !Firsttime)
            {
                ShowInTaskbar = false;
                Hide();
            }
            else
            {
                Show();
            }

            if (Firsttime)
            {
                AddApplicationToStartup();
                settings.SaveSettings("FirstTime", "Not");
            }

            GeneratePlanner(DateTime.Now);

            List<TimetablePeriod> todayPeriods = Timetablehandler.ProcessForUse(DBGetDayEvents("Trinity", DateTime.Now), DateTime.UtcNow, false, IsEventsUptoDate(4), false);
            todayPeriods = EPRCheck(LastEPR, todayPeriods);
            ClockWindow.SetSchedule(todayPeriods);

            menu.MenuItems.Add("Home", new EventHandler(HomeMenu_Click));
            menu.MenuItems.Add("Move", new EventHandler(MoveMenu_Click));
            menu.MenuItems.Add("Quit", new EventHandler(QuitMenu_Click));
            nIcon.ContextMenu = menu;
            nIcon.Icon = Properties.Resources.logo;
            nIcon.DoubleClick += HomeMenu_Click;
            nIcon.Visible = true;

            LoadCachedTasks();
            
            logger.Info("Beginning Login checks");
            if (!FF.LoadKey())
            {
                if (FF.KeyAvailable())
                {
                    offlineMode = true;
                    if (FF.Unauthorised)
                    {
                        DisplayMsg("App Unauthorised - Login Again");
                        MessageGrid.MouseDown += MessageLogin;
                    }
                    else
                    {
                        DisplayMsg("No connection - Restart to go Online");
                        MessageGrid.MouseDown += NoConnectionRestart;
                    }
                }
                else
                {
                    DisplayMsg("Please Login", new SolidColorBrush(Color.FromRgb(0x4E, 0x73, 0xDF)));

                    //Open login ui automatically
                    if (Firsttime)
                    {
                        FF.LoginUI();
                    }
                    MessageGrid.MouseDown += MessageLogin;
                }
            }
            TenTimer.Start();
            
            StartupCheckBox.IsChecked = IsApplicationInStartup();
            StartupCheckBox.Checked += StartupCheckBox_Checked;

        }

        private void MessageLogin(object sender, MouseButtonEventArgs e)
        {
            FF.LoginUI();
        }

        private void NoConnectionRestart(object sender, MouseButtonEventArgs e)
        {
            if ( MessageBox.Show("Restart application to attempt to go on, you sure?", "Restart Application", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                logger.Info("User restarting to go online");
                safeclose = true;
                System.Windows.Forms.Application.Restart();
                Close();
            }
        }

        private void ClockWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null && e.PropertyName == "Schedule")
                PropertyChanged(this, new PropertyChangedEventArgs("ClockSchedule"));
        }

        //Returns whether planner events are up to date givening option of accetable leniance in how dated data can be
        private bool IsEventsUptoDate(int days)
        {
            DateTime temp = FFEventsLastUpdated.AddDays(days);
            return DateTime.UtcNow < temp;
        }

        private void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            safeclose = true;
            ClockWindow?.Close();
        }

        private void LoadCachedTasks()
        {
            logger.Info("Loading Local tasks");
            if (Directory.Exists(TasksPath))
            {
                string[] TaskIDs = Directory.GetDirectories(TasksPath);
                TaskIDs.Reverse();
                int loadedtasks = 0;
                foreach (string TaskID in TaskIDs)
                {
                    try
                    {
                        if (File.Exists(TaskID + "\\Task.json"))
                        {
                            Firefly.FullTask tmp = JsonConvert.DeserializeObject<Firefly.FullTask>(File.ReadAllText(TaskID + "\\Task.json"));
                            Tasks.Add(tmp.id, tmp);
                            loadedtasks += 1;
                        }
                    }
                    catch
                    {
                        logger.Warn("Failed to load task - " + TaskID.Substring(TaskID.LastIndexOf("\\")));
                    }
                }
                logger.Info("Successfully loaded " + loadedtasks + " tasks");
            }
            Tasks = Tasks.OrderBy(p => p.Value.dueDate).Reverse().ToDictionary(k => k.Key, k => k.Value);
        }

        private void TenTimer_Tick(object sender, EventArgs e)
        {
            //Check for changes
            //UpdateTasks(TasksPath);
            
        }

        private List<TimetablePeriod> EPRCheck(EPRcollection epr, List<TimetablePeriod> periods)
        {
            DateTime EPRlocalDate = epr.Date.ToLocalTime();

            if (epr.Errors)
            {
                nIcon.ShowBalloonTip(10000, "EPR Error", "EPR Processing ran into some errors, please check EPR yourself", System.Windows.Forms.ToolTipIcon.Error);
            }

            for (int i = 0; i < periods.Count; i++)
            {
                if (EPRlocalDate.Day == DateTime.Now.Day && EPRlocalDate.Month == DateTime.Now.Month && EPRlocalDate.Year == DateTime.Now.Year)
                {
                    //Room change
                    if (LastEPR.Changes.ContainsKey(periods[i].Classcode + "-" + periods[i].period))
                    {
                        TimetablePeriod item = periods[i];
                        item.Roomcode = LastEPR.Changes[item.Classcode + "-" + periods[i].period].Roomcode;
                        item.Teacher = LastEPR.Changes[item.Classcode + "-" + periods[i].period].Teacher;
                        periods[i] = item;
                        nIcon.ShowBalloonTip(10000, "Class Change", item.Classcode + " Room: " + item.Roomcode + " Teacher: " + item.Teacher, System.Windows.Forms.ToolTipIcon.Info);
                        Console.WriteLine("Class was changed! Room: " + item.Roomcode + " Teacher: " + item.Teacher);
                    }
                }
            }
            return periods;
        }

        //Event fired when successfully connected to Firefly
        private void FF_OnLogin(object sender, EventArgs e)
        {
            logger.Info("Login successful!");
            offlineMode = false;
            HideMsg();
            //Unbind hook if it exists
            MessageGrid.MouseDown -= MessageLogin;

            StatusLabel.Dispatcher.Invoke(new Action(() => {
                StatusLabel.Content = "Welcome " + FF.Name;
            }));
            //TasksBlock.Text = "";
            if (!Directory.Exists(TasksPath))
                Directory.CreateDirectory(TasksPath);
            if (TasksPath[TasksPath.Length - 1] != '\\' || TasksPath[TasksPath.Length - 1] != '/')
                TasksPath += "\\";
            //UpdateTasks(TasksPath);

            //UpdateCalendar("Trinity");

            //TasksPath + "\\" + TaskID + "\\Task.json"
            //Tasks = FF.GetAllTasksByIds(FF.GetAllIds()).Reverse().ToDictionary(pv => pv.id, pv => pv);

            string EPRstr = FF.EPR();
            Dispatcher.Invoke(() => {

                eprbrowser.NavigateToString("<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\"><style>table {width: 100%; border: 1px solid #333; border-collapse: collapse !important;}td {border-right: 1px solid #333; padding: 0.375rem;} tr:not(:last-child) {border-bottom: 1px solid #ccc;}</style></head><body scroll=\"no\">" + EPRstr + "</body></html>");
                EmailLabel.Content = FF.Email;
                IDLabel.Content = FF.Username;
                UserImage.Source = FF.GetUserImage();
                TaskStack.ItemsSource = Tasks.Values;
            });

            List<Firefly.FFEvent> TodayEvents = new List<Firefly.FFEvent>();
            //List<TimetablePeriod> periods = new List<TimetablePeriod>();
            Firefly.FFEvent[] Events = FF.GetEvents(DateTime.Now.AddDays(-15), DateTime.Now.AddDays(15));
            if (Events != null)
            {
                DBUpdateEvents("Trinity", Events, DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(15));
            }
            FFEventsLastUpdated = DateTime.UtcNow;

            List<TimetablePeriod> todayPeriods = Timetablehandler.ProcessForUse(DBGetDayEvents("Trinity", DateTime.Now), DateTime.UtcNow, false, true, false);
            
            //Check EPR for updates
            try
            {

                //EPR Check
                EPRcollection EPR;
                if (EPRstr == null)
                {
                    EPR = LastEPR;
                }
                else
                {
                    EPR = EPRHandler.ProcessEPR(EPRstr);
                    LastEPR = EPR;
                }

                todayPeriods = EPRCheck(EPR, todayPeriods);
            }
            catch
            {
                logger.Warn("EPR Processing failed");
            }

            //aply new schedule
            ClockWindow.SetSchedule(todayPeriods);

            //Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["TasksPath"])
        }

        private int[] UpdateTasks(string filepath)
        {
            int[] AllIDs = FF.GetAllIds();
            Array.Reverse(AllIDs); //Reverse Order of the IDs 
            List<int> NewIDs = new List<int>();
            foreach (int ID in AllIDs)
            {
                if (Tasks.ContainsKey(ID))
                {
                    //Update the cached version
                    Firefly.Response[] tmpresps= FF.GetResponseForID(ID);
                    if (tmpresps == null)
                        continue;
                    Tasks[ID] = FF.UpdateResponses(Tasks[ID], tmpresps);
                    SaveTask(filepath + ID + @"\Task.json" ,Tasks[ID]);
                }
                else
                {
                    NewIDs.Add(ID);
                }
            }

            Firefly.FullTask[] tmp = FF.GetAllTasksByIds(NewIDs.ToArray());
            foreach (Firefly.FullTask item in tmp)
            {
                Tasks.Add(item.id, item);
                SaveTask(filepath + item.id + @"\Task.json", item);
            }

            //return newly added items
            return NewIDs.ToArray();
        }
        
        //Registry edits that don't require admin
        public static void AddApplicationToStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.SetValue("MYTGS App", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\" /SystemStartup");
            }
        }

        public static void RemoveApplicationFromStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.DeleteValue("MYTGS App", false);
            }
        }

        public static bool IsApplicationInStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                object k = key.GetValue("MYTGS App");
                if (k != null && (string)k == "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\" /SystemStartup")
                {
                    return true;
                }
            }
            return false;
        }

        private void QuitMenu_Click(object sender, EventArgs e)
        {
            safeclose = true;
            Application.Current.Shutdown();
        }

        private void HomeMenu_Click(object sender, EventArgs e)
        {
            ShowInTaskbar = true;
            Show();
            Activate();
        }

        private void MoveMenu_Click(object sender, EventArgs e)
        {
            ClockWindow.MoveRequest = true;

            //Ensure to remove double up of move event
            ClockWindow.MouseDown -= MoveClockWindow;
            ClockWindow.MouseDown += MoveClockWindow;
        }

        private void MoveClockWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ClockWindow.DragMove();
            }
            ClockWindow.MoveRequest = false;
            ClockWindow.MouseDown -= MoveClockWindow;
        }

        private void SaveTask(string FilePath, Firefly.FullTask task)
        {
            try
            {
                Directory.CreateDirectory(FilePath.Substring(0,FilePath.LastIndexOf("\\"))); //Creates the required directories 
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(task, Formatting.Indented));
            }
            catch
            {
                logger.Warn("Unable to save task - " + task.id);
            }
        }

        private void TaskStack_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainTabControl.SelectedIndex = 5;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Check if closing by the menu or system shutdown
            if (safeclose == false)
            {
                ShowInTaskbar = false;
                e.Cancel = true;
                Hide();
                return;
            }
            ClockWindow?.Close();
        }


        private void StartupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AddApplicationToStartup();
        }

        private void StartupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            RemoveApplicationFromStartup();
        }

        private void DisplayMsg(string msg, Brush background = null, Brush foreground = null)
        {

            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(() => {
                    MessageGrid.Visibility = Visibility.Visible;
                    MessageLabel.Content = msg;
                    MessageGrid.Background = background ?? Brushes.Red;
                    MessageLabel.Foreground = foreground ?? Brushes.White;
                });
                return;
            }
            else
            {
                MessageGrid.Visibility = Visibility.Visible;
                MessageLabel.Content = msg;
                MessageGrid.Background = background ?? Brushes.Red;
                MessageLabel.Foreground = foreground ?? Brushes.White;
            }
        }

        private void HideMsg(Brush background = null, Brush foreeground = null)
        {
            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(() => {
                    MessageGrid.Visibility = Visibility.Collapsed;
                    MessageLabel.Content = "";
                    MessageGrid.Background = background ?? Brushes.White;
                    MessageLabel.Foreground = background ?? Brushes.Black;
                });
                return;
            }
            else
            {
                MessageGrid.Visibility = Visibility.Collapsed;
                MessageLabel.Content = "";
                MessageGrid.Background = background ?? Brushes.White;
                MessageLabel.Foreground = background ?? Brushes.Black;
            }
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (((TextBox)sender).CaretIndex == 0)
            {
                e.Handled = Regex.IsMatch(e.Text, "[^0-9.-]+");
            }
            else
            {
                e.Handled = !IsTextAllowed(e.Text);
            }
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (Regex.IsMatch(text, "[^0-9.-]+"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextBox_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            int value;
            if (!int.TryParse(((TextBox)sender).Text, out value))
            {
                ((TextBox)sender).Text = Offset.ToString();
                e.Handled = true;
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.IsEnabled = false;
            UpdateApplication();
        }

        private void Eprbrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            eprbrowser.Height = ((dynamic)eprbrowser.Document).Body.parentElement.scrollHeight;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            safeclose = true;
            Application.Current.Shutdown();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            switch (MessageBox.Show("Do you want to delete all saved user data as well?", "Logout - Delete User Data", MessageBoxButton.YesNoCancel))
            {
                case MessageBoxResult.Yes:
                    logger.Info("User logging out - Deleting user data");
                    FF.Logout();
                    DBWipe("Trinity");

                    safeclose = true;
                    System.Windows.Forms.Application.Restart();
                    Close();
                    break;

                case MessageBoxResult.No:
                    logger.Info("User logging out - Keeping user data");
                    FF.Logout();

                    safeclose = true;
                    System.Windows.Forms.Application.Restart();
                    Close();
                    break;
            }
        }

        private void PlannerGrid_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    PlannerDate = PlannerDate.AddDays(e.KeyboardDevice.Modifiers == ModifierKeys.Shift ? -7 : -1);
                    GeneratePlanner(PlannerDate);
                    break;
                case Key.Right:
                    PlannerDate = PlannerDate.AddDays(e.KeyboardDevice.Modifiers == ModifierKeys.Shift ? 7 : 1);
                    GeneratePlanner(PlannerDate);
                    break;
            }


        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            PlannerDate = PlannerDate.AddDays(Keyboard.Modifiers == ModifierKeys.Shift ? -7 : -1);
            GeneratePlanner(PlannerDate);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            PlannerDate = PlannerDate.AddDays(Keyboard.Modifiers == ModifierKeys.Shift ? 7 : 1);
            GeneratePlanner(PlannerDate);
        }
    }
}
