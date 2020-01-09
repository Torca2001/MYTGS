using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using SQLiteNetExtensions.Extensions;
using Firefly;

namespace MYTGS
{
    public partial class MainWindow 
    {
        private void InitializeEventDB(string school)
        {
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.CreateTable<FFEvent>();
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

    }
}
