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

        public bool GetBoolSettings(string name,bool fallback = false)
        {
            switch (GetSettings(name))
            {
                case "1":
                    return true;
                case "0":
                    return false;
                default:
                    return fallback;
            }
        }

        public int GetIntSettings(string name, int fallback = 0)
        {
            int tempint = fallback;
            if (int.TryParse(GetSettings(name), out tempint) == false){
                return fallback;
            }
            return tempint;
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

        public string XOffset
        {
            get => xOffset;
            set
            {
                xOffset = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("XOffset"));
                }
                settings.SaveSettings("XOffset", value);
            }
        }
        private string xOffset { get; set; }

        public string YOffset
        {
            get => yOffset;
            set
            {
                yOffset = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("YOffset"));
                }
                settings.SaveSettings("YOffset", value);
            }
        }
        private string yOffset { get; set; }

        public bool TablePreference
        {
            get => ClockWindow.TablePositionPreference;
            set
            {
                ClockWindow.TablePositionPreference = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("TablePreference"));
                }
                settings.SaveSettings("TablePreference", value == true ? "1" : "0");
            }
        }
        
        public int ScreenPreference
        {
            get => screenPreference;
            set
            {
                screenPreference = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ScreenPreference"));
                }
                settings.SaveSettings("ScreenPreference", value.ToString());
            }
        }
        private int screenPreference { get; set; }


        public int ClockPlacementMode
        {
            get => clockPlacementMode;
            set
            {
                clockPlacementMode = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ClockPlacementMode"));
                }
                settings.SaveSettings("ClockPlacementMode", value.ToString());
            }
        }
        private int clockPlacementMode { get; set; }

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
                settings.SaveSettings("Offset", value.ToString());
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
                settings.SaveSettings("VolumeCtrl", value.ToString());
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
            EnableBell = settings.GetBoolSettings("EnableBell");
            TablePreference = settings.GetBoolSettings("TablePreference");
            FadeOnHover = settings.GetBoolSettings("FadeOnHover", true);
            HideOnFinish = settings.GetBoolSettings("HideOnFinish", true);
            StartMinimized = settings.GetBoolSettings("StartMinimized", true);
            CombineDoubles = settings.GetBoolSettings("CombineDoubles",true);
            HideOnFullscreen = settings.GetBoolSettings("HideOnFullscreen",true);
            Offset = settings.GetIntSettings("Offset");
            VolumeCtrl = settings.GetIntSettings("VolumeCtrl", 100);

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


            XOffset = settings.GetSettings("XOffset");
            YOffset = settings.GetSettings("YOffset");

            ScreenPreference = settings.GetIntSettings("ScreenPreference",0);
            tmpScreen = ScreenPreference;
            int tmpint = settings.GetIntSettings("ClockPlacementMode");
            if (tmpint >= 0 && tmpint <= 5)
            {
                ClockPlacementMode = tmpint;
            }
            else
            {
                ClockPlacementMode = 0;
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
