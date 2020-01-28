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

namespace MYTGS
{
    public partial class MainWindow
    {
        private void UpdateCalendar(string school)
        {
            WebClient web = new WebClient();
            try
            {
                string ics = web.DownloadString(CalendarUrl);
                Calendar schoolCalendar = Calendar.Load(ics);
                Console.WriteLine("checking uuid");
                string databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");
                using (SQLiteConnection db = new SQLiteConnection(databasePath))
                {
                    db.CreateTable<CalendarEvent>();
                    db.Insert(new CalendarEvent() {
                        DtEnd = schoolCalendar.Events[0].DtEnd.AsDateTimeOffset,
                        DtStart = schoolCalendar.Events[0].DtStart.AsDateTimeOffset,
                        DtStamp = schoolCalendar.Events[0].DtStamp.AsDateTimeOffset,
                        Uid = schoolCalendar.Events[0].Uid,
                        Url = schoolCalendar.Events[0].Url,
                        Name = schoolCalendar.Events[0].Name ,
                        Location = schoolCalendar.Events[0].Location,
                        Status = schoolCalendar.Events[0].Status,


                    });
                }
            }
            catch
            {

            }
        }

    }

    public class CalendarEvent
    {
        [PrimaryKey]
        public string Uid { get; set; }
        public Uri Url { get; set; }
        public string Summary { get; set; }
        public bool UserCreated { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }

        public DateTimeOffset DtEnd { get; set; }
        public DateTimeOffset DtStart { get; set; }
        public DateTimeOffset DtStamp { get; set; }

    }
}


