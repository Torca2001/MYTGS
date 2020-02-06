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

namespace MYTGS
{
    public partial class MainWindow
    {
        public List<CalendarEvent> EarlyFinishes { get; set; } = new List<CalendarEvent>(); 
        public CalendarEvent[] CurrentEarlyFinishes {
            get
            {
                return EarlyFinishes.Where(p => p.DtStart <= DateTime.UtcNow.AddDays(1)).OrderBy(p => p.DtStart).ToArray();
            }
                
        }

        private void InitializeCalendarDB(string school)
        {
            string databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.CreateTable<CalendarEvent>();
            }
        }

        private void UpdateCalendar(string school)
        {
            WebClient web = new WebClient();
            try
            {
                string ics = web.DownloadString(CalendarUrl);
                Calendar schoolCalendar = Calendar.Load(ics);
                string databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");
                using (SQLiteConnection db = new SQLiteConnection(databasePath))
                {
                    foreach (Ical.Net.CalendarComponents.CalendarEvent item in schoolCalendar.Events)
                    {
                        try
                        {
                            db.InsertOrReplace(new CalendarEvent()
                            {
                                DtEnd = item.DtEnd.AsDateTimeOffset,
                                DtStart = item.DtStart.AsDateTimeOffset,
                                DtStamp = item.DtStamp.AsDateTimeOffset,
                                Uid = item.Uid,
                                //Url = item.Url,
                                Name = item.Name,
                                Location = item.Location,
                                Summary = item.Summary,
                                Status = item.Status,
                                UserCreated = false,
                                Description = item.Description

                            });
                        }
                        catch
                        {
                            logger.Warn("Calendar Event failed to parse UID: " + item.Uid);
                        }
                    }
                }
            }
            catch
            {
                logger.Warn("Unable to convert Calendar");
            }
        }

        private void CheckForEarlyFinishes(string school)
        {
            string databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");
            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                EarlyFinishes.Clear();
                foreach (CalendarEvent item in db.Table<CalendarEvent>())
                {
                    if (item.Summary.ToLower().Trim().StartsWith("early finish"))
                    {
                        EarlyFinishes.Add(item);
                    }
                }
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentEarlyFinishes"));
            }
        }


    }

    public class CalendarEvent
    {
        [PrimaryKey]
        public string Uid { get; set; }
        //public Uri Url { get; set; }
        public string Summary { get; set; }
        public bool UserCreated { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }

        public DateTimeOffset DtEnd { get; set; }
        public DateTimeOffset DtStart { get; set; }
        public DateTimeOffset DtStamp { get; set; }

    }
}


