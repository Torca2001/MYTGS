using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MYTGS
{
    class EPRHandler
    {
        

        public static EPRcollection ProcessEPR(string EPRstr)
        {
            //Make engine ignore case sensitivity
            //day\s*([0 - 9]0 ?)[\s:]*(\d{1,2})\s*\/\s*(\d{1,2})\s*\/\s*(\d{4,})  //Bullentin day parsing - Bulletin Day 9: 27/9/2019
            //room\s{0,4}(? i) changes(?:.|\n)*?<tbody>((?:.|\n)*?)<\/tbody> //Locates the table for room changes
            //replacement\s{0,4}(?i)teachers(?:.|\n)*?<tbody.*?>((?:.|\n)*?)<\/tbody.*?> // Locates the teacher replacement table
            //(<tr>[\s\S]*?<\/tr>) //Get the row 
            //<[^\/]*>\s*([\s\S]*?)\s*?<\/ //Gets the content of each coloumn excluding the html only getting innner most html
            EPRcollection EPR = new EPRcollection(new DateTime(), new Dictionary<string, TimetablePeriod>(), 0);
            bool ErrorsParsing = false;
            try
            {
                EPRstr = Regex.Replace(EPRstr, @"\t|\n|\r", "");
                Match header = Regex.Match(EPRstr, @"day\s*([0-9]0?)[\s:]*(\d{1,2})\s*\/\s*(\d{1,2})\s*\/\s*(\d{4,})", RegexOptions.IgnoreCase);
                if (!header.Success)
                {
                    throw new Exception("No Header located");
                }
                EPR.Day = Convert.ToInt16(header.Groups[1].Value); //Day of EPR
                if (EPR.Day > 10)
                {
                    throw new Exception("Header day exceeded range");
                }

                EPR.Date = new DateTime(Convert.ToInt16(header.Groups[4].Value), Convert.ToInt16(header.Groups[3].Value), Convert.ToInt16(header.Groups[2].Value)); //Date of EPR
                EPR.Date = EPR.Date.ToUniversalTime(); //Convert to UTC 

                Match RoomChangeTable = Regex.Match(EPRstr, @"room\s{0,4}changes(?:.|\n)*?<tbody>((?:.|\n)*?)<\/tbody>", RegexOptions.IgnoreCase);
                if (RoomChangeTable.Success)
                {
                    MatchCollection Rows = Regex.Matches(RoomChangeTable.Groups[1].Value, @"<tr>([\s\S]*?)<\/tr>");
                    //Ignore first row since its just the headers
                    for (int i = 1; i < Rows.Count; i++)
                    {
                        try
                        {
                            MatchCollection columns = Regex.Matches(Rows[i].Value, @"<[^\/]*>\s*([\s\S]*?)\s*?<\/", RegexOptions.IgnoreCase);
                            if (string.IsNullOrWhiteSpace(columns[2].Groups[1].Value))
                                continue;
                            TimetablePeriod period = new TimetablePeriod()
                            {
                                period = Convert.ToInt32(columns[0].Groups[1].Value),
                                Classcode = columns[2].Groups[1].Value,
                                Teacher = columns[3].Groups[1].Value,
                                Roomcode = columns[5].Groups[1].Value
                            };
                            EPR.Changes.Add(period.Classcode, period);
                        }
                        catch
                        {
                            ErrorsParsing = true;
                        }
                    }
                }

                Match TeacherChangeTable = Regex.Match(EPRstr, @"replacement\s{0,4}(?i)teachers(?:.|\n)*?<tbody.*?>((?:.|\n)*?)<\/tbody.*?>", RegexOptions.IgnoreCase);
                if (TeacherChangeTable.Success)
                {
                    MatchCollection Rows = Regex.Matches(TeacherChangeTable.Groups[1].Value, @"<tr>([\s\S]*?)<\/tr>");
                    //Ignore first row since its just the headers
                    for (int i = 1; i < Rows.Count; i++)
                    {
                        try
                        {
                            MatchCollection columns = Regex.Matches(Rows[i].Value, @"<[^\/]*>\s*([\s\S]*?)\s*?<\/", RegexOptions.IgnoreCase);
                            if (string.IsNullOrWhiteSpace(columns[2].Groups[1].Value))
                                continue;
                            if (EPR.Changes.ContainsKey(columns[2].Groups[1].Value))
                            {
                                TimetablePeriod roomchangeditem = EPR.Changes[columns[2].Groups[1].Value];
                                roomchangeditem.Teacher = columns[5].Groups[1].Value;
                                EPR.Changes[columns[2].Groups[1].Value] = roomchangeditem;
                            }
                            else
                            {
                                TimetablePeriod period = new TimetablePeriod()
                                {
                                    period = Convert.ToInt16(columns[0].Groups[1].Value),
                                    Classcode = columns[2].Groups[1].Value,
                                    Teacher = columns[5].Groups[1].Value,
                                    Roomcode = columns[1].Groups[1].Value
                                };
                                EPR.Changes.Add(period.Classcode, period);
                            }
                        }
                        catch
                        {
                            ErrorsParsing = true;
                        }
                    }
                }
                EPR.Errors = ErrorsParsing;
                return EPR;
            }
            catch
            {
                //Throw generic exception
                throw new Exception();
            }


        }
    }

    struct EPRcollection
    {
        public DateTime Date;
        public int Day;
        public Dictionary<string, TimetablePeriod> Changes;
        public bool Errors; //Indicate if there were errors doing the parsing of this collection

        public EPRcollection(DateTime date, Dictionary<string, TimetablePeriod> changes, int day)
        {
            Date = date;
            Changes = changes;
            Day = day;
            Errors = false;
        }
    }
}
