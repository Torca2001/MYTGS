using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for TimetableClock.xaml
    /// </summary>
    public partial class TimetableClock : Window , INotifyPropertyChanged
    {
        private List<TimetablePeriod> schedule { get; set; }

        public DateTime FirstDayDate
        {
            get => firstDayDate;
            set
            {
                firstDayDate = value;
                currentTimetableDay = CalculateTimetableDay(DateTime.Now);
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("FirstDayDate"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentTimetableDay"));
                }
            }
        }
        private DateTime firstDayDate { get; set; } = new DateTime(2020, 2, 10);
        public CalendarEvent[] CurrentEarlyFinishes { get; set; }

        public string CurrentTimetableDay { get => "Day " + currentTimetableDay; }
        private int currentTimetableDay { get; set; }
        private DateTime LastDay = DateTime.Now;
        public event EventHandler BellTrigger;
        private bool BellFlag = false;
        private int Lastperiod = 20; //Higher than possible period to prevent startup triggering bell
        private bool LastPeriodGoto = false;

        public bool FadeOnHover = false;
        public bool HideOnFullscreen = false;
        public bool CombineDoubles = false;
        public bool HideOnFinish
        {
            get => hideOnFinish;
            set
            {
                hideOnFinish = value;
                if (value==false && Hiding)
                {
                    Hiding = false;
                    FadeInWindow();
                }
            }
        }
        private bool hideOnFinish { get; set; }

        public bool ClassChanges { get => classChanges; 
            set
            {
                classChanges = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ClassChanges"));
            }
        }

        private bool classChanges { get; set; }
        public bool TablePositionPreference
        {
            get => tablePositionPreference;
            set
            {
                tablePositionPreference = value;
            }
        }
        private bool tablePositionPreference { get; set; }
        private bool Hiding = true;
        public int Offset { get; set; }

        public bool MoveRequest = false;
        private bool CurrentlyHovered = false;

        public TimeSpan Countdown
        {
            get => countdown;
            set
            {
                countdown = value;
                if (PropertyChanged!=null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Countdown"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CountDownStr"));
                }
            }
            
        }
        public string CountDownStr
        {
            get
            {
                return $"{Countdown:hh\\:mm\\:ss}";
            }
        }

        public string LabelDesc { get => labelDesc; set {
                labelDesc = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("LabelDesc"));
            }
        }
        public string LabelRoom { get => labelRoom; set
            {
                labelRoom = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("LabelRoom"));
            }
        }

        public bool ShowTable { get => showTable; set
            {
                showTable = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowTable"));

                if (value)
                {
                    if (tablewindow == null || tablewindow.IsLoaded == false)
                        tablewindow = new TableWindow();

                    Point tmp = new Point(Left, Top-tablewindow.Height);
                    Point Bottmp = new Point(Left + tablewindow.Width, Top);

                    int topcheck = CornersVisible(tmp,Bottmp);
                    Bottmp.Y = Top + Height + tablewindow.Height;
                    tmp.Y = Top + Height;
                    int botcheck = CornersVisible(tmp, Bottmp);
                    if (topcheck == botcheck)
                    {
                        if (TablePositionPreference)
                        {
                            botcheck = 10;
                            topcheck = 1;
                        }
                        else
                        {
                            botcheck = 1;
                            topcheck = 10;
                        }
                    }

                    if (topcheck>botcheck)
                    {
                        tablewindow.Top = Top - tablewindow.Height;
                        tablewindow.Left = Left;
                    }
                    else
                    {
                        tablewindow.Top = Top + Height;
                        tablewindow.Left = Left;
                    }
                    tablewindow.Schedule = Schedule;
                    tablewindow.Show();
                    tablewindow.Activate();

                }
                else
                {
                    tablewindow?.Hide();
                }
            }
        }

        private TableWindow tablewindow = new TableWindow();

        //Event to fire when a period changed for bell
        protected virtual void OnBell(EventArgs e)
        {
            EventHandler handler = BellTrigger;
            handler?.BeginInvoke(this, e, EndAsyncEvent, null);
        }

        protected virtual void EndAsyncEvent(IAsyncResult iar)
        {
            var ar = (System.Runtime.Remoting.Messaging.AsyncResult)iar;
            var invokedMethod = (EventHandler)ar.AsyncDelegate;
            try
            {
                invokedMethod.EndInvoke(iar);
            }
            catch
            {
                //do nothing
            }
        }

        public List<TimetablePeriod> Schedule { get => schedule; set
            {
                schedule = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Schedule"));
            }
        }

        private string labelRoom;

        private TimeSpan countdown = new TimeSpan(0, 0, 0);

        private string labelDesc;

        DispatcherTimer SecTimer = new DispatcherTimer();

        private bool showTable = false;
        public event PropertyChangedEventHandler PropertyChanged;

        public TimetableClock()
        {
            tablewindow.Loaded += Tablewindow_Loaded;
            Schedule = new List<TimetablePeriod>();
            InitializeComponent();
            DefClock.MouseHoveringHide += new EventHandler(Grid_MouseEnter);
            this.DataContext = this;
            currentTimetableDay = CalculateTimetableDay(DateTime.Now);
            
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentTimetableDay"));
            }
            

            //ContentCtrl.Content = new Button();
            SecTimer.Interval = TimeSpan.FromMilliseconds(250);
            SecTimer.Tick += SecTimer_Tick;
            SecTimer.Start();
        }

        private void Tablewindow_Loaded(object sender, RoutedEventArgs e)
        {
            tablewindow.Schedule = Schedule;
        }

        public void SetSchedule(List<TimetablePeriod> periods)
        {
            //Order list by start time of the periods
            Schedule = periods.OrderBy(o => o.Start).ToList();
            ClassChanges = false;
            for (int i = 0; i < Schedule.Count; i++)
            {
                if (Schedule[i].Changes)
                {
                    ClassChanges = true;
                    break;
                }
            }

            if (tablewindow != null || tablewindow.IsLoaded == false)
            {
                tablewindow.Schedule = Schedule;
            }
            else
            {
                tablewindow = new TableWindow();
                tablewindow.Schedule = Schedule;
            }
        }

        //Returns the number of times the corner is visible on screens to compare
        private int CornersVisible(Point pos, Point Botpos)
        {
            int count = 0;
            var allscreens = System.Windows.Forms.Screen.AllScreens;
            for (int i = 0; i < 4; i++)
            {
                Point tmp = pos;
                switch (i)
                {
                    case 0:
                        //Top left
                        break;
                    case 1:
                        //Top Right
                        tmp.X = Botpos.X;
                        break;
                    case 2:
                        //Bottom Left
                        tmp.Y = Botpos.Y;
                        break;
                    case 3:
                        //Bottom Right
                        tmp = Botpos;
                        break;
                }

                //Check if corner is present 
                for (int p = 0; p < allscreens.Length; p++)
                {
                    if (allscreens[p].Bounds.Contains((int)tmp.X, (int)tmp.Y))
                        count++;
                }
            }
            return count;
        }
        
        //Update timer
        private void SecTimer_Tick(object sender, EventArgs e)
        {
            bool ScheduleFinished = false;
            bool IsGotoPeriod = false;

            //Check if the day has changed
            if (LastDay.ToShortDateString() != DateTime.Now.ToShortDateString())
            {
                LastDay = DateTime.Now;
                currentTimetableDay = CalculateTimetableDay(DateTime.Now);
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentTimetableDay"));
            }

            //Time to compare against
            DateTime RN = DateTime.Now.AddSeconds(Offset);
            int i = 0;
            for (i = 0;  i <= Schedule.Count; i++)
            {
                if (i == Schedule.Count)
                {
                    IsGotoPeriod = false;
                    Countdown = TimeSpan.Zero;
                    LabelDesc = "End";
                    LabelRoom = "";
                    ScheduleFinished = true;
                    break;
                }
                if (Schedule[i].GotoPeriod)
                {
                    IsGotoPeriod = false;
                    DateTime GotoTime = Schedule[i].Start.AddMinutes(-5);
                    if (Timetablehandler.CompareInBetween(GotoTime, Schedule[i].Start, RN) && !(i!=0 && Schedule[i-1].Classcode == Schedule[i].Classcode))
                    {
                        IsGotoPeriod = true;
                        Countdown = Schedule[i].Start - RN;
                        LabelDesc = "Go to " + AutoDesc(Schedule[i]);
                        LabelRoom = Schedule[i].Roomcode;
                        break;
                    }
                    else if (Timetablehandler.CompareInBetween(Schedule[i].Start, Schedule[i].End, RN))
                    {
                        if (CombineDoubles && i+1 < Schedule.Count && Schedule[i].Classcode == Schedule[i+1].Classcode)
                        {
                            Countdown = Schedule[i+1].End - RN;
                        }
                        else
                        {
                            Countdown = Schedule[i].End - RN;
                        }
                        LabelDesc = AutoDesc(Schedule[i]);
                        LabelRoom = Schedule[i].Roomcode;
                        break;
                    }
                    else if ( Schedule[i].Start > RN)
                    {
                        if (i!= 0 && Schedule[i - 1].Classcode == Schedule[i].Classcode)
                        {
                            Countdown = schedule[i].End - RN;
                            LabelDesc = AutoDesc(Schedule[i]);
                            LabelRoom = Schedule[i].Roomcode;
                            break;
                        }
                        Countdown = GotoTime - RN;
                        LabelDesc = "Next " + AutoDesc(Schedule[i]);
                        LabelRoom = Schedule[i].Roomcode;
                        break;
                    }
                }
                else if (Timetablehandler.CompareInBetween(Schedule[i].Start, Schedule[i].End, RN))
                {
                    IsGotoPeriod = false;
                    if (CombineDoubles && i + 1 < Schedule.Count && Schedule[i].Classcode == Schedule[i + 1].Classcode)
                    {
                        Countdown = Schedule[i + 1].End - RN;
                    }
                    else
                    {
                        Countdown = Schedule[i].End - RN;
                    }
                    LabelDesc = AutoDesc(Schedule[i]);
                    LabelRoom = Schedule[i].Roomcode;
                    break;
                }
                else if (Schedule[i].Start > RN && Schedule[i].Start < Schedule[i].End)
                {
                    IsGotoPeriod = false;
                    Countdown = Schedule[i].Start - RN;
                    LabelDesc = "Next " + AutoDesc(Schedule[i]);
                    LabelRoom = Schedule[i].Roomcode;
                    break;
                }
            }

            bool Belltrigger = false;
            if (i < Schedule.Count)
            {
                if ((Schedule[i].Start - RN).TotalMilliseconds < 1000 && (Schedule[i].Start - RN).TotalMilliseconds > 0)
                {
                    IsGotoPeriod = true;
                    Belltrigger = true;
                }
                else if (Schedule[i].End > Schedule[i].Start && (Schedule[i].End - RN).TotalMilliseconds < 1000 && (Schedule[i].End - RN).TotalMilliseconds > 0)
                {
                    Belltrigger = true;
                }
            }


            //Bell check
            if (Belltrigger)
            {
                BellFlag = true;
            }
            else if (BellFlag == true && (IsGotoPeriod != LastPeriodGoto || i > Lastperiod))
            {
                BellFlag = false;
                OnBell(new EventArgs());
            }
            Lastperiod = i;
            LastPeriodGoto = IsGotoPeriod;

            //Variables for auto hiding check
            bool IsFullscreenApp = false;
            bool IsScheduleDone = false;
                    
            //Applying User settings -- Run checks
            if (HideOnFullscreen)
            {
                IsFullscreenApp = IsForegroundFullScreen(System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle), true);
            }

            if (HideOnFinish)
            {
                IsScheduleDone = ScheduleFinished;
            }


            bool PlsHide = IsScheduleDone == true || IsFullscreenApp == true;
            //Check if criteria to hide is true
            if (Hiding == false && PlsHide == true)
            {
                Hiding = true;
                FadeOutWindow();
            } //If previouos criteria failed then check if required to reshow
            else if (Hiding == true && PlsHide == false)
            {
                Hiding = false;
                //Only Fadein if mouse isn't hovering
                if (CurrentlyHovered == false)
                {
                    FadeInWindow();
                }
            }
        }

        private int CalculateTimetableDay(DateTime LocalDay)
        {
            if (LocalDay.DayOfWeek == DayOfWeek.Sunday || LocalDay.DayOfWeek == DayOfWeek.Saturday)
            {
                return 0;
            }
            if (FirstDayDate.DayOfWeek == DayOfWeek.Monday)
            {
                int days = (int)Math.Ceiling(LocalDay.Subtract(FirstDayDate).TotalDays) % 14;

                if (days < 0)
                    days += 14;

                switch (days)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        return days;
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                        return days - 2;
                    default:
                        return 0;
                }

            }
            else
            {
                return 0;
            }
        }

        private string AutoDesc(TimetablePeriod period)
        {
            return period.Description.Length < 13 ? period.Description : period.Classcode;
        }

        //This is required as stuttering will occur if you make the object disappear as it will forget its previous size
        private void Grid_MouseEnter(object sender, EventArgs e)
        {
            if (!ShowTable && FadeOnHover && !MoveRequest)
            {
                double Current_Width = ContentGrid.ActualWidth;
                double Current_Height = ContentGrid.ActualHeight;
                Point Pos = this.PointToScreen(new Point(Width - Current_Width, Height - Current_Height));
                Point PosBottom = this.PointToScreen(new Point(Width, Height));
                DispatcherTimer Checker = new DispatcherTimer();
                Checker.Interval = TimeSpan.FromMilliseconds(10);
                CurrentlyHovered = true;
                FadeOutWindow();
                //Loop to check if mouse leaves
                Checker.Tick += (s, eargs) =>
                {
                    //Cast it to an easier to reference object
                    System.Drawing.Point mousepoint = System.Windows.Forms.Control.MousePosition;
                    //Check if it is out of the bounds of the previous rectangle
                    if ( ShowTable || mousepoint.X < Pos.X ||
                        mousepoint.Y < Pos.Y ||
                        mousepoint.X > PosBottom.X ||
                        mousepoint.Y > PosBottom.Y
                    )
                    {
                        //Stop the checking timer and resume visiblity 
                        Checker.Stop();
                        CurrentlyHovered = false;

                        //Only Fadein if isn't hidden by something else
                        if (!Hiding)
                        {
                            FadeInWindow();
                        }
                    }
                };
                Checker.Start();
            }
        }

        //shared storyboard to prevent fighting of multiple storyboards that are incompleted
        Storyboard storyboard;

        private void FadeInWindow()
        {
            if (storyboard != null)
            {
                storyboard.Stop();
            }

            // Create a storyboard to contain the animations.
            storyboard = new Storyboard();
            TimeSpan duration = TimeSpan.FromMilliseconds(100);

            // Create a DoubleAnimation to fade the not selected option control
            DoubleAnimation animation = new DoubleAnimation();

            animation.From = 0.0;
            animation.To = 1.0;
            animation.Duration = new Duration(duration);
            // Configure the animation to target de property Opacity
            Storyboard.SetTargetName(animation, ContentGrid.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));
            // Add the animation to the storyboard
            storyboard.Children.Add(animation);
            storyboard.Completed += Storyboard_Completed;
            // Begin the storyboard
            storyboard.Begin(this);
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            storyboard = null;
        }

        private void FadeOutWindow()
        {

            if (storyboard != null)
            {
                storyboard.Stop();
            }

            // Create a storyboard to contain the animations.
            storyboard = new Storyboard();
            TimeSpan duration = TimeSpan.FromMilliseconds(100);

            // Create a DoubleAnimation to fade the not selected option control
            DoubleAnimation animation = new DoubleAnimation();

            animation.From = 1.0;
            animation.To = 0.0;
            animation.Duration = new Duration(duration);
            // Configure the animation to target de property Opacity
            Storyboard.SetTargetName(animation, ContentGrid.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));
            // Add the animation to the storyboard
            storyboard.Children.Add(animation);
            storyboard.Completed += Storyboard_Completed;

            // Begin the storyboard
            storyboard.Begin(this);
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Variable to hold the handle for the form
            var helper = new WindowInteropHelper(this).Handle;
            //Performing some magic to hide the form from Alt+Tab
            SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        }

        private void ContentGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !MoveRequest )
            {
                ShowTable = !ShowTable;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);


        public static bool IsForegroundFullScreen()
        {
            return IsForegroundFullScreen(null);
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            tablewindow?.Close();
        }

        public static bool IsForegroundFullScreen(System.Windows.Forms.Screen screen, bool SameScreen = false)
        {

            if (screen == null)
            {
                screen = System.Windows.Forms.Screen.PrimaryScreen;
            }
            RECT rect = new RECT();
            IntPtr hWnd = (IntPtr)GetForegroundWindow();

            GetWindowRect(new HandleRef(null, hWnd), ref rect);
            
            try
            {
                //in case you want the process name:

                const int nChars = 256;
                StringBuilder Buff = new StringBuilder(nChars);
                string tt = "";

                if (GetWindowText(hWnd, Buff, nChars) > 0)
                {
                    tt = Buff.ToString();
                }


                //Console.WriteLine(proc.ProcessName); //Check if its a window process
                if (tt == "explorer" || tt == "")
                {
                    return false;
                }
            }
            catch
            {
                //Weird stuff can happen
            }

            
            bool Fullsize = screen.Bounds.Width == (rect.right - rect.left) && screen.Bounds.Height == (rect.bottom - rect.top);

            if (SameScreen)
            {
                return new System.Drawing.Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top).Contains(screen.Bounds) && Fullsize;
            }
            return Fullsize;


        }


    }


}


