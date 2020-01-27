using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for TimetableClock.xaml
    /// </summary>
    public partial class TimetableClock : Window , INotifyPropertyChanged
    {
        private List<TimetablePeriod> schedule = new List<TimetablePeriod>();
        public bool FadeOnHover = false;
        public bool HideOnFullscreen = false;
        public bool HideOnFinish = false;
        private bool Hiding = false;
        public int Offset { get; set; }

        public bool MoveRequest = false;
        private bool AutoHide = false;

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
            InitializeComponent();
            this.DataContext = this;

            //ContentCtrl.Content = new Button();
            SecTimer.Interval = TimeSpan.FromMilliseconds(250);
            SecTimer.Tick += SecTimer_Tick;
            SecTimer.Start();
        }

        public void SetSchedule(List<TimetablePeriod> periods)
        {
            //Order list by start time of the periods
            Schedule = periods.OrderBy(o => o.Start).ToList();
        }



        private void SecTimer_Tick(object sender, EventArgs e)
        {
            int i = 0;
            for (i = 0;  i <= Schedule.Count; i++)
            {
                if (i == Schedule.Count)
                {
                    Countdown = TimeSpan.Zero;
                    LabelDesc = "End";
                    LabelRoom = "";
                    if (!Hiding)
                    {
                        Hiding = true;
                        FadeOutWindow();
                    }
                    break;
                }


                DateTime RN = DateTime.Now.AddSeconds(Offset);
                if (Schedule[i].GotoPeriod)
                {
                    DateTime GotoTime = Schedule[i].Start.AddMinutes(-5);
                    if (Timetablehandler.CompareInBetween(GotoTime, Schedule[i].Start, RN))
                    {
                        Countdown = Schedule[i].Start - RN;
                        LabelDesc = "Go to " + AutoDesc(Schedule[i]);
                        LabelRoom = Schedule[i].Roomcode;
                        break;
                    }
                    else if (Timetablehandler.CompareInBetween(Schedule[i].Start, Schedule[i].End, RN))
                    {
                        Countdown = Schedule[i].End - RN;
                        LabelDesc = AutoDesc(Schedule[i]);
                        LabelRoom = Schedule[i].Roomcode;
                        break;
                    }
                }
                else if (Timetablehandler.CompareInBetween(Schedule[i].Start, Schedule[i].End, RN))
                {
                    Countdown = Schedule[i].End - RN;
                    LabelDesc = AutoDesc(Schedule[i]);
                    LabelRoom = Schedule[i].Roomcode;
                    break;
                }
            }

            if (i != Schedule.Count && Hiding)
            {
                Hiding = false;
                FadeInWindow();
            }

            if (HideOnFullscreen && IsForegroundFullScreen(System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle), true))
            {
                if (!AutoHide)
                {
                    FadeOutWindow();
                    AutoHide = true;
                }
            }
            else
            {
                if (AutoHide)
                {
                    FadeInWindow();
                    AutoHide = false;
                }
            }

        }

        private string AutoDesc(TimetablePeriod period)
        {
            return period.Description.Length < 13 ? period.Description : period.Classcode;
        }

        //This is required as stuttering will occur if you make the object disappear as it will forget its previous size
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if ( FadeOnHover && !MoveRequest)
            {
                double Current_Width = ContentGrid.ActualWidth;
                double Current_Height = ContentGrid.ActualHeight;
                Point Pos = this.PointToScreen(new Point(Width - Current_Width, Height - Current_Height));
                DispatcherTimer Checker = new DispatcherTimer();
                Checker.Interval = TimeSpan.FromMilliseconds(10);
                FadeOutWindow();
                //Loop to check if mouse leaves
                Checker.Tick += (s, eargs) =>
                {
                    //Cast it to an easier to reference object
                    System.Drawing.Point mousepoint = System.Windows.Forms.Control.MousePosition;
                    //Check if it is out of the bounds of the previous rectangle
                    if (mousepoint.X < Pos.X ||
                        mousepoint.Y < Pos.Y ||
                        mousepoint.X > Pos.X + Current_Width ||
                        mousepoint.Y > Pos.Y + Current_Height
                    )
                    {
                        //Stop the checking timer and resume visiblity 
                        Checker.Stop();
                        FadeInWindow();
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

        public static bool IsForegroundFullScreen()
        {
            return IsForegroundFullScreen(null);
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

            /* in case you want the process name:
            uint procId = 0;
            GetWindowThreadProcessId(hWnd, out procId);
            var proc = System.Diagnostics.Process.GetProcessById((int)procId);
            Console.WriteLine(proc.ProcessName);
            */
            bool Fullsize = screen.Bounds.Width == (rect.right - rect.left) && screen.Bounds.Height == (rect.bottom - rect.top);

            if (SameScreen)
            {
                return new System.Drawing.Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top).Contains(screen.Bounds) && Fullsize;
            }
            return Fullsize;


        }


    }


}


