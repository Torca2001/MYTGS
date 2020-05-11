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
using HtmlAgilityPack;
using System.Windows.Media.Imaging;
using System.IO.Pipes;
using System.Collections.ObjectModel;
using System.Threading;
using SQLite;
using NAudio.Wave;

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
        public ObservableCollection<Firefly.FullTask> TaskSearch { get; set; } = new ObservableCollection<Firefly.FullTask>();
        DispatcherTimer TenTimer = new DispatcherTimer();
        DispatcherTimer UpdateTimer = new DispatcherTimer();
        public ObservableCollection<TimetablePeriod> EPRChanges { get; set; } = new ObservableCollection<TimetablePeriod>();
        public List<DirectSoundDeviceInfo> AudioDevicesList { get; set; } = new List<DirectSoundDeviceInfo>();
        TimetableClock ClockWindow = new TimetableClock();
        DateTime LastDayCheck = DateTime.Now;
        DateTime PlannerDate = DateTime.Now;
        Thread SearchThread = null;
        int EPRWait = 0;
        bool PlannerCurrentDay = true;
        private bool IsFirstTime = false;
        DirectSoundOut outputAudioDevice = new DirectSoundOut();
        public bool TodayEarlyFinish { get; set; }


        public List<TimetablePeriod> ClockSchedule { get => ClockWindow.Schedule; }

        public event PropertyChangedEventHandler PropertyChanged;
        System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
        Settings settings = new Settings();
        bool safeclose = false;
        bool offlineMode = false;
        const string SchoolDBFile = "Trinity";
        public int tmpScreen { get; set; } = 1;
        

        //Get path to database
        SQLiteConnection dbSchool = null;

        private string TasksPath = Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]) + "Tasks\\";

        public MainWindow()
        { 
            if (!Directory.Exists(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"])))
            {
                Directory.CreateDirectory(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]));
            }

            try
            {
                dbSchool = new SQLiteConnection(Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), SchoolDBFile + ".db"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Catastrophic Error - DB Failed to initalize correctly!");
                MessageBox.Show("Database system has failed to start! Ensure application has access to folder %appdata%/MYTGS", "Databse Error");
                dbSchool = new SQLiteConnection(":memory:"); //Use in memory Database
            }

            settings.Initalize();

            //Initalize Pipe server for single instance only checking
            NamedPipeServerStream pipeServer = new NamedPipeServerStream("MYTGS",
               PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

            // Wait for a connection
            pipeServer.BeginWaitForConnection
            (new AsyncCallback(HandleConnection), pipeServer);

            //Hook into program terminating to start safe shutdown
            Application.Current.SessionEnding += Current_SessionEnding;
            ClockWindow.PropertyChanged += ClockWindow_PropertyChanged;

            //Initalize SQL Database's tables
            InitializeEventDB(dbSchool);
            InitializeCalendarDB(dbSchool);
            InitializeTasksDB(dbSchool);
            InitializeCacheDB(dbSchool);


            //Update audio drivers list
            UpdateAudioDeviceList();


            //Loads data about last time DB was updated
            LoadCache(dbSchool);
            LoadEventInfo(dbSchool);
            LoadSettings();

            try
            {
                outputAudioDevice?.Dispose();
                outputAudioDevice = new DirectSoundOut(new Guid(SelectedAudioDevice));
            }
            catch
            {
                //Link to default audio device
                outputAudioDevice = new DirectSoundOut();
            }

            //Check to see if connected to domain or not
            if (ConnectedToDomain())
            {
                DomainLastActive = DateTime.UtcNow;
            }

            FF.OnLogin += FF_OnLogin;
            ClockWindow.BellTrigger += ClockWindow_BellTrigger;
            // 10 minutes in milliseconds
            TenTimer.Interval = TimeSpan.FromMinutes(10);
            TenTimer.Tick += TenTimer_Tick;

            UpdateTimer.Interval = TimeSpan.FromHours(6);
            UpdateTimer.Tick += UpdateTimer_Tick;
            InitializeComponent(); //Initialize WPF Window and objects

            PlacementModeCombo.SelectedIndex = ClockPlacementMode;
            HorizontalOffsetTextbox.Text = XOffset;
            VerticalOffsetTextbox.Text = YOffset;
            if (TablePreference)
            {
                TablePreferenceComboBox.SelectedIndex = 1;
            }

            for (int i = 0; i < AudioDevicesList.Count; i++)
            {
                if (AudioDevicesList[i].Guid.ToString() == SelectedAudioDevice)
                {
                    BellAudioDeviceComboBox.SelectedIndex = i;
                    break;
                }
            }

            eprbrowser.NavigateToString("<p>EPR Not Loaded </p>");

            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                UpdateVerLabel.Content = "Updates V: " + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            this.DataContext = this;
            bool Firsttime = settings.GetSettings("FirstTime") == "";
            IsFirstTime = Firsttime;

            earlyfinishcheck.IsChecked = IsTodayEarlyFinish(dbSchool);
            
            ClockWindow.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            ClockWindow.Show();
            SetClockPosition(XOffset, YOffset, ScreenPreference, ClockPlacementMode);


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

            CheckForEarlyFinishes(dbSchool);
            List<TimetablePeriod> todayPeriods = Timetablehandler.ProcessForUse(DBGetDayEvents(dbSchool, DateTime.Now), DateTime.UtcNow, IsTodayEarlyFinish(dbSchool), IsEventsUptoDate(4), false);
            todayPeriods = EPRCheck(LastEPR, todayPeriods);
            ClockWindow.SetSchedule(todayPeriods);
            UpdateFirstDay(LastEPR.Date, LastEPR.Day);
            DashboardMessageToXaml(FF.DashboardLocateMessage(Dashboardstring));

            menu.MenuItems.Add("Home", new EventHandler(HomeMenu_Click));
            menu.MenuItems.Add("Move", new EventHandler(MoveMenu_Click));
            menu.MenuItems.Add("Quit", new EventHandler(QuitMenu_Click));
            nIcon.ContextMenu = menu;
            nIcon.Icon = Properties.Resources.CustomIcon;
            nIcon.DoubleClick += HomeMenu_Click;
            nIcon.MouseDown += NIcon_MouseDown;
            nIcon.Visible = true;


            FF.OnSiteConnect += SiteConnected;
            FF.SchoolCheckAsync("MYTGS");

            TenTimer.Start();
            UpdateTimer.Start();
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed && !IsApplicationInStartup())
                {
                    object k = key.GetValue("MYTGS App");
                    if (k != null && ((string)k).Length > 5)
                    {
                        AddApplicationToStartup();
                    }
                }
            }
            StartupCheckBox.IsChecked = IsApplicationInStartup();
            StartupCheckBox.Checked += StartupCheckBox_Checked;
            

            UpdateCalendar(dbSchool);
            CheckForEarlyFinishes(dbSchool);
            TwoWeekTimetable = LocateTwoWeeks(FirstDayDate);
            GenerateTwoWeekTimetable();
            UpdateSearchResults();
        }

        //test bell button
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            float vol = VolumeCtrl / 100.0f;

            byte[] bytes = Convert.FromBase64String(Bellwave);
            using (var wave = new WaveChannel32(new Mp3FileReader(new MemoryStream(bytes)), vol, 0f))
            {
                outputAudioDevice.Init(wave);
                outputAudioDevice.Play();

                Thread.Sleep((int)wave.TotalTime.TotalMilliseconds);
            }
        }

        private void SetClockPosition(string xstring, string ystring, int screenpref, int mode)
        {
            try
            {
            var allscreens = System.Windows.Forms.Screen.AllScreens;
            if (allscreens.Length == 0)
            {
                ClockWindow.Top = 0;
                ClockWindow.Left = 0;
                return;
            }

            System.Drawing.Rectangle screenbounds = new System.Drawing.Rectangle();
            if (screenpref == 0 || screenpref > allscreens.Length)
            {
                screenbounds = new System.Drawing.Rectangle(0, 0, (int)SystemParameters.WorkArea.Width, (int)SystemParameters.WorkArea.Height);
            }
            else
            {
                screenbounds = allscreens[screenpref - 1].WorkingArea;
            }

            xstring = xstring.Trim();
            ystring = ystring.Trim();
            double xdisplacement = 0;
            double ydisplacement = 0;
            if (xstring.EndsWith("%"))
            {
                double.TryParse(xstring.Substring(0, xstring.Length - 1), out xdisplacement);
                xdisplacement = screenbounds.Width * xdisplacement / 100;
            }
            else
            {
                double.TryParse(xstring, out xdisplacement);
            }

            if (ystring.EndsWith("%"))
            {
                double.TryParse(ystring.Substring(0, ystring.Length - 1), out ydisplacement);
                ydisplacement = screenbounds.Height * ydisplacement / 100;
            }
            else
            {
                double.TryParse(ystring, out ydisplacement);
            }
            

            Point clockpos = new Point();
            switch (mode)
            {
                case 1:
                    clockpos.X = screenbounds.Right - xdisplacement - ClockWindow.Width;
                    clockpos.Y = screenbounds.Top + ydisplacement;

                    break;
                case 2:
                    clockpos.X = screenbounds.Left + xdisplacement;
                    clockpos.Y = screenbounds.Bottom - ydisplacement-ClockWindow.Height;

                    break;
                case 3:
                    clockpos.X = screenbounds.Left + xdisplacement;
                    clockpos.Y = screenbounds.Top + ydisplacement;

                    break;
                case 4:
                    clockpos.X = screenbounds.Left + ((screenbounds.Width-ClockWindow.Width)/2) + xdisplacement;
                    clockpos.Y = screenbounds.Top + ((screenbounds.Height-ClockWindow.Height)/2) + ydisplacement;

                    break;
                case 5:
                    clockpos.X = xdisplacement;
                    clockpos.Y = ydisplacement;

                    break;
                default:
                    clockpos.X = screenbounds.Right - xdisplacement - ClockWindow.Width;
                    clockpos.Y = screenbounds.Bottom - ydisplacement - ClockWindow.Height;
                    break;
            }

            clockpos = ClampWindowPos(clockpos, ClockWindow.Width, ClockWindow.Height);
            ClockWindow.Left = clockpos.X;
            ClockWindow.Top = clockpos.Y;
            }
            catch(Exception e)
            {
                logger.Error(e, "unable to set clockwindow position");
            }
        }

        //Clamp window location to closest Screen preventing it from going off
        private Point ClampWindowPos(Point windowlocation, double width, double height)
        {
            //Find closest screen
            var res = System.Windows.Forms.Screen.AllScreens;
            //No screens? then how can this even be determined
            if (res.Length == 0)
            {
                return windowlocation;
            }
            
            int close = 0;
            double closeness = double.PositiveInfinity;
            for (int i = 0; i < res.Length; i++)
            {
                double xpos = res[i].Bounds.Left - windowlocation.X + ((res[i].Bounds.Width - width) / 2);
                double ypos = res[i].Bounds.Top - windowlocation.Y + ((res[i].Bounds.Height - height) / 2);
                double dis = xpos * xpos + ypos * ypos;
                if (dis < closeness)
                {
                    closeness = dis;
                    close = i;
                }
            }

            //Clamp to screen position
            if (windowlocation.Y < res[close].Bounds.Top)
            {
                windowlocation.Y = res[close].Bounds.Top;
            }
            else if (windowlocation.Y+height > res[close].Bounds.Bottom)
            {
                windowlocation.Y = res[close].Bounds.Bottom - height;
            }

            if (windowlocation.X < res[close].Bounds.Left)
            {
                windowlocation.X = res[close].Bounds.Left;
            }
            else if (windowlocation.X + width > res[close].Bounds.Right)
            {
                windowlocation.X = res[close].Bounds.Right - width;
            }

            return windowlocation;

        }

        private bool ConnectedToDomain()
        {
            try
            {
                System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain();
                return true;
            }
            catch(Exception e)
            {
                //Do nothing
            }

            return false;
        }

        private void ClockWindow_BellTrigger(object sender, EventArgs e)
        {
            //Check to see if connected to domain or not
            if (ConnectedToDomain())
            {
                DomainLastActive = DateTime.UtcNow;
            }


            //Only run bell if domain wasn't connected to in the last 10 minutes
            if (EnableBell && (DateTime.UtcNow-DomainLastActive).TotalMinutes >= 10)
            {
                float vol = VolumeCtrl / 100.0f;

                byte[] bytes = Convert.FromBase64String(Bellwave);
                using (var wave = new WaveChannel32(new Mp3FileReader(new MemoryStream(bytes)), vol, 0f))
                {
                    outputAudioDevice.Init(wave);
                    outputAudioDevice.Play();

                    Thread.Sleep((int)wave.TotalTime.TotalMilliseconds);
                }
            }
        }

        private void SiteConnected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
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
                        if (IsFirstTime)
                        {
                            FF.LoginUI();
                        }
                        MessageGrid.MouseDown += MessageLogin;
                    }
                }
            });
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateApplication();
            });
        }

        private void NIcon_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ClockWindow.ShowTable = !ClockWindow.ShowTable;
            }
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

        private void HandleConnection(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;
                // End waiting for the connection
                pipeServer.EndWaitForConnection(iar);

                byte[] buffer = new byte[255];

                // Read the incoming message
                pipeServer.Read(buffer, 0, 255);

                // Convert byte buffer to string
                string stringData = Encoding.UTF8.GetString(buffer, 0, 255);
                if (stringData.StartsWith("SHOW"))
                {
                    Dispatcher.Invoke(() => {
                        ShowInTaskbar = true;
                        Show();
                        Activate();
                    });
                }

                // Kill original sever and create new wait server
                pipeServer.Close();
                pipeServer = null;
                pipeServer = new NamedPipeServerStream("MYTGS", PipeDirection.InOut,
                   1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                // Recursively wait for the connection again and again....
                pipeServer.BeginWaitForConnection(
                   new AsyncCallback(HandleConnection), pipeServer);
            }
            catch
            {
                return;
            }
        }

        DateTime LastTenTimerCheck = DateTime.Now;
        private void TenTimer_Tick(object sender, EventArgs e)
        {
            //Check for changes
            //UpdateTasks(TasksPath);

            //Check to see if connected to domain or not
            if (ConnectedToDomain())
            {
                DomainLastActive = DateTime.UtcNow;
            }

            if (DateTime.Now.ToShortDateString() != LastTenTimerCheck.ToShortDateString())
            {
                LastTenTimerCheck = DateTime.Now;

                //Property change event so ui will react
                if (PropertyChanged != null)
                {
                    GenerateTwoWeekTimetable();
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentTimetableDay"));
                }
            }

            if (offlineMode == false)
            {

                UpdateCalendar(dbSchool);
                CheckForEarlyFinishes(dbSchool);
                
                Firefly.FullTask[] Tasks = FF.GetAllTasksByIds(FF.GetIds(TaskLastFetch));
                if (Tasks != null)
                {
                    TaskLastFetch = DateTime.UtcNow;
                    DBInsertOrReplace(dbSchool, Tasks);
                }

                if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday && LastEPR.Date.Day < DateTime.Now.Day && LastEPR.Date.Month < DateTime.Now.Month && LastEPR.Date.Year < DateTime.Now.Year)
                {
                    try
                    {
                        string EPRstr = FF.EPR();
                        if (EPRstr != null)
                        {
                            EPRstring = EPRstr;
                            LastEPR = EPRHandler.ProcessEPR(EPRstr);
                            UpdateFirstDay(LastEPR.Date, LastEPR.Day);
                            ClockWindow.SetSchedule(EPRCheck(LastEPR, ClockWindow.Schedule, true));
                        }
                    }
                    catch
                    {
                        logger.Warn("EPR Processing failed");
                    }

                    Dispatcher.Invoke(() => {

                        eprbrowser.NavigateToString("<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\"><style>table {width: 100%; border: 1px solid #333; border-collapse: collapse !important;}td {border-right: 1px solid #333; padding: 0.375rem;} tr:not(:last-child) {border-bottom: 1px solid #ccc;}</style></head><body>" + EPRstring + "</body></html>");
                    });


                }
                else if (EPRWait > 3)
                {
                    EPRWait = 0;
                    try
                    {
                        string EPRstr = FF.EPR();
                        if (EPRstr != null)
                        {
                            EPRstring = EPRstr;
                            LastEPR = EPRHandler.ProcessEPR(EPRstr);
                            UpdateFirstDay(LastEPR.Date, LastEPR.Day);
                            ClockWindow.SetSchedule(EPRCheck(LastEPR, ClockWindow.Schedule, false));
                        }
                    }
                    catch
                    {
                        logger.Warn("EPR Processing failed");
                    }

                    string tempdash = FF.DashboardString();
                    if (tempdash != null)
                    {
                        Dashboardstring = tempdash;
                    }
                    Dispatcher.Invoke(() =>
                    {
                        DashboardMessageToXaml(FF.DashboardLocateMessage(Dashboardstring));
                    });

                    Dispatcher.Invoke(() => {

                        eprbrowser.NavigateToString("<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\"><style>table {width: 100%; border: 1px solid #333; border-collapse: collapse !important;}td {border-right: 1px solid #333; padding: 0.375rem;} tr:not(:last-child) {border-bottom: 1px solid #ccc;}</style></head><body>" + EPRstring + "</body></html>");
                    });
                }
                else
                {
                    EPRWait++;
                }

                if (LastDayCheck.ToShortDateString() != DateTime.Now.ToShortDateString())
                {
                    LastDayCheck = DateTime.Now;
                    Firefly.FFEvent[] Events = FF.GetEvents(DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(15));
                    if (Events != null)
                    {
                        DBUpdateEvents(dbSchool, Events, DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(15));
                        FFEventsLastUpdated = DateTime.UtcNow;
                    }
                    
                    if (PlannerCurrentDay)
                    {
                        PlannerDate = DateTime.Now;
                    }
                    GeneratePlanner(PlannerDate);


                    List<TimetablePeriod> todayPeriods = Timetablehandler.ProcessForUse(DBGetDayEvents(dbSchool, DateTime.Now), DateTime.UtcNow, IsTodayEarlyFinish(dbSchool), true, false);
                    todayPeriods = EPRCheck(LastEPR, todayPeriods, true);
                    ClockWindow.SetSchedule(todayPeriods);

                }
            }
            else
            {
                List<TimetablePeriod> todayPeriods = Timetablehandler.ProcessForUse(DBGetDayEvents(dbSchool, DateTime.Now), DateTime.UtcNow, IsTodayEarlyFinish(dbSchool), true, false);
                todayPeriods = EPRCheck(LastEPR, todayPeriods, true);
                ClockWindow.SetSchedule(todayPeriods);
            }
        }

        private List<TimetablePeriod> EPRCheck(EPRcollection epr, List<TimetablePeriod> periods, bool Notify = true)
        {
            DateTime EPRlocalDate = epr.Date.ToLocalTime();

            if (Notify && epr.Errors)
            {
                nIcon.ShowBalloonTip(10000, "EPR Error", "EPR Processing ran into some errors, please check EPR yourself", System.Windows.Forms.ToolTipIcon.Error);
            }

            Dispatcher.Invoke(() =>
            {
                EPRChanges.Clear();
            });
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
                        Dispatcher.Invoke(() =>
                        {
                            EPRChanges.Add(item);
                        });
                        if (Notify)
                        {
                            nIcon.ShowBalloonTip(10000, "Class Change", item.Classcode + " Room: " + item.Roomcode + " Teacher: " + item.Teacher, System.Windows.Forms.ToolTipIcon.Info);
                        }
                    }
                }
            }
            return periods;
        }

        //Event fired when successfully connected to Firefly
        //
        //
        //       ON LOGIN
        //
        //---------------------------------------------------

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

            UpdateCalendar(dbSchool);

            //TasksPath + "\\" + TaskID + "\\Task.json"
            //Get tasks/ get all tasks if firsttime run
            Firefly.FullTask[] Tasks = FF.GetAllTasksByIds(IsFirstTime ? FF.GetAllIds() : FF.GetIds(TaskLastFetch));
            if (Tasks != null)
            {
                TaskLastFetch = DateTime.UtcNow;
                DBInsertOrReplace(dbSchool, Tasks);
            }

            string EPRstr = FF.EPR();
            if (EPRstr != null)
            {
                EPRstring = EPRstr;
            }

            Dispatcher.Invoke(() => {

                eprbrowser.NavigateToString("<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\"><style>table {width: 100%; border: 1px solid #333; border-collapse: collapse !important;}td {border-right: 1px solid #333; padding: 0.375rem;} tr:not(:last-child) {border-bottom: 1px solid #ccc;}</style></head><body>" + EPRstring + "</body></html>");
                EmailLabel.Content = FF.Email;
                IDLabel.Content = FF.Username;
                UserImage.Source = FF.GetUserImage();
                UpdateSearchResults();
            });

            List<Firefly.FFEvent> TodayEvents = new List<Firefly.FFEvent>();
            //List<TimetablePeriod> periods = new List<TimetablePeriod>();

            Firefly.FFEvent[] Events = FF.GetEvents(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(30));
            if (Events != null)
            {
                DBUpdateEvents(dbSchool, Events, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(30));
                FFEventsLastUpdated = DateTime.UtcNow;
            }


            List<TimetablePeriod> todayPeriods = Timetablehandler.ProcessForUse(DBGetDayEvents(dbSchool, DateTime.Now), DateTime.UtcNow, IsTodayEarlyFinish(dbSchool), true, false);

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
                    UpdateFirstDay(LastEPR.Date, LastEPR.Day);
                }

                todayPeriods = EPRCheck(EPR, todayPeriods);
            }
            catch
            {
                logger.Warn("EPR Processing failed");
            }

            //aply new schedule
            ClockWindow.SetSchedule(todayPeriods);

            string tempdash = FF.DashboardString();
            if (tempdash != null)
            {
                Dashboardstring = tempdash;
                
            }

            Dispatcher.Invoke(() =>
            {
                DashboardMessageToXaml(FF.DashboardLocateMessage(Dashboardstring));
                TwoWeekTimetable = LocateTwoWeeks(FirstDayDate);
                GenerateTwoWeekTimetable();
            });
            //Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["TasksPath"])
        }
        
        //Registry edits that don't require admin
        public void AddApplicationToStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                string location = "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\" /SystemStartup";
                if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                {
                    location = Environment.GetFolderPath(Environment.SpecialFolder.Programs)
                   + @"\Torca\MYTGS\MYTGS.appref-ms /SystemStartup";
                }
                key.SetValue("MYTGS App", location);
            }
        }

        public void RemoveApplicationFromStartup()
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
                string location = "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"";
                if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                {
                    location = Environment.GetFolderPath(Environment.SpecialFolder.Programs)
                   + @"\Torca\MYTGS\MYTGS.appref-ms";
                }
                object k = key.GetValue("MYTGS App");
                if (k != null && ((string)k).StartsWith(location))
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
            Dispatcher.Invoke(() =>
            {
                ShowInTaskbar = true;
                Show();
                Activate();
            });
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

        public void TaskStack_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GotoTaskpage();
        }

        //Application Closing
        //
        //
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

            outputAudioDevice?.Dispose();
            dbSchool.Close();
            settings.Close();
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
                    DBWipe(dbSchool);

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
                    PlannerCurrentDay = PlannerDate.ToShortDateString() == DateTime.Now.ToShortDateString();
                    GeneratePlanner(PlannerDate);
                    break;
                case Key.Right:
                    PlannerDate = PlannerDate.AddDays(e.KeyboardDevice.Modifiers == ModifierKeys.Shift ? 7 : 1);
                    PlannerCurrentDay = PlannerDate.ToShortDateString() == DateTime.Now.ToShortDateString();
                    GeneratePlanner(PlannerDate);
                    break;
            }


        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            PlannerDate = PlannerDate.AddDays(Keyboard.Modifiers == ModifierKeys.Shift ? -7 : -1);
            PlannerCurrentDay = PlannerDate.ToShortDateString() == DateTime.Now.ToShortDateString();
            GeneratePlanner(PlannerDate);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            PlannerDate = PlannerDate.AddDays(Keyboard.Modifiers == ModifierKeys.Shift ? 7 : 1);
            PlannerCurrentDay = PlannerDate.ToShortDateString() == DateTime.Now.ToShortDateString();
            GeneratePlanner(PlannerDate);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 6;
            eprbrowser.Focus();
            Task.Run(() =>
            {
                string EPRstr = FF.EPR();
                if (EPRstr == null)
                {
                    return;
                }

                EPRstring = EPRstr;
                Dispatcher.Invoke(() => {

                    eprbrowser.NavigateToString("<html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\"><style>table {width: 100%; border: 1px solid #333; border-collapse: collapse !important;}td {border-right: 1px solid #333; padding: 0.375rem;} tr:not(:last-child) {border-bottom: 1px solid #ccc;}</style></head><body>" + EPRstring + "</body></html>");
                });
            });
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            //Sanitize request, only want web requests to be processed
            if (e.Uri.AbsoluteUri.StartsWith("http://") || e.Uri.AbsoluteUri.StartsWith("https://"))
            {
                System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
                e.Handled = true;
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 7;
        }

        private void L_Click(object sender, RoutedEventArgs e)
        {
            UserEarlyFinishEvent(dbSchool, earlyfinishcheck.IsChecked == true);
            CheckForEarlyFinishes(dbSchool);
            List<TimetablePeriod> todayPeriods = Timetablehandler.ProcessForUse(DBGetDayEvents(dbSchool, DateTime.Now), DateTime.UtcNow, IsTodayEarlyFinish(dbSchool), IsEventsUptoDate(4), false);
            ClockWindow.SetSchedule(todayPeriods);
        }

        private void SearchTextBox_Update(object sender, EventArgs e)
        {
            if (this.IsLoaded)
                UpdateSearchResults();
        }

        private void UpdateSearchResults()
        {
            if (SearchThread != null && SearchThread.IsAlive)
            {
                SearchThread.Abort();
            }

            TaskSearchSpinner.Visibility = Visibility.Visible;
            string searchtext = SearchTextBox.Text;
            string teachertext = TaskTeacherSearchBox.Text;
            string idtext = TaskIDSearchBox.Text;
            string classtext = TaskClassSearchBox.Text;
            int selindex = TaskSearchCombo.SelectedIndex;
            bool deletecheck = (bool)TaskSearchDeletedCheck.IsChecked;
            bool hiddencheck = (bool)TaskSearchHiddenCheck.IsChecked;
            bool hideMarked = (bool)TaskSearchHideMarked.IsChecked;
            SearchThread = new Thread(() =>
            {
                try
                {
                    var results = DBTaskSearch(dbSchool, searchtext, teachertext, idtext, classtext, selindex, deletecheck, hiddencheck, hideMarked);
                    Dispatcher.Invoke(() =>
                    {
                        TaskSearchSpinner.Visibility = Visibility.Hidden;
                        TaskSearch = new ObservableCollection<Firefly.FullTask>(results);
                        PropertyChanged(this, new PropertyChangedEventArgs("TaskSearch"));
                    });
                }
                catch(Exception e)
                {
                    logger.Warn(e, "Task Search error");
                }
            });
            SearchThread.Start();
        }

        private void TaskSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevent running on initalization
            if (!this.IsLoaded)
                return;

                //Redo the order
                switch (TaskSearchCombo.SelectedIndex)
            {
                case 1:
                    //Oldest activity
                    TaskSearch = new ObservableCollection<Firefly.FullTask>(TaskSearch.OrderBy(pv => pv.LatestestActivity));

                    break;
                case 2:
                    //Latest due
                    TaskSearch = new ObservableCollection<Firefly.FullTask>(TaskSearch.OrderByDescending(pv => pv.dueDate));
                    break;
                case 3:
                    //Oldest due
                    TaskSearch = new ObservableCollection<Firefly.FullTask>(TaskSearch.OrderBy(pv => pv.dueDate));

                    break;
                case 4:
                    //latest set
                    TaskSearch = new ObservableCollection<Firefly.FullTask>(TaskSearch.OrderByDescending(pv => pv.setDate));

                    break;
                case 5:
                    //oldest set
                    TaskSearch = new ObservableCollection<Firefly.FullTask>(TaskSearch.OrderBy(pv => pv.setDate));

                    break;
                default:
                    //latest activity
                    TaskSearch = new ObservableCollection<Firefly.FullTask>(TaskSearch.OrderByDescending(pv => pv.LatestestActivity));

                    break;
            }
            PropertyChanged(this, new PropertyChangedEventArgs("TaskSearch"));
        }

        private void SearchUpdate_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdateSearchResults();
            }
        }

        private void TaskStack_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                GotoTaskpage();
            }
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            UpdateAudioDeviceList();
        }

        private void UpdateAudioDeviceList()
        {
            AudioDevicesList.Clear();
            foreach(var device in DirectSoundOut.Devices)
            {
                if (device.Guid.ToString() == "00000000-0000-0000-0000-000000000000")
                {
                    var temp = device;
                    temp.Description = "Default Audio Device";
                    AudioDevicesList.Add(temp);
                }
                else
                {
                    AudioDevicesList.Add(device);
                }
            }

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("AudioDevicesList"));



        }

        private void BellAudioDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded == false)
                return;
            
            SelectedAudioDevice = ((DirectSoundDeviceInfo)((ComboBox)sender).SelectedItem).Guid.ToString();
            try
            {
                outputAudioDevice?.Dispose();
                outputAudioDevice = new DirectSoundOut(new Guid(SelectedAudioDevice));
            }
            catch
            {
                //Link to default audio device
                outputAudioDevice = new DirectSoundOut();
            }
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            if (tmpScreen >= 1)
                tmpScreen--;

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("tmpScreen"));
            }
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            tmpScreen++;

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("tmpScreen"));
            }
        }

        private void OffsetTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                if (!IsLoaded && ((TextBox)sender).Text.Length == 0)
                {
                    ((TextBox)sender).Text = "0";
                    ((TextBox)sender).BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xAB, 0xAD, 0xB3));
                }

                if ((Regex.IsMatch(((TextBox)sender).Text, @"^(-)?[0-9]*(\.)?[0-9]*%$") && double.TryParse(((TextBox)sender).Text.Substring(0, ((TextBox)sender).Text.Length - 1), out double p)))
                {
                    ((TextBox)sender).BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xAB, 0xAD, 0xB3));
                }
                else if (Regex.IsMatch(((TextBox)sender).Text, @"^(-)?[0-9]{1,20}$"))
                {
                    ((TextBox)sender).BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xAB, 0xAD, 0xB3));
                }
                else
                {
                    ((TextBox)sender).BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
                }
            }

        }

        private void IntTextbox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (((TextBox)sender).Text.Length == 0)
            {
                ((TextBox)sender).Text = "0";
            }
        }

        //Apply changes to clock placement
        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            XOffset = HorizontalOffsetTextbox.Text;
            YOffset = VerticalOffsetTextbox.Text;
            ClockPlacementMode = PlacementModeCombo.SelectedIndex;
            ScreenPreference = tmpScreen;
            TablePreference = TablePreferenceComboBox.SelectedIndex == 1;

            SetClockPosition(XOffset, YOffset, ScreenPreference, ClockPlacementMode);
        }
    }
}
