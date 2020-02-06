using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using SQLiteNetExtensions.Extensions;
using Firefly;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Media;
using SQLiteNetExtensions.Attributes;
using System.Windows.Forms;

namespace MYTGS
{
    public partial class MainWindow 
    {
        public static List<Brush> ColourPallete = new List<Brush>()
        {
            Brushes.Cyan,
            Brushes.DodgerBlue,
            Brushes.Orange,
            Brushes.Green,
            Brushes.Tan,
            Brushes.Magenta,
            Brushes.Gray,
            Brushes.Teal
        };
        private int colourpos = new Random().Next(0, ColourPallete.Count);

        public DateTime FFEventsLastUpdated
        {
            get => ffEventsLastUpdated;
            set
            {
                ffEventsLastUpdated = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("FFEventsLastUpdated"));
                }
                DBUpdateItem("Trinity", new SettingsItem("FFEventsLastUpdated", JsonConvert.SerializeObject(value)));
            }
        }
        private DateTime ffEventsLastUpdated { get; set; }

        private void InitializeEventDB(string school)
        {
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.CreateTable<FFEvent>();
                db.CreateTable<SettingsItem>();
                db.CreateTable<ColourItem>();
            }
        }

        public string GetLastEventUpdate(string school, string Name)
        {
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                TableQuery<SettingsItem> results = db.Table<SettingsItem>().Where(s => s.name == Name);
                if (results.Count() > 0)
                {
                    return results.First().value;
                }
                else
                {
                    return "";
                }
            }
        }

        private void LoadEventInfo(string school)
        {
            try
            {
                ffEventsLastUpdated = JsonConvert.DeserializeObject<DateTime>(GetLastEventUpdate(school, "FFEventsLastUpdated"));
            }
            catch
            {
                ffEventsLastUpdated = new DateTime();
            }
        }

        private void DBInsert(string school, object obj)
        {
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.InsertWithChildren(obj);
            }
        }

        private void DBUpdateItem(string school, object obj)
        {
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.InsertOrReplaceWithChildren(obj);
            }
        }

        private void DBInsertAll(string school, System.Collections.IEnumerable obj)
        {
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.InsertAllWithChildren(obj);
            }
        }

        private void DBInsertOrReplace(string school, System.Collections.IEnumerable obj)
        {
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");
            
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.InsertOrReplaceAllWithChildren(obj);
            }
        }

        private void DBUpdateEvents(string school, FFEvent[] obj, DateTime StartUTC, DateTime EndUTC)
        {
            //Get path to database
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            //Open connection
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                //Find all entries for the given period that no longer exist
                var deleted = db.Table<FFEvent>().Where(s => (s.start >= StartUTC && s.end <= EndUTC ))
                    .ToList().Select(s => s.guid).Except(obj.Select(p => p.guid));

                //Delete the lost entries
                db.DeleteAllIds<FFEvent>(deleted);

                //Insert/update all event entries
                db.InsertOrReplaceAllWithChildren(obj);
            }
        }

        private FFEvent[] DBGetAllEvents(string school)
        {
            //Get path to database
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            //Open connection
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                //Return the table in array form
                return db.Table<FFEvent>().ToArray();
            }
        }

        private void DBWipe(string school)
        {
            //Get path to database
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            //Open connection
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                //Return the table in array form
                db.DeleteAll<SettingsItem>();
                db.DeleteAll<FFEvent>();
                db.DeleteAll<ColourItem>();
            }
        }

        private FFEvent[] DBGetDayEvents(string school, DateTime Day)
        {
            //Get path to database
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            //Open connection
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                //Get the start of the local day to utc
                DateTime start = new DateTime(Day.Year, Day.Month, Day.Day, 0, 0, 0).ToUniversalTime();
                //Get the end of the local day to utc
                DateTime end = new DateTime(Day.Year, Day.Month, Day.Day, 0, 0, 0).AddDays(1).ToUniversalTime();

                //Find all events that meet criteria and return array
                return db.Table<FFEvent>().Where(s => (s.start >= start && s.end <= end)).ToArray();
            }
        }

        private FFEvent[] DBGetEventsBetween(string school, DateTime startUTC, DateTime endUTC)
        {
            //Get path to database
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            //Open connection
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                //Find all events that meet criteria and return array
                return db.Table<FFEvent>().Where(s => (s.start >= startUTC && s.end <= endUTC)).ToArray();
            }
        }

        private ColourItem DBGetColour(string school, string name)
        {
            //Get path to database
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            //Open connection
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                //Find all events that meet criteria and return array
                var temp = db.Table<ColourItem>().Where(s => s.name == name);
                if (temp.Count() > 0)
                {
                    ColourItem t = temp.First();
                    t.value = JsonConvert.DeserializeObject<Brush>(t.valueBlobbed);
                    return t;
                }
                else
                {
                    Brush random = ColourPallete[colourpos % ColourPallete.Count];
                    colourpos++;
                    DBInsert(school, new ColourItem(name, random));
                    return new ColourItem(name, random);
                }
            }
        }


        struct ColourItem
        {
            [PrimaryKey, Unique]
            public string name { get; set; }
            
            [TextBlob("valueBlobbed")]
            public Brush value { get; set; }

            public string valueBlobbed { get; set; }

            public ColourItem(string Name, Brush Value)
            {
                name = Name;
                value = Value;
                valueBlobbed = "";
            }

        }

        Grid[] PlannerGrids = new Grid[7];
        private void GeneratePlanner(DateTime CurrentTime, int left = 3, int right = 3)
        {
            PlannerGrid.Children.Clear();

            for (int i = 0; i <= left+right; i++)
            {
                TimetablePeriod[] dayperiods = Timetablehandler.ParseEventsToPeriods(DBGetDayEvents("Trinity", CurrentTime.AddDays(-left+i)));

                System.Windows.Controls.Label first = new System.Windows.Controls.Label();
                first.FontSize = 14;
                first.Foreground = new SolidColorBrush(Color.FromRgb(0x5D, 0x89, 0xFF));
                first.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                first.Content = CurrentTime.AddDays(-left + i).ToShortDateString() + " " + CurrentTime.AddDays(-left + i).DayOfWeek;
                if (CurrentTime.AddDays(-left + i).ToShortDateString() == DateTime.Now.ToShortDateString())
                {
                    first.Foreground = Brushes.OrangeRed;
                }
                first.SetValue(Grid.ColumnProperty, i);
                PlannerGrid.Children.Add(first);

                for (int k = 0; k < 7; k++)
                {
                    if (dayperiods[k].Start == new DateTime())
                    {
                        continue;
                    }
                    Period pp = new Period();
                    pp.FontSize = 14;
                    pp.MouseDown += Pp_MouseDown;
                    pp.SetValue(Grid.ColumnProperty, i);
                    pp.SetValue(Grid.RowProperty, k+1);
                    pp.Margin = new System.Windows.Thickness(1);
                    pp.Background = DBGetColour("Trinity", dayperiods[k].Classcode).value;
                    pp.DataContext = dayperiods[k];
                    PlannerGrid.Children.Add(pp);
                }

            }


        }

        private void Pp_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //((TimetablePeriod)((Period)sender).DataContext).Classcode
            ColorDialog picker = new ColorDialog();
            picker.AllowFullOpen = true;
            Color ck = ((SolidColorBrush)((Period)sender).Background).Color;
            picker.Color = System.Drawing.Color.FromArgb(ck.A, ck.R, ck.G, ck.B);
            if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(picker.Color.A, picker.Color.R, picker.Color.G, picker.Color.B));
                DBUpdateItem("Trinity" ,new ColourItem(((TimetablePeriod)((Period)sender).DataContext).Classcode, brush));
                GeneratePlanner(PlannerDate);
            }

        }
    }
}
