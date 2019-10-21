using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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
        public List<TimetablePeriod> Schedule = new List<TimetablePeriod>();

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
            } }
        public string LabelRoom { get => labelRoom; set
            {
                labelRoom = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("LabelRoom"));
            }
        }

        private string labelRoom;

        private TimeSpan countdown = new TimeSpan(0, 0, 0);

        private string labelDesc;

        DispatcherTimer SecTimer = new DispatcherTimer();

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
            Schedule = periods.OrderBy(o => o.Start).ToList();
        }



        private void SecTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i <= Schedule.Count; i++)
            {
                if (i == Schedule.Count)
                {
                    Countdown = TimeSpan.Zero;
                    LabelDesc = "End";
                    LabelRoom = "";
                    break;
                }
                if (Schedule[i].GotoPeriod)
                {
                    DateTime GotoTime = Schedule[i].Start.AddMinutes(-5);
                    if (Timetablehandler.CompareInBetween(GotoTime, Schedule[i].Start, DateTime.UtcNow))
                    {
                        Countdown = Schedule[i].Start.ToLocalTime() - DateTime.Now;
                        LabelDesc = "Go to " + AutoDesc(Schedule[i]);
                        LabelRoom = Schedule[i].Roomcode;
                        break;
                    }
                    else if (Timetablehandler.CompareInBetween(Schedule[i].Start, Schedule[i].End, DateTime.UtcNow))
                    {
                        Countdown = Schedule[i].End.ToLocalTime() - DateTime.Now;
                        LabelDesc = AutoDesc(Schedule[i]);
                        LabelRoom = Schedule[i].Roomcode;
                        break;
                    }
                }
                else if (Timetablehandler.CompareInBetween(Schedule[i].Start, Schedule[i].End, DateTime.UtcNow))
                {
                    Countdown = Schedule[i].End.ToLocalTime() - DateTime.Now;
                    LabelDesc = AutoDesc(Schedule[i]);
                    LabelRoom = Schedule[i].Roomcode;
                    break;
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private string AutoDesc(TimetablePeriod period)
        {
            return period.Description.Length < 13 ? period.Description : period.Classcode;
        }
        
    }
}
