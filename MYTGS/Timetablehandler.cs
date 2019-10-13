using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MYTGS
{
    class Timetablehandler
    {
        //Unique times for each period that works for all types of days
        static public readonly TimeSpan[] times = new TimeSpan[7]
        {
            new TimeSpan(8,30,0), //Period 0
            new TimeSpan(8,55,0), //Period 1
            new TimeSpan(9,50,0), //Period 2
            new TimeSpan(11,0,0), //Period 3
            new TimeSpan(12,0,0), //Period 4
            new TimeSpan(13,40,0),//Period 5
            new TimeSpan(14,30,0) //Period 6
        };

        static public readonly BreakPeriod[] RecessPeriods = new BreakPeriod[3]
        {
            new BreakPeriod(new TimeSpan(10,35,0),new TimeSpan(10,50,0),"Recess"),
            new BreakPeriod(new TimeSpan(10,5,0),new TimeSpan(10,20,0),"Recess"),
            new BreakPeriod(new TimeSpan(10,25,0),new TimeSpan(10,40,0),"Recess")
        };

        static public readonly BreakPeriod[] LunchPeriods = new BreakPeriod[3]
        {
            new BreakPeriod(new TimeSpan(12,40,0),new TimeSpan(13,25,0),"Lunch"),
            new BreakPeriod(new TimeSpan(12,10,0),new TimeSpan(13,25,0),"Long Lunch"),
            new BreakPeriod(new TimeSpan(12,20,0),new TimeSpan(12,55,0),"Short Lunch")
        };


        //Times for the periods - 0 normal day times -- 1 -- Wednesday times -- 2 -- Early finish timetable
        static public readonly BreakPeriod[,] DefaultPeriods = new BreakPeriod[7,3]
        {
            { new BreakPeriod(new TimeSpan(8,15,0),new TimeSpan(8,45,0),"Form"), new BreakPeriod(new TimeSpan(8,15,0),new TimeSpan(8,14,0),"No Form"), new BreakPeriod(new TimeSpan(8,15,0),new TimeSpan(8,45,0),"Form") },
            { new BreakPeriod(new TimeSpan(8,50,0),new TimeSpan(9,40,0),"Period 1"), new BreakPeriod(new TimeSpan(8,20,0),new TimeSpan(9,10,0),"Period 1"), new BreakPeriod(new TimeSpan(8,50,0),new TimeSpan(9,35,0),"Period 1") },
            { new BreakPeriod(new TimeSpan(9,45,0),new TimeSpan(10,35,0),"Period 2"), new BreakPeriod(new TimeSpan(9,15,0),new TimeSpan(10,5,0),"Period 2"), new BreakPeriod(new TimeSpan(9,40,0),new TimeSpan(10,25,0),"Period 2") },
            { new BreakPeriod(new TimeSpan(10,55,0),new TimeSpan(11,45,0),"Period 3"), new BreakPeriod(new TimeSpan(10,25,0),new TimeSpan(11,15,0),"Period 3"), new BreakPeriod(new TimeSpan(10,45,0),new TimeSpan(11,30,0),"Period 3") },
            { new BreakPeriod(new TimeSpan(11,50,0),new TimeSpan(12,40,0),"Period 4"), new BreakPeriod(new TimeSpan(11,20,0),new TimeSpan(12,10,0),"Period 4"), new BreakPeriod(new TimeSpan(11,35,0),new TimeSpan(12,20,0),"Period 4") },
            { new BreakPeriod(new TimeSpan(13,30,0),new TimeSpan(14,20,0),"Period 5"), new BreakPeriod(new TimeSpan(13,30,0),new TimeSpan(14,20,0),"Period 5"), new BreakPeriod(new TimeSpan(13,00,0),new TimeSpan(13,45,0),"Period 5") },
            { new BreakPeriod(new TimeSpan(14,25,0),new TimeSpan(15,15,0),"Period 6"), new BreakPeriod(new TimeSpan(14,25,0),new TimeSpan(15,15,0),"Period 6"), new BreakPeriod(new TimeSpan(13,50,0),new TimeSpan(14,35,0),"Period 6") }
        };



        //Fix missing periods
        static public TimetablePeriod[] FillInTable(TimetablePeriod[] periods, DateTime day, bool Earlyfinish)
        {
            if (periods.Length < 7)
            {
                throw new NotImplementedException();
            }

            int position = 0;
            if (Earlyfinish)
            {
                position = 2;
            }
            else if(day.DayOfWeek == DayOfWeek.Wednesday)
            {
                position = 1;
            }

            for (int i = 0; i < 7; i++)
            {
                if (string.IsNullOrWhiteSpace(periods[i].Classcode)){
                    TimetablePeriod tmp = new TimetablePeriod
                    {
                        Start = DateTimespan(day, DefaultPeriods[i, position].Start),
                        End = DateTimespan(day, DefaultPeriods[i, position].End),
                        Classcode = DefaultPeriods[i, position].description.Contains("period") ? "P" + DefaultPeriods[i, position].description.Last() : DefaultPeriods[i, position].description,
                        Description = DefaultPeriods[i, position].description,
                        period = i,
                        Roomcode = "",
                        GotoPeriod = DefaultPeriods[i, position].description == "No Form" ? false : true
                    };
                    periods[i] = tmp;
                }
            }

            return periods;
        }



        static public List<TimetablePeriod> ProcessForUse(Firefly.FFEvent[] events, DateTime day, bool EarlyFinish, bool EventsUptoDate)
        {
            //If events not up to date then assume school day
            //If event is up to date and no events for the day Assume holiday

            List<TimetablePeriod> Modified = new List<TimetablePeriod>();
            TimetablePeriod[] periods = ParseEventsToPeriods(EventsForDay(events, day));
            
            for (int i = 0; i < 7; i++)
            {
                if (!string.IsNullOrWhiteSpace(periods[i].Roomcode))
                {
                    //Add if it is a valid period
                    Modified.Add(periods[i]);
                }
            }

            int position = 0;
            if (EarlyFinish)
            {
                position = 2;
            }
            else if (day.DayOfWeek == DayOfWeek.Wednesday)
            {
                position = 1;
            }

            if (day.DayOfWeek == DayOfWeek.Sunday || day.DayOfWeek == DayOfWeek.Saturday)
            {
                //Don't fill in table only check for overlaps
                periods = OverlapCheck(Modified.ToArray());
                return periods.ToList();
            }
            else
            {
                //Fill in table with normal periods if not weekend
                if (!EventsUptoDate)
                {
                    periods = FillInTable(periods, day, EarlyFinish);
                    Array.Resize(ref periods, 9);
                    periods[7] = new TimetablePeriod(DateTimespan(day, RecessPeriods[position].Start), DateTimespan(day, RecessPeriods[position].End), RecessPeriods[position].description, "Break", "", false, 7);
                    periods[8] = new TimetablePeriod(DateTimespan(day, LunchPeriods[position].Start), DateTimespan(day, LunchPeriods[position].End), RecessPeriods[position].description, "Break", "", false, 8);
                    periods = OverlapCheck(periods);
                    return periods.ToList();
                }
                else
                {
                    if (Modified.Count == 0)
                    {
                        //Don't add breaks assume day off/holiday
                        return Modified;
                    }
                    periods = FillInTable(periods, day, EarlyFinish);
                    Array.Resize(ref periods, 9);
                    periods[7] = new TimetablePeriod(DateTimespan(day, RecessPeriods[position].Start), DateTimespan(day, RecessPeriods[position].End), RecessPeriods[position].description, "Break", "", false, 7);
                    periods[8] = new TimetablePeriod(DateTimespan(day, LunchPeriods[position].Start), DateTimespan(day, LunchPeriods[position].End), RecessPeriods[position].description, "Break", "", false, 8);
                    periods = OverlapCheck(periods);
                    return periods.ToList();
                }
            }
            
        }

        static public TimetablePeriod[] OverlapCheck(TimetablePeriod[] periods)
        {
            periods = periods.OrderBy(o => o.Start).ToArray();
            for (int i = 0; i < periods.Length-1; i++)
            {
                if (periods[i + 1].GotoPeriod)
                {
                    DateTime tmp = periods[i + 1].Start.AddMinutes(-5);
                    if (CompareInBetweenExclusive(periods[i].Start, periods[i].End, tmp))
                    {
                        periods[i].End = tmp;
                    }
                }
            }
            return periods;
        }

        private static DateTime DateTimespan(DateTime dd, TimeSpan tt)
        {
            dd = dd.ToLocalTime();
            dd = dd.AddHours(tt.Hours - dd.Hour);
            dd = dd.AddMinutes(tt.Minutes - dd.Minute);
            dd = dd.AddSeconds(tt.Seconds - dd.Second);
            dd = dd.AddMilliseconds(-dd.Millisecond);
            return dd.ToUniversalTime();
        }

        //Create period table for the day



        /// <summary>
        /// Converts the list of events for the day into their respective periods
        /// </summary>
        /// <param name="events">List of Events</param>
        /// <returns>Array of the periods between 0-6</returns>
        static public TimetablePeriod[] ParseEventsToPeriods(Firefly.FFEvent[] events)
        {
            Regex reg = new Regex(@"(\w*?)-(.)-(\d)-([1-3]?\d)-([1-3]?\d)-(\d{4})");
            TimetablePeriod[] table = new TimetablePeriod[7];
            foreach (Firefly.FFEvent item in events)
            {
                try
                {

                    Match match = reg.Match(item.guid);
                    if (match.Success)
                    {
                        table[Convert.ToInt16(match.Groups[3].Value)] = new TimetablePeriod(item.start, item.end, item.subject, match.Groups[1].ToString(), item.location, true, Convert.ToInt16(match.Groups[3].Value));
                    }
                }
                catch
                {
                    //Regex error
                }
            }

            return table;
        }

        static public Firefly.FFEvent[] FilterTodayOnly(Firefly.FFEvent[] events)
        {
            return EventsForDay(events, DateTime.UtcNow);
        }

        static public Firefly.FFEvent[] EventsForDay(Firefly.FFEvent[] events, DateTime date)
        {
            DateTime localdate = date.ToLocalTime();
            List<Firefly.FFEvent> Events = new List<Firefly.FFEvent>();
            foreach (Firefly.FFEvent item in events)
            {
                DateTime localstart = item.start.ToLocalTime();
                if (localstart.Day == localdate.Day && localstart.Month == localdate.Month && localstart.Year == localdate.Year)
                {
                    Events.Add(item);
                }
            }

            return Events.ToArray();
        }

        static public bool CompareInBetween(DateTime start, DateTime end, TimeSpan Inbetween)
        {
            DateTime tim = DateTime.Now;
            tim.AddHours(Inbetween.Hours - tim.Hour);
            tim.AddMinutes(Inbetween.Minutes - tim.Minute);
            tim.AddSeconds(Inbetween.Seconds - tim.Second);
            return CompareInBetween(start, end, tim);
        }

        static public bool CompareInBetween(DateTime start, DateTime end, DateTime Inbetween)
        {
            return (DateTime.Compare(Inbetween, start) >= 0 && DateTime.Compare(Inbetween, end) <= 0);
        }

        static public bool CompareInBetweenExclusive(DateTime start, DateTime end, DateTime Inbetween)
        {
            return (DateTime.Compare(Inbetween, start) > 0 && DateTime.Compare(Inbetween, end) < 0);
        }

    }

    public struct TimetablePeriod
    {
        public DateTime Start;
        public DateTime End;
        public string Description;
        public string Classcode;
        public string Roomcode;
        public bool GotoPeriod;
        public int period;

        public TimetablePeriod(DateTime start, DateTime end, string description, string classcode, string roomcode, bool gotoPeriod, int period)
        {
            Start = start;
            End = end;
            Description = description;
            Classcode = classcode;
            Roomcode = roomcode;
            GotoPeriod = gotoPeriod;
            this.period = period;
        }

    }

    public struct BreakPeriod
    {
        public TimeSpan Start;
        public TimeSpan End;
        public string description;

        public BreakPeriod(TimeSpan start, TimeSpan end, string description)
        {
            Start = start;
            End = end;
            this.description = description;
        }
    }
}
