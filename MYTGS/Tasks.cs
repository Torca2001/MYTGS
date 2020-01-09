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
        private void InitializeTasksDB(string school)
        {
            var databasePath = Path.Combine(Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["AppPath"]), school + ".db");

            using (SQLiteConnection db = new SQLiteConnection(databasePath))
            {
                db.CreateTable<FullTask>();
            }
        }


    }
}
