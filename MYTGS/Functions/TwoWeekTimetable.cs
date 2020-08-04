using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using SQLite;
using SQLiteNetExtensions.Extensions;
using System.Threading.Tasks;
using Ical.Net;
using System.IO;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Windows;
//Code for calculating two week timetable and populating wpf page

namespace MYTGS
{
    public partial class MainWindow
    {
        ColorDialog picker = new ColorDialog();
        public int CurrentTimetableDay { get => CalculateTimetableDay(DateTime.Now); }
        public int SelectedDay { 
            get => selectedDay; 
            set
            {
                selectedDay = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedDay"));
                }
            }
        }
        private int selectedDay = 0;

        public bool ShowPeriodSidePanel
        {
            get { return showPeriodSidePanel; }
            set
            {
                showPeriodSidePanel = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowPeriodSidePanel"));
                }
            }
        }
        private bool showPeriodSidePanel = false;
        public DateTime FirstDayDate { get => firstDayDate;
            set
                {
                    firstDayDate = value;
                    settings.SaveSettings("FirstDayDate", JsonConvert.SerializeObject(value));

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("FirstDayDate"));
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentTimetableDay"));
                    if (ClockWindow != null)
                        ClockWindow.FirstDayDate = FirstDayDate;
                }
            }
            }
        private DateTime firstDayDate { get; set; } = new DateTime(2020, 2, 10);

        public TimetablePeriod[,] TwoWeekTimetable { get; set; }

        private void UpdateFirstDay(DateTime eprday, int day)
        {
            if (eprday.Year < 5)
            {
                logger.Warn("Unable toget First day from EPR, Out of Range Date");
                return;
            }

            switch (day)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                    day--;
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                    day++;
                    break;
            }

            eprday = eprday.AddDays(-day);
            eprday = eprday.Subtract(eprday.TimeOfDay);
            if (eprday.DayOfWeek == DayOfWeek.Monday)
            {
                if (FirstDayDate != eprday)
                {
                    logger.Info("Updated first day " + eprday.ToString() + " is now day 1");
                }
                FirstDayDate = eprday;
            }
            else
            {
                logger.Warn("Locating First day from EPR failed");
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

        private TimetablePeriod[,] LocateTwoWeeks(DateTime Firstday, int ShiftAhead = 2, int Maxshifting = 5)
        {
            //Firstday needs to be a monday
            if (Firstday.DayOfWeek != DayOfWeek.Monday)
            {
                return new TimetablePeriod[0, 0];
            }

            TimetablePeriod[,] Days = new TimetablePeriod[10, 7];
            bool AllPopulated = false;
            int TwoWeekShift = ShiftAhead;
            while (AllPopulated == false && -TwoWeekShift < Maxshifting)
            {
                AllPopulated = true;
                for (int i = 0; i < 10; i++)
                {
                    int shift = i;
                    if (shift > 4)
                    {
                        shift += 2;
                    }
                    Firefly.FFEvent[] events = DBGetDayEvents(dbSchool, Firstday.AddDays(TwoWeekShift * 14 + shift));
                    TimetablePeriod[] dayperiods = Timetablehandler.ParseEventsToPeriods(events);
                    int founds = 0;
                    for (int k = 0; k < 7; k++)
                    {
                        if (dayperiods[k].Start == new DateTime())
                        {
                            continue;
                        }
                        founds++;
                    }

                    //If one or more valid periods accept the day
                    if (founds == 0)
                    {
                        TwoWeekShift--;
                        AllPopulated = false;
                        break;
                    }

                    for (int k = 0; k < 7; k++)
                    {
                        Days[i, k] = dayperiods[k];
                    }
                }
            }

            if (AllPopulated == false)
            {
                return new TimetablePeriod[0, 0];
            }

            return Days;
        }

        private void GenerateTwoWeekTimetable()
        {
            TwoWeekTimetableGrid.Children.Clear();


            for (int i = 0; i < 10; i++)
            {
                for (int k = 0; k < 7; k++)
                {
                    if (TwoWeekTimetable.GetLength(0) <= i || TwoWeekTimetable.GetLength(1) <= k)
                    {
                        continue;
                    }
                    if (TwoWeekTimetable[i,k].Start == new DateTime())
                    {
                        continue;
                    }
                    Period pp = new Period();
                    pp.FontSize = 10;
                    pp.SecondaryFontSize = 12;
                    pp.MouseDown += TT_MouseDown;
                    pp.SetValue(Grid.ColumnProperty, i);
                    pp.SetValue(Grid.RowProperty, k);
                    pp.Margin = new System.Windows.Thickness(1);
                    pp.Foreground = DBGetColour(dbSchool, TwoWeekTimetable[i, k].Classcode + "-text", Brushes.White).value;
                    pp.Background = DBGetColour(dbSchool, TwoWeekTimetable[i, k].Classcode).value;
                    pp.DataContext = TwoWeekTimetable[i, k];
                    TwoWeekTimetableGrid.Children.Add(pp);
                }

            }

            if (TwoWeekTimetableGrid.Children.Count == 0)
            {
                System.Windows.Controls.Label lbl = new System.Windows.Controls.Label();
                lbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                lbl.Foreground = Brushes.Red;
                lbl.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                lbl.SetValue(Grid.RowSpanProperty, 7);
                lbl.SetValue(Grid.ColumnSpanProperty, 10);
                lbl.Content = "There was a problem finding a two week timetable from the planner. \n Try viewing the planner at the top right. \n If you think this is a mistake please contact the application author, \n contact details in the settings page";
                TwoWeekTimetableGrid.Children.Add(lbl);
            }


        }

        private void TT_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender is Period) == false)
            {
                return;
            }

            //((TimetablePeriod)((Period)sender).DataContext).Classcode
            if (SelectedDay != Grid.GetColumn((Period)sender)+1 || PeriodSideGrid.DataContext != ((Period)sender).DataContext)
            {
                ShowPeriodSidePanel = true;
                PeriodSideGrid.DataContext = ((Period)sender).DataContext;
                SelectedDay = Grid.GetColumn((Period)sender)+1;
            }
            else
            {
                ShowPeriodSidePanel = !ShowPeriodSidePanel;
            }
        }

        //Relevant UI events for Timetable

        //Close side window
        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            ShowPeriodSidePanel = false;
        }

        //Change Text Colour
        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button && ((System.Windows.Controls.Button)sender).DataContext is TimetablePeriod)
            {
                picker.AllowFullOpen = true;
                picker.AnyColor = true;
                Color ck = ((SolidColorBrush)DBGetColour(dbSchool, ((TimetablePeriod)((System.Windows.Controls.Button)sender).DataContext).Classcode + "-text").value).Color;
                picker.Color = System.Drawing.Color.FromArgb(ck.A, ck.R, ck.G, ck.B);
                if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(picker.Color.A, picker.Color.R, picker.Color.G, picker.Color.B));
                    DBUpdateItem(dbSchool, new ColourItem(((TimetablePeriod)((System.Windows.Controls.Button)sender).DataContext).Classcode + "-text", brush));
                    GenerateTwoWeekTimetable();
                }
            }
        }

        //Change Background Colour
        private void Button_Click_13(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button && ((System.Windows.Controls.Button)sender).DataContext is TimetablePeriod)
            {
                picker.AllowFullOpen = true;
                picker.AnyColor = true;
                Color ck = ((SolidColorBrush)DBGetColour(dbSchool, ((TimetablePeriod)((System.Windows.Controls.Button)sender).DataContext).Classcode).value).Color;
                picker.Color = System.Drawing.Color.FromArgb(ck.A, ck.R, ck.G, ck.B);
                if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(picker.Color.A, picker.Color.R, picker.Color.G, picker.Color.B));
                    DBUpdateItem(dbSchool, new ColourItem(((TimetablePeriod)((System.Windows.Controls.Button)sender).DataContext).Classcode, brush));
                    GenerateTwoWeekTimetable();
                }
            }
        }

    }
}


