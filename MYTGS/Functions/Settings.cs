using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MYTGS
{
    class Settings
    {
        string databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), "settings.db");

        public void SaveSettings(string Name, string Value)
        {
            SaveSettings(new SettingsItem(Name, Value));
        }

        public void SaveSettings(SettingsItem item)
        {
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.InsertOrReplace(item);
            }
        }

        public string GetSettings(string Name)
        {
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

        public Settings()
        {
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.CreateTable<SettingsItem>();
            }
        }

    }

    struct SettingsItem
    {
        [PrimaryKey, Unique]
        public string name { get; set; }

        public string value { get; set; }

        public SettingsItem(string Name, string Value)
        {
            name = Name;
            value = Value;
        }

    }

    public partial class MainWindow: INotifyPropertyChanged
    {
        public string CalendarUrl
        {
            get => calendarUrl;
            set
            {
                calendarUrl = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("CalendarUrl"));
                }
                settings.SaveSettings("CalendarUrl", value);
            }
        }
        private string calendarUrl { get; set; }

        public bool FadeOnHover
        {
            get => ClockWindow.FadeOnHover;
            set
            {
                ClockWindow.FadeOnHover = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("FadeOnHover"));
                }
                settings.SaveSettings("FadeOnHover", value == true ? "1" : "0");
            }
        }

        public bool HideOnFinish
        {
            get => ClockWindow.HideOnFinish;
            set
            {
                ClockWindow.HideOnFinish = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("HideOnFinish"));
                }
                settings.SaveSettings("HideOnFinish", value == true ? "1" : "0");
            }
        }

        public EPRcollection LastEPR
        {
            get => lastEPR;
            set
            {
                lastEPR = value;
                settings.SaveSettings("LastEPR", JsonConvert.SerializeObject(value));
            }
        }

        private EPRcollection lastEPR { get; set; }

        public bool HideOnFullscreen
        {
            get => ClockWindow.HideOnFullscreen;
            set
            {
                ClockWindow.HideOnFullscreen = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("HideOnFullScreen"));
                }
                settings.SaveSettings("HideOnFullscreen", value == true ? "1" : "0");
            }
        }

        public int Offset
        {
            get => ClockWindow.Offset;
            set
            {
                ClockWindow.Offset = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Offset"));
                }
                settings.SaveSettings("Offset", Offset.ToString());
            }
        }

        public bool StartMinimized
        {
            get => startMinimized;
            set
            {
                startMinimized = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("StartMinimized"));
                }
                settings.SaveSettings("StartMinimized", value == true ? "1" : "0");
            }
        }
        private bool startMinimized { get; set; }

        private void LoadSettings()
        {
            switch (settings.GetSettings("FadeOnHover"))
            {
                case "1":
                    FadeOnHover = true;
                    break;
                case "0":
                    FadeOnHover = false;
                    break;
                default:
                    FadeOnHover = true;
                    break;
            }

            switch (settings.GetSettings("HideOnFinish"))
            {
                case "1":
                    HideOnFinish = true;
                    break;
                case "0":
                    HideOnFinish = false;
                    break;
                default:
                    HideOnFinish = true;
                    break;
            }

            switch (settings.GetSettings("StartMinimized"))
            {
                case "1":
                    StartMinimized = true;
                    break;
                case "0":
                    StartMinimized = false;
                    break;
                default:
                    StartMinimized = true;
                    break;
            }

            switch (settings.GetSettings("HideOnFullscreen"))
            {
                case "1":
                    HideOnFullscreen = true;
                    break;
                case "0":
                    HideOnFullscreen = false;
                    break;
                default:
                    HideOnFullscreen = true;
                    break;
            }

            if (settings.GetSettings("Offset").Length > 0)
            {

                Offset = int.Parse(settings.GetSettings("Offset"));
            }
            else
            {
                Offset = 0;
            }

            try
            {
                lastEPR = JsonConvert.DeserializeObject<EPRcollection>(settings.GetSettings("LastEPR"));
            }
            finally
            {
                if (lastEPR == null)
                {
                    lastEPR = new EPRcollection();
                }
            }

            if (settings.GetSettings("CalendarUrl").Length > 0)
            {
                calendarUrl = settings.GetSettings("CalendarUrl");
            }
            else
            {
                calendarUrl = "https://outlook.office365.com/owa/calendar/2565f03a392b4aa7ae08559caf271bc8@trinity.vic.edu.au/7df612fa5cb549e993a058753de347464943400756469956671/calendar.ics";
            }
        }

    }
}
