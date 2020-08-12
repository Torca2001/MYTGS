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
                return EarlyFinishes.Where(p => p.DtStart.DateTime >= DateTime.UtcNow.AddDays(-1)).OrderBy(p => p.DtStart).ToArray();
            }
                
        }

        private void InitializeCalendarDB(SQLiteConnection sqldb)
        {

            sqldb.CreateTable<CalendarEvent>();
            sqldb.CreateTable<EarlyFinishEvent>();
        }

        private void UpdateCalendar(SQLiteConnection sqldb)
        {
            WebClient web = new WebClient();
            try
            {
                string ics = web.DownloadString(CalendarUrl);
                Calendar schoolCalendar = Calendar.Load(ics);
                foreach (Ical.Net.CalendarComponents.CalendarEvent item in schoolCalendar.Events)
                {
                    try
                    {
                        sqldb.InsertOrReplace(new CalendarEvent()
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
            catch(Exception e)
            {
                logger.Warn("Unable to convert Calendar");
                logger.Warn(e);
            }
        }


        private void UserEarlyFinishEvent(SQLiteConnection sqldb, bool flag)
        {
            UserEarlyFinishEvent(sqldb, flag, DateTime.Now);
        }

        private void UserEarlyFinishEvent(SQLiteConnection sqldb, bool flag, DateTime date)
        {
            try
            {
                sqldb.InsertOrReplace(new EarlyFinishEvent
                {
                    Date = date.ToShortDateString(),
                    OverrideEarlyFinishto = flag
                });
            }
            catch
            {
                logger.Warn("Failed to modify User Early Finish Event " + date.Year + " " + date.Month + " " + date.Day);
            }
        }

        private void RemoveEarlyFinish(SQLiteConnection sqldb, DateTime date)
        {
            try
            {
                string datestr = date.ToShortDateString();
                var result = sqldb.Table<EarlyFinishEvent>().Where(p => p.Date == datestr);
                sqldb.DeleteAll(result);
            }
            catch
            {
                logger.Warn("Failed to delete early finish events!");
            }
        }

        private bool? UserScheduledEarlyFinish(SQLiteConnection sqldb)
        {
            //User calendar takes priority
            string comp = DateTime.Now.ToShortDateString();
            var result = sqldb.Table<EarlyFinishEvent>().Where(p => p.Date == comp).ToArray();
            if (result.Count() > 0)
            {
                return result.First().OverrideEarlyFinishto;
            }

            return null;
        }

        private bool IsTodayEarlyFinish(SQLiteConnection sqldb, bool autocalendar = true)
        {
            //User calendar takes priority
            string comp = DateTime.Now.ToShortDateString();
            var result = sqldb.Table<EarlyFinishEvent>().Where(p => p.Date == comp).ToArray();
            if (result.Count() > 0)
            {
                return result.First().OverrideEarlyFinishto;
            }

            //No need to check outlook calendar
            if (autocalendar == false)
            {
                return false;
            }

            //Check outlook calendar last as user takes priority
            foreach (CalendarEvent item in CurrentEarlyFinishes)
            {
                if (item.DtStart.DateTime.ToLocalTime().ToShortDateString() == DateTime.Now.ToShortDateString())
                {
                    TodayEarlyFinish = true;
                    return true;
                }
            }
            TodayEarlyFinish = false;
            return false;
        }

        private void CheckForEarlyFinishes(SQLiteConnection sqldb)
        {

            EarlyFinishes.Clear();
            foreach (CalendarEvent item in sqldb.Table<CalendarEvent>())
            {
                if (item.Summary.ToLower().Trim().Contains("early finish"))
                {
                    EarlyFinishes.Add(item);
                }
            }
            if (ClockWindow != null)
                ClockWindow.CurrentEarlyFinishes = CurrentEarlyFinishes;
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentEarlyFinishes"));
        }


    }

    public class EarlyFinishEvent
    {
        [PrimaryKey]
        public string Date { get; set; }
        [NotNull]
        public bool OverrideEarlyFinishto { get; set; }
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


