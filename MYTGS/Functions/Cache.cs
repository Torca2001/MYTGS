using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using Newtonsoft.Json;
using SQLiteNetExtensions.Extensions;
using Firefly;
using System.ComponentModel;

namespace MYTGS
{
    public partial class MainWindow
    {
        public string EPRstring
        {
            get => eprstring;
            set
            {
                eprstring = value;
                SaveCache(CacheSaveLocation, "EPRstring", value);
            }
        }
        private SQLiteConnection CacheSaveLocation;

        private string eprstring { get; set; }

        public string Dashboardstring
        {
            get => dashboardstring;
            set
            {
                dashboardstring = value;
                SaveCache(CacheSaveLocation,"Dashboardstring", value);
            }
        }

        private string dashboardstring { get; set; }

        public DateTime TaskLastFetch
        {
            get => tasklastFetch;
            set
            {
                tasklastFetch = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TaskLastFetch"));
                SaveCache(CacheSaveLocation, "TaskLastFetch", JsonConvert.SerializeObject(value));
            }
        }

        private DateTime tasklastFetch { get; set; }

        private void InitializeCacheDB(SQLiteConnection sqldb)
        {
            CacheSaveLocation = sqldb;
            sqldb.CreateTable<SettingsItem>();
        }

        private void LoadCache(SQLiteConnection sqldb)
        {
            if (GetCache(CacheSaveLocation,"EPRstring") != null)
            {

                EPRstring = GetCache(CacheSaveLocation, "EPRstring");
            }
            else
            {
                EPRstring = "";
            }

            if (GetCache(CacheSaveLocation, "Dashboardstring") != "")
            {
                Dashboardstring = GetCache(CacheSaveLocation, "Dashboardstring");
            }
            else
            {
                Dashboardstring = "";
            }

            try
            {
                if (GetCache(CacheSaveLocation, "TaskLastFetch") == "")
                {
                    tasklastFetch = new DateTime(2001, 7, 7);
                }
                tasklastFetch = JsonConvert.DeserializeObject<DateTime>(GetCache(CacheSaveLocation, "TaskLastFetch"));
            }
            catch
            {
                tasklastFetch = new DateTime(2001, 7, 7);
            }
        }

        private string GetCache(SQLiteConnection sqldb, string Name)
        {
            TableQuery<SettingsItem> results = sqldb.Table<SettingsItem>().Where(s => s.name == Name);
            if (results.Count() > 0)
            {
                return results.First().value;
            }
            else
            {
                return "";
            }
        }

        private void SaveCache(SQLiteConnection sqldb, string Name, string Value)
        {
            SaveCache(sqldb, new SettingsItem(Name, Value));
        }

        private void SaveCache(SQLiteConnection sqldb, SettingsItem item)
        {
            sqldb.InsertOrReplace(item);
        }
    }
}
