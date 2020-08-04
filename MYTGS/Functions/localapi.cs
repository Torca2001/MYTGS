using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Deployment.Application;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Media;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using EmbedIO.Actions;
using Newtonsoft.Json;
using EmbedIO.Utilities;

namespace MYTGS
{
    public partial class MainWindow
    {
        private WebServer LocalapiWebserver = null;

        private int ApiPort = 13693;

        private void initializeLocalApi(string url = "http://localhost:/13693", string origins = "")
        {
            try
            {
                if (LocalapiWebserver != null)
                {
                    LocalapiWebserver.Dispose();
                }

                //CORS prevents malicous sites from accessing localhost
                WebServer server = new WebServer(url).WithCors(origins)
                    .WithModule(new ActionModule("/api/timetable", HttpVerbs.Get, ctx =>
                {
                    if (TwoWeekTimetable == null)
                    {
                        return ctx.SendStringAsync("[]", "application/json", Encoding.UTF8);
                    }
                    return ctx.SendStringAsync(JsonConvert.SerializeObject(TwoWeekTimetable, Formatting.Indented), "application/json", Encoding.UTF8);
                }))
                    .WithModule(new ActionModule("/api/info", HttpVerbs.Get, ctx =>
                {
                //Hide name and id
                if (ApiHideName)
                    {
                        return ctx.SendDataAsync(new { Name = "Anon", Day = CurrentTimetableDay, ID = "000000", ReferenceDay = FirstDayDate });
                    }
                    else
                    {
                        return ctx.SendDataAsync(new { Name = FF.Name, Day = CurrentTimetableDay, ID = FF.Username, ReferenceDay = FirstDayDate });
                    }
                }))
                //    .WithModule(new ActionModule("/api/appinfo", HttpVerbs.Get, ctx =>
                //{
                //    if (ApplicationDeployment.IsNetworkDeployed)
                //    {
                //        return ctx.SendDataAsync(new { version = ApplicationDeployment.CurrentDeployment.CurrentVersion });
                //    }
                //    else
                //    {
                //        return ctx.SendDataAsync(new { version = "Debug" });
                //    }
                //}))
                    .WithModule(new ActionModule("/api/epr", HttpVerbs.Get, ctx =>
                    {
                        List<EPRPeriod> tmp = new List<EPRPeriod>();
                        for (int i = 0; i < EPRChanges.Count; i++)
                        {
                            tmp.Add(new EPRPeriod(EPRChanges[i].period, EPRChanges[i].Classcode, EPRChanges[i].Roomcode, EPRChanges[i].Teacher, EPRChanges[i].TeacherChange, EPRChanges[i].RoomChange));
                        }

                        return ctx.SendDataAsync(tmp);
                    }));
                //Will respond with the entire timetable
                server.Start();
                LocalapiWebserver = server;
            }
            catch (Exception e)
            {
                logger.Error("Local Web Api failed to start!");
                logger.Error(e);
                DisplayMsg("Local Web Api failed to start! reenable to restart!");
            }
        }
    }

}
