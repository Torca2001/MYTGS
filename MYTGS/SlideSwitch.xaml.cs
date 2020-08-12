using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for SlideSwitch.xaml
    /// </summary>
    public partial class SlideSwitch : UserControl, INotifyPropertyChanged
    {
        public double Percentage { get; set; } = 0;

        [DefaultValue(typeof(int), "2")]
        public int Options
        {
            get => options;
            set
            {
                options = value;
                Intervals = 1f / (options - 1);
                LocktoClosest();
            }
        }

        //make sure to set intervals if chaning default options
        private int options { get; set; } = 2;

        private float Intervals = 1;


        public event EventHandler ChangedSelected;
        public int Selected
        {
            get
            {
                return selected;
            }

            set
            {
                int old = selected;
                SetTo(value);
                if (old != value)
                    OnChangeSelected(new ChangedEventArgs(Selected, old));
            }
        }

        public class ChangedEventArgs : EventArgs
        {
            public ChangedEventArgs(int newSelected, int oldSelected)
            {
                NewSelected = newSelected;
                OldSelected = oldSelected;
            }

            public int NewSelected { get; private set; }
            public int OldSelected { get; private set; }
        }

        protected virtual void OnChangeSelected(ChangedEventArgs e)
        {
            EventHandler handler = ChangedSelected;
            handler?.Invoke(this, e);
        }
        private int selected { get; set; } = 1;

        public static readonly DependencyProperty HundredPercentProperty = DependencyProperty.Register("HundredPercent", typeof(bool), typeof(SlideSwitch), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnHundredPercent)) { BindsTwoWayByDefault = true });

        private static void OnHundredPercent(DependencyObject src, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                ((SlideSwitch)src).Selected = ((SlideSwitch)src).options;
            }
            else if ((bool)e.NewValue == false && ((SlideSwitch)src).Selected == ((SlideSwitch)src).options)
            {
                ((SlideSwitch)src).Selected = 1;
            }
        }

        public bool HundredPercent
        {
            get => selected == options;
            set
            {
                if (value == true)
                {
                    Selected = options;
                }
                else
                {
                    Selected = 1;
                }
                SetValue(HundredPercentProperty, value);
            }
        }

        public Thickness LeftMargin
        {
            get
            {
                return new Thickness(ActualHeight / 2, 0, (ActualWidth- ActualHeight / 2)-pos, 0);
            }
        }

        public Thickness RightMargin
        {
            get
            {
                return new Thickness(pos + ActualHeight / 2,0, ActualHeight / 2,0);
            }
        }

        [Browsable(false)]
        public int pos { get
            {
                return (int)(Percentage * (ActualWidth - ActualHeight));
            } 
        }

        public SlideSwitch()
        {
            InitializeComponent();
        }

        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void usrctl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            VarsUpdating();
        }

        private void VarsUpdating()
        {
            NotifyPropertyChanged("Selected");
            NotifyPropertyChanged("pos");
            NotifyPropertyChanged("LeftMargin");
            NotifyPropertyChanged("RightMargin");
            NotifyPropertyChanged("HundredPercent");
        }

        private void usrctl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Focus();
                Double clamping = (2 * e.GetPosition(this).X - ActualHeight) / (2 * (ActualWidth-ActualHeight));
                
                if (clamping > 1)
                {
                    clamping = 1;
                }
                else if (clamping < 0)
                {
                    clamping = 0;
                }
                Percentage = clamping;
                VarsUpdating();
            }
        }

        private void usrctl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Focus();
                Double clamping = (2 * e.GetPosition(this).X - ActualHeight) / (2 * (ActualWidth - ActualHeight));
                if (clamping > 1)
                {
                    clamping = 1;
                }
                else if (clamping < 0)
                {
                    clamping = 0;
                }
                Percentage = clamping;
                VarsUpdating();
            }
        }

        private void usrctl_MouseLeave(object sender, MouseEventArgs e)
        {
            LocktoClosest();
        }

        private void LocktoClosest()
        {
            int i = 0;
            for (i = 0; i < options-1; i++)
            {
                if (i*Intervals - Intervals/2 < Percentage && Intervals/2 + Intervals*i >= Percentage)
                {
                    //check if its within range
                    break;
                }
            }
            Selected = i+1;
            SetValue(HundredPercentProperty, HundredPercent);
        }

        private void SetTo(int i)
        {
            i = i - 1;
            if (i > options)
            {
                throw new Exception("Out of upper range");
            }
            else if (i < 0)
            {
                throw new Exception("Out of lower range");
            }
            selected = i+1;
            Percentage = i * Intervals;
            VarsUpdating();
        }

        private void usrctl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                LocktoClosest();
            }
        }

        private void usrctl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    if (Selected > 1)
                    {
                        Selected--;
                    }
                    break;
                case Key.Right:
                    if (Selected < Options)
                    {
                        Selected++;
                    }

                    break;
                case Key.Space:
                    HundredPercent = !HundredPercent;
                    break;
            }
        }
    }
}
