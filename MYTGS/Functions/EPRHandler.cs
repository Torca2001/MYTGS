using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Configuration;

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

            
            //Debug function to only run during debug mode
            //if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed == false)
            //{
            //    Console.WriteLine("EPR Debug mode on!");
            //    EPR.Date = DateTime.Now;
            //    EPR.Day = DateTime.Now.DayOfWeek == DayOfWeek.Monday ? 2 : 0;
            //    TimetablePeriod tt = new TimetablePeriod();
            //    tt.TeacherChange = true;
            //    tt.RoomChange = true;
            //    tt.Teacher = "New Teacher";
            //    tt.Roomcode = "3TEST3";
            //    for (int i = 0; i < 7; i++)
            //    {
            //        EPR.Changes.Add("122MM4-"+i, tt);
            //    }
            //    tt.Teacher = "New Teacher2";
            //    tt.Roomcode = "5TEST5";
            //    for (int i = 0; i < 7; i++)
            //    {
            //        EPR.Changes.Add("122MS1-"+i, tt);
            //    }
            //    return EPR;
            //}

            bool ErrorsParsing = false;
            try
            {
                EPRstr = Regex.Replace(EPRstr, @"\t|\n|\r", "");
                Match header = Regex.Match(EPRstr, @"day\s*?([0-9]0?)[\s:]*(\d{1,2})\s*\/\s*(\d{1,2})\s*\/\s*(\d{4,})", RegexOptions.IgnoreCase);
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

                Match RoomChangeTable = Regex.Match(EPRstr, @"room\s{0,4}changes(?:.|\n)*?<tbody>((?:.|\n)*?)<\/tbody>", RegexOptions.IgnoreCase);
                if (RoomChangeTable.Success)
                {
                    MatchCollection Rows = Regex.Matches(RoomChangeTable.Groups[1].Value, @"<tr>([\s\S]*?)<\/tr>");

                    MatchCollection columnfirst = Regex.Matches(Rows[0].Value, @"<[^\/]*>\s*([^<>]+?)\s*<\/", RegexOptions.IgnoreCase);
                    int PeriodColoumn = 0;
                    int ClassColoumn = 2;
                    int TeacherColoumn = 3;
                    int RoomcodeColoumn = 5;
                    for (int i = 0; i < columnfirst.Count; i++)
                    {
                        if (columnfirst[i].Groups[1].Value.ToLower().Contains("period"))
                        {
                            PeriodColoumn = i;
                        }
                        else if (columnfirst[i].Groups[1].Value.Trim().ToLower().StartsWith("class"))
                        {
                            ClassColoumn = i;
                        }
                        else if (columnfirst[i].Groups[1].Value.Trim().ToLower().StartsWith("teacher"))
                        {
                            TeacherColoumn = i;
                        }
                        else if (columnfirst[i].Groups[1].Value.ToLower().Contains("new room"))
                        {
                            RoomcodeColoumn = i;
                        }
                    }

                    //Ignore first row since its just the headers
                    for (int i = 1; i < Rows.Count; i++)
                    {
                        try
                        {
                            //There shouldn't ever be clashes on first run
                            MatchCollection columns = Regex.Matches(Rows[i].Value, @"<[^\/]*>\s*([^<>]+?)\s*<\/", RegexOptions.IgnoreCase);
                            if (string.IsNullOrWhiteSpace(columns[ClassColoumn].Groups[1].Value) || EPR.Changes.ContainsKey(columns[ClassColoumn].Groups[1].Value + "-" + Convert.ToInt32(columns[PeriodColoumn].Groups[1].Value)))
                                continue;
                            TimetablePeriod period = new TimetablePeriod()
                            {
                                period = Convert.ToInt32(columns[PeriodColoumn].Groups[1].Value),
                                Classcode = columns[ClassColoumn].Groups[1].Value,
                                Teacher = columns[TeacherColoumn].Groups[1].Value,
                                Roomcode = columns[RoomcodeColoumn].Groups[1].Value,
                                RoomChange = true
                            };
                            EPR.Changes.Add(period.Classcode + "-" + period.period, period);
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

                    //Will make coloum title to its respective coloumns
                    MatchCollection columnfirst = Regex.Matches(Rows[0].Value, @"<[^\/]*>\s*([^<>]+?)\s*<\/", RegexOptions.IgnoreCase);
                    int PeriodColoumn = 0;
                    int ClassColoumn = 2;
                    int TeacherColoumn = 5;
                    int RoomcodeColoumn = 1;
                    for (int i = 0; i < columnfirst.Count; i++)
                    {
                        if (columnfirst[i].Groups[1].Value.ToLower().Contains("period"))
                        {
                            PeriodColoumn = i;
                        }
                        else if (columnfirst[i].Groups[1].Value.Trim().ToLower().StartsWith("class"))
                        {
                            ClassColoumn = i;
                        }
                        else if (columnfirst[i].Groups[1].Value.Trim().ToLower().StartsWith("replacement teacher"))
                        {
                            TeacherColoumn = i;
                        }
                        else if (columnfirst[i].Groups[1].Value.ToLower().Contains("room"))
                        {
                            RoomcodeColoumn = i;
                        }
                    }

                    //Ignore first row since its just the headers
                    for (int i = 1; i < Rows.Count; i++)
                    {
                        try
                        {
                            MatchCollection columns = Regex.Matches(Rows[i].Value, @"<[^\/]*>\s*([^<>]+?)\s*<\/", RegexOptions.IgnoreCase);
                            if (string.IsNullOrWhiteSpace(columns[ClassColoumn].Groups[1].Value))
                                continue;
                            int PeriodTmp = Convert.ToInt16(columns[PeriodColoumn].Groups[1].Value);
                            if (EPR.Changes.ContainsKey(columns[ClassColoumn].Groups[1].Value + "-" + PeriodTmp))
                            {
                                TimetablePeriod roomchangeditem = EPR.Changes[columns[ClassColoumn].Groups[1].Value + "-" + PeriodTmp];
                                roomchangeditem.Teacher = columns[TeacherColoumn].Groups[1].Value;
                                roomchangeditem.TeacherChange = true;
                                EPR.Changes[columns[ClassColoumn].Groups[1].Value + "-" + PeriodTmp] = roomchangeditem;
                            }
                            else
                            {
                                TimetablePeriod period = new TimetablePeriod()
                                {
                                    period = PeriodTmp,
                                    Classcode = columns[ClassColoumn].Groups[1].Value,
                                    Teacher = columns[TeacherColoumn].Groups[1].Value,
                                    Roomcode = columns[RoomcodeColoumn].Groups[1].Value,
                                    TeacherChange = true
                                };
                                EPR.Changes.Add(period.Classcode + "-" + period.period, period);
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

    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class EPRcollection
    {
        public DateTime Date;
        public int Day;
        public Dictionary<string, TimetablePeriod> Changes;
        public bool Errors; //Indicate if there were errors doing the parsing of this collection

        public EPRcollection(DateTime date, Dictionary<string, TimetablePeriod> changes, int day, bool errors = false)
        {
            Date = date;
            Changes = changes;
            Day = day;
            Errors = errors;
        }

        public EPRcollection()
        {
            Date = new DateTime();
            Changes = new Dictionary<string, TimetablePeriod>();
            Day = 0;
            Errors = false;
        }
    }

    public struct EPRPeriod
    {
        public int Period;
        public string ClassCode;
        public string RoomCode;
        public string Teacher;
        public bool TeacherChange;
        public bool RoomChange;

        public EPRPeriod(int period, string classCode, string roomCode, string teacher, bool teacherChange, bool roomChange)
        {
            Period = period;
            ClassCode = classCode;
            RoomCode = roomCode;
            Teacher = teacher;
            TeacherChange = teacherChange;
            RoomChange = roomChange;
        }
    }
}
