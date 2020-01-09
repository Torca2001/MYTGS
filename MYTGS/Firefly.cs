using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml;
using MYTGS;
using NLog;
using System.Windows.Threading;
using HtmlAgilityPack;
using System.Windows.Media.Imaging;

namespace Firefly
{
    class Firefly
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        //Login window
        Login LoginWindow;
        //variables from SSO
        private bool loggedIn = false;
        private string name;
        private string username;
        private string guid;
        private string email;
        private bool canSetTasks;
        //Variables for firefly school
        private string schoolUrl = "";
        private string schoolName;
        private readonly bool _offlineMode = false;
        private bool unauthorised = false;
        public Dictionary<string, FullTask> AllTasks = new Dictionary<string, FullTask>();
        //Event when Logged in
        public event EventHandler OnLogin;
        //Events for read and unread events
        public event EventHandler OnRead;
        public event EventHandler OnUnread;
        //Auth Token for server access
        private string Token;
        //Device ID for access as well
        private readonly string DeviceID = "TT" + Environment.MachineName;
        //Make Variables read only to public
        public bool LoggedIn { get => loggedIn; }
        public string Name { get => name; }
        public string Username { get => username; }
        public string Guid { get => guid; }
        public string Email { get => email;  }
        public bool CanSetTasks { get => canSetTasks; }
        public string SchoolUrl { get => schoolUrl; }
        public string SchoolName { get => schoolName; }
        public bool OfflineMode { get => _offlineMode; }
        public bool Unauthorised { get => unauthorised; }
        //Unread messages list
        public Dictionary<int, List<int>> UnReadList = new Dictionary<int, List<int>>();
        //List of all tasks for logged in user
        public Dictionary<int, FullTask> Tasks = new Dictionary<int, FullTask>();

        //Timer object for task checking
        DispatcherTimer TaskTimer = new DispatcherTimer();

