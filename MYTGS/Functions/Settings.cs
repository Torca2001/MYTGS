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
        SQLiteConnection db = null;

        public void SaveSettings(string Name, string Value)
        {
            SaveSettings(new SettingsItem(Name, Value));
        }

        public void SaveSettings(SettingsItem item)
        {
            db.InsertOrReplace(item);
        }

        public void Close()
        {
            db?.Close();
        }

        public void Initalize()
        {
            try
            {
                db = new SQLiteConnection(databasePath);
            }
            catch
            {
                Console.WriteLine("Warning - Settings is using a memory DB");
                db = new SQLiteConnection(":memory:");
            }
            db.CreateTable<SettingsItem>();
        }

        public string GetSettings(string Name)
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

        public string SelectedAudioDevice
        {
            get => selectedAudioDevice;
            set
            {
                selectedAudioDevice = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedAudioDevice"));
                }
                settings.SaveSettings("SelectedAudioDevice", value);
            }
        }
        private string selectedAudioDevice { get; set; }

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

        public bool CombineDoubles
        {
            get => ClockWindow.CombineDoubles;
            set
            {
                ClockWindow.CombineDoubles = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("CombineDoubles"));
                }
                settings.SaveSettings("CombineDoubles", value == true ? "1" : "0");
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

        public int VolumeCtrl
        {
            get => volumeCtrl;
            set
            {
                volumeCtrl = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("VolumeCtrl"));
                }
                settings.SaveSettings("VolumeCtrl", VolumeCtrl.ToString());
            }
        }
        private int volumeCtrl { get; set; }

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

        public bool EnableBell
        {
            get => enableBell;
            set
            {
                enableBell = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("EnableBell"));
                }
                settings.SaveSettings("EnableBell", value == true ? "1" : "0");
            }
        }
        private bool enableBell { get; set; }

        public DateTime DomainLastActive
        {
            get => domainLastActive;
            set
            {
                domainLastActive = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DomainLastActive"));
                settings.SaveSettings("DomainLastActive", JsonConvert.SerializeObject(value));
            }
        }

        private DateTime domainLastActive { get; set; }

        private void LoadSettings()
        {
            switch (settings.GetSettings("EnableBell"))
            {
                case "1":
                    EnableBell = true;
                    break;
                case "0":
                    EnableBell = false;
                    break;
                default:
                    EnableBell = false;
                    break;
            }

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

            switch (settings.GetSettings("CombineDoubles"))
            {
                case "1":
                    CombineDoubles = true;
                    break;
                case "0":
                    CombineDoubles = false;
                    break;
                default:
                    CombineDoubles = true;
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

            if (settings.GetSettings("VolumeCtrl").Length > 0)
            {

                VolumeCtrl = int.Parse(settings.GetSettings("VolumeCtrl"));
            }
            else
            {
                VolumeCtrl = 100;
            }

            try
            {
                if (settings.GetSettings("FirstDayDate") == "")
                {
                    FirstDayDate = new DateTime(2020, 2, 10);
                }
                else
                {
                    FirstDayDate = JsonConvert.DeserializeObject<DateTime>(settings.GetSettings("FirstDayDate"));
                }
            }
            catch
            {
                FirstDayDate = new DateTime(2020, 2, 10);
            }

            try
            {
                if (settings.GetSettings("DomainLastActive") == "")
                {
                    DomainLastActive = new DateTime(2020, 2, 10);
                }
                else
                {
                    DomainLastActive = JsonConvert.DeserializeObject<DateTime>(settings.GetSettings("DomainLastActive"));
                }
            }
            catch
            {
                DomainLastActive = new DateTime(2020, 2, 10);
            }

            try
            {
                if (settings.GetSettings("LastEPR") == "")
                {
                    lastEPR = new EPRcollection();
                    lastEPR.Day = 1;
                }
                else{
                    lastEPR = JsonConvert.DeserializeObject<EPRcollection>(settings.GetSettings("LastEPR"));
                }
            }
            catch
            {

            }
            finally
            {
                if (lastEPR == null)
                {
                    lastEPR = new EPRcollection();
                    lastEPR.Day = 1;
                }
            }

            Guid tp = Guid.Empty;
            Guid.TryParse(settings.GetSettings("SelectedAudioDevice"), out tp);
            SelectedAudioDevice = tp.ToString();

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