        public Firefly(string school)
        {
            WebClient web = new WebClient();
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.LoadXml(web.DownloadString(@"http://appgateway.ffhost.co.uk/appgateway/school/" + school));
                if (xml.SelectNodes("response").Count == 1 && xml.SelectNodes("response")[0].Attributes["exists"].Value == "true" && xml.SelectNodes("response")[0].Attributes["enabled"].Value == "true")
                {
                    XmlNode response = xml.SelectSingleNode("response");
                    XmlNode addressNode = response.SelectSingleNode("address");
                    if (addressNode == null)
                        throw new Exception("No address given!");
                    schoolName = response.FirstChild.InnerText;
                    if (addressNode.Attributes["ssl"].Value == "true")
                    {
                        schoolUrl = "https://" + addressNode.InnerText;
                    }
                    else
                    {
                        schoolUrl = "http://" + addressNode.InnerText;
                    }
                }
            }
            catch(Exception e)
            {
                logger.Warn(e, "Failed to retrieve Firefly Page. Running in Offline Mode");
                _offlineMode = true;
            }
        }
        
        public bool StatusNetworkCheck()
        {
            if (string.IsNullOrEmpty(Token))
            {
                loggedIn = false;
                return false;
            }

            if (loggedIn)
            {
                SSOResponse resp = SSO(Token);
                if (resp.valid)
                {
                    SSOApply(resp);
                    return true;
                }
                loggedIn = false;
            }
            return false;
        }

        //public void MarkAsRead()
        //{
        //    //data={eventVersionId:164342,recipient:{type:"user",guid:"DB:Cloud:DB:Synergetic:Stu:618262"}}
        //    //_api/1.0/tasks/12054/mark_as_read
        //    //application/x-www-form-urlencoded
        //}

        //file location
        private string Path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MYTGS\Data.dat";

        //Login call returns true if token was sucessfully received
        public void LoginUI()
        {
            if (SchoolUrl == "")
                return;
            if (LoginWindow!=null)
            {
                LoginWindow.Show();
                LoginWindow.Activate();
                return;
            }
            LoginWindow = new Login(schoolUrl + "/login/api/loginui?app_id=android_tasks&device_id=" + DeviceID );
            LoginWindow.OnResult += LoginWindow_OnResult;
            LoginWindow.Show();
        }

        public string EPR()
        {
            if (!loggedIn)
            {
                return null;
            }
            try
            {
                //https://mytgs.fireflycloud.net.au/administration-1/extra-period-roster-epr
                HtmlDocument doc = new HtmlDocument();
                WebClient web = new WebClient();
                doc.LoadHtml(web.DownloadString(SchoolUrl + @"/administration-1/extra-period-roster-epr?ffauth_device_id=" + DeviceID + "&ffauth_secret=" + Token));
                HtmlNode container = doc.GetElementbyId("ffContainer");

                return container.InnerHtml;
            }
            catch(WebException e)
            {
                logger.Warn(e);
                return null;
            }
            catch
            {
                return null;
            }
            
        }

        public bool LoadKey()
        {
            //Make sure user isn't already logged in or invalid file
            if (LoggedIn || !KeyAvailable())
                return false;
            string tmp = File.ReadAllText(Path);
            SSOResponse resp = SSO(tmp);
            if (resp.valid)
            {
                SSOApply(resp);
                Token = tmp;
                loggedIn = true;
                OnLoggedIn(new EventArgs());
                return true;
            }
            return false;
        }

        public bool KeyAvailable()
        {
            if (!File.Exists(Path))
                return false;
            string tmp = File.ReadAllText(Path);
            return tmp.Length > 5;
        }

        private void SaveKey()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MYTGS"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MYTGS");
            }
            using (FileStream fs = new FileStream(Path, FileMode.OpenOrCreate))
            {
                using (TextWriter tw = new StreamWriter(fs, Encoding.UTF8, 1024, true))
                {
                    tw.WriteLine(Token);
                }
                // Set the stream length to the current position in order to truncate leftover text
                fs.SetLength(fs.Position);
            }
            File.SetAttributes(Path, FileAttributes.Hidden);
        }

        public int[] GetAllIds()
        {
            //1970-01-01T00:00:00
            return GetIds(new DateTime(1970,1,1));
        }

        public int[] GetIds(DateTime date)
        {
            //Fail if not logged in
            if (!loggedIn)
            {
                return null;
            }

            try
            {
                WebRequest request = WebRequest.Create(SchoolUrl + @"/api/v2/apps/tasks/ids/filterby?ffauth_device_id=" + DeviceID + "&ffauth_secret=" + Token);
                request.Method = "POST";
                request.ContentType = "application/json; charset=UTF-8";
                string html;
                string strJson = "{\"watermark\":" + JsonConvert.SerializeObject(date, new JsonSerializerSettings{DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"})+"}";
                strJson = "";
                //{ "watermark":"1970-01-01T00:00:00Z"}
                byte[] data = Encoding.ASCII.GetBytes(strJson);
                request.ContentLength = data.Length;
                using (Stream ReqStream = request.GetRequestStream())
                {
                    ReqStream.Write(data, 0, data.Length);
                }
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
                int[] ids;
                ids = JsonConvert.DeserializeObject<int[]>(html);
                return ids;
            }
            catch (Exception e)
            {
                logger.Error(e, "Fetching of Task ID Error");
            }
            return null;

        }

        

        public FullTask[] GetAllTasksByIds(int[] Ids)
        {
            if (!loggedIn)
            {
                return null;
            }

            if (Ids.Length == 0)
                return new FullTask[0];

            if (Ids.Length <= 50)
                return GetFiftyTasksByIds(Ids);
            else
            {
                List<FullTask> AllTasks = new List<FullTask>();
                List<int> tmpList = Ids.ToList<int>();
                foreach (List<int> list in splitList<int>(tmpList,50))
                {
                    FullTask[] tmp = GetFiftyTasksByIds(list.ToArray());
                    if (tmp != null)
                        AllTasks.AddRange(tmp);
                }
                return AllTasks.ToArray();
            }
        }

        public static IEnumerable<List<T>> splitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }


        //Calendar Events
        public FFEvent[] GetEvents(DateTime start, DateTime end)
        {
            if (!loggedIn)
            {
                return null;
            }

            try
            {
                JsonSerializerSettings jsonset = new JsonSerializerSettings
                {
                    DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"
                };
                string strJson ="data=query Query{events(for_guid:\"" + guid + "\",start:" + JsonConvert.SerializeObject(start,jsonset) + ",end:" + JsonConvert.SerializeObject(end, jsonset) + "){guid,description,start,end,location,subject,attendees{principal{guid,name,sort_key,group{guid,name,sort_key,personal_colour}},role}}}";
                //"data=query Query{events(for_guid:\"" + Guid + "\",start: \"2019-04-29T14:00:00Z\",end:\"2019-05-010T15:00:00Z\"){guid,description,start,end,location,subject,attendees{principal{guid,name,sort_key,group{guid,name,sort_key,personal_colour}},role}}}";
                byte[] data = Encoding.ASCII.GetBytes(strJson);
                WebRequest request = WebRequest.Create(SchoolUrl + @"/_api/1.0/graphql?ffauth_device_id=" + DeviceID + "&ffauth_secret=" + Token);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.ContentLength = data.Length;
                string html;
                using (Stream ReqStream = request.GetRequestStream())
                {
                    ReqStream.Write(data, 0, data.Length);
                }
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
                
                FFEvent[] Events;
                Events = JsonConvert.DeserializeObject<Data>(html, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                }).data.events;

                //Iterate through all events
                for (int i = 0; i < Events.Length; i++)
                {
                    //Check if there is a teacher assigned
                    if (Events[i].attendees.Length > 0 && Events[i].attendees[0].role == "Chairperson")
                    {
                        Events[i].Teacher = Events[i].attendees[0].principal.name.Trim();
                    }
                }

                return Events;
            }
            catch(Exception e)
            {
                logger.Error(e, "Fetching of Planner Data Error");
            }
            return null;
        }

        public FullTask UpdateResponses(FullTask task, Response[] responses)
        {
            if (responses == null)
                return task;
            //Just a mess.... EventGuid can be null and there can be multiple null ones
            /*
            Dictionary<string,Response> respList = task.recipientsResponses.Length > 0 ? task.recipientsResponses[0].responses.ToDictionary(x => {
                int z = 0;
                x.eventGuid ?? "0"
            }, x => x) : new Dictionary<string, Response>();
            foreach (Response item in responses) {
                if (respList.ContainsKey(item.eventGuid ?? "0"))
                {
                    //Update response item, this is required as they sometimes change
                    
                    if (respList[item.eventGuid].latestRead != item.latestRead)
                    {
                        if (item.latestRead)
                        {
                            
                        }
                        else
                        {

                        }
                    } 
                    //respList[item.eventGuid] = item;
                }
                else
                {
                    //Add new items to list
                    //respList.Add(item.eventGuid, item);
                }
            }
            */
            return task;
        }

        public BitmapImage GetUserImage()
        {
            if (!OfflineMode && !loggedIn)
            {
                return null;
            }

            try
            {
                // /profilepic.aspx?guid= &size=regular
                using (WebClient webClient = new WebClient())
                {
                    byte[] data = webClient.DownloadData(SchoolUrl + @"/profilepic.aspx?guid=" + Guid + "&size=regular&ffauth_device_id=" + DeviceID + "&ffauth_secret=" + Token);

                    using (MemoryStream mem = new MemoryStream(data))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.StreamSource = mem;
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        img.Freeze();
                        return img;
                    }

                }
            }
            catch
            {
                return null;
            }
        }

        public Response[] GetResponseForID(int ID)
        {
            if (!loggedIn)
            {
                return null;
            }

            try
            {
                //https://mytgs.fireflycloud.net.au/_api/1.0/tasks/11671/responses
                WebClient web = new WebClient();
                string html = web.DownloadString(SchoolUrl + @"/_api/1.0/tasks/" + ID + "/responses?ffauth_device_id=" + DeviceID + "&ffauth_secret=" + Token);
                TmpResp Resp = JsonConvert.DeserializeObject<TmpResp>(html, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
                if (Resp.responses.responses.Length == 0)
                    return new Response[0];
                return Resp.responses.responses[0].ToTaskResponses(Resp.responses.users);
            }
            catch
            {
                return null;
            }
        }

        private FullTask[] GetFiftyTasksByIds(int[] FiftyIds)
        {
            //max task id size is 50 
            if (!loggedIn)
            {
                return null;
            }

            try
            {
                string strIds = "{ \"ids\" : " + JsonConvert.SerializeObject(FiftyIds) + " }";
                byte[] data = Encoding.ASCII.GetBytes(strIds);
                WebRequest request = WebRequest.Create(SchoolUrl + @"/api/v2/apps/tasks/byIds?ffauth_device_id=" + DeviceID + "&ffauth_secret=" + Token);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;
                string html;
                using (Stream ReqStream = request.GetRequestStream())
                {
                    ReqStream.Write(data, 0, data.Length);
                }
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
                FullTask[] Tasks;
                Tasks = JsonConvert.DeserializeObject<FullTask[]>(html, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
                return Tasks;
            }
            catch(Exception e)
            {
                logger.Error(e, "Fetching of Task data error");
                return null;
            }
        }

        /// <summary>
        /// Filters a list of events into ones for the current day
        /// </summary>
        /// <param name="events">Events to filter</param>
        /// <returns>Events for current day</returns>
        static public List<FFEvent> CurrentDayEvents(FFEvent[] events)
        {
            List<FFEvent> results = new List<FFEvent>();
            foreach (FFEvent item in events)
            {
                DateTime startlocal = item.start.ToLocalTime();
                if (startlocal.Day == DateTime.Now.Day && startlocal.Month == DateTime.Now.Month && startlocal.Year == DateTime.Now.Year)
                {
                    results.Add(item);
                }
            }
            return results;
        }



        private void LoginWindow_OnResult(object sender, Login.OnResultEventArgs e)
        {
            //if successful login and double check
            SSOResponse resp = SSO(e.Token);
            if (e.Result && resp.valid)
            {
                //Set variables
                SSOApply(resp);
                loggedIn = true;
                Token = e.Token;
                SaveKey();
                //Trigger Successful login event
                OnLoggedIn(new EventArgs());
            }
        }

        private void SSOApply(SSOResponse response)
        {
            //Apply the result from an SSO response to the obejct
            guid = response.guid;
            username = response.username;
            name = response.name;
            email = response.email;
            canSetTasks = response.canSetTasks;
        }

        private SSOResponse SSO(string key)
        {
            if (SchoolUrl == "")
                return new SSOResponse(false);
            string html;
            //catch unauthorised error
            try
            {
                //Create webrequest for User details
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SchoolUrl + @"/login/api/sso?ffauth_device_id=" + DeviceID + "&ffauth_secret=" + key);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
            }
            //catch webexception errors
            catch(WebException e)
            {
                if (e.Message.StartsWith("The remote name could not be resolved"))
                {
                    logger.Info(e, "SSO No Network Connection");
                    unauthorised = false;
                    return new SSOResponse(false);
                }
                else if (e.Message.StartsWith("The remote server returned an error: (401) Unauthorized"))
                {
                    Console.WriteLine(e.Message);
                    loggedIn = false;
                    logger.Warn(e, "SSO Token Invalid");
                    unauthorised = true;
                }
                return new SSOResponse(false);
            }
            catch
            {
                return new SSOResponse(false);
            }
            //Parse results
            Match result = Regex.Match(html, "identifier=\"(.*)\" username=\"(.*)\" name=\"(.*)\" email=\"(.*)\" canSetTask=\"(.*)\"");
            if (result.Success && result.Groups.Count>3)
            {
                SSOResponse tmp = new SSOResponse(true);
                //Set user GUID
                tmp.guid = result.Groups[1].Value;
                //Set Username aka id name
                tmp.username = result.Groups[2].Value;
                //Set Name aka full name
                tmp.name = result.Groups[3].Value;
                //Set User Email
                tmp.email = result.Groups[4].Value;
                //Check if cansetTasks is true or yes since its returned as a string and needs to be converted to bool
                tmp.canSetTasks = (result.Groups[1].Value.ToLower() == "yes" || result.Groups[1].Value.ToLower() == "true");
                return tmp;
            }
            return new SSOResponse(false);
        }

        //Event to fire when a successfull login has occured;
        protected virtual void OnLoggedIn(EventArgs e)
        {
            EventHandler handler = OnLogin;
            handler?.BeginInvoke(this,e,EndAsyncEvent,null);
        }

        protected virtual void OnReadEvent(ReadArgs e)
        {
            EventHandler handler = OnRead;
            handler?.BeginInvoke(this, e, EndAsyncEvent, null);
        }

        protected virtual void OnUnreadEvent(ReadArgs e)
        {
            EventHandler handler = OnUnread;
            handler?.BeginInvoke(this, e, EndAsyncEvent, null);
        }

        protected virtual void EndAsyncEvent(IAsyncResult iar)
        {
            var ar = (System.Runtime.Remoting.Messaging.AsyncResult)iar;
            var invokedMethod = (EventHandler)ar.AsyncDelegate;
            try
            {
                invokedMethod.EndInvoke(iar);
            }
            catch(Exception e)
            {
                logger.Warn(e, "Event Listener broke");
            }
        }
    }
}
