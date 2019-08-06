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

namespace Firefly
{
    class Firefly
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        //Login window
        Login LoginWindow;
        //variables from SSO
        private bool loggedIn;
        private string name;
        private string username;
        private string guid;
        private string email;
        private bool canSetTasks;
        //Variables for firefly school
        private string schoolUrl = "";
        private string schoolName;
        public Dictionary<string, FullTask> AllTasks = new Dictionary<string, FullTask>();
        //Event when Logged in
        public event EventHandler OnLogin;
        //Events for read and unread events
        public event EventHandler OnRead;
        public event EventHandler OnUnread;
        //Auth Token for server access
        private string Token;
        //Device ID for access as well
        private string DeviceID = "TT" + Environment.MachineName;
        //Make Variables read only to public
        public bool LoggedIn { get => loggedIn; }
        public string Name { get => name; }
        public string Username { get => username; }
        public string Guid { get => guid; }
        public string Email { get => email;  }
        public bool CanSetTasks { get => canSetTasks; }
        public string SchoolUrl { get => schoolUrl; }
        public string SchoolName { get => schoolName; }
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
                logger.Warn(e, "Firefly Object initialization error");
            }
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
            Console.WriteLine(schoolUrl + "/login/api/loginui?app_id=android_tasks&device_id=" + schoolUrl);
            LoginWindow = new Login(schoolUrl + "/login/api/loginui?app_id=android_tasks&device_id=" + DeviceID );
            LoginWindow.Show();
            LoginWindow.OnResult += LoginWindow_OnResult;
        }

        public string EPR()
        {
            //https://mytgs.fireflycloud.net.au/administration-1/extra-period-roster-epr
            HtmlDocument doc = new HtmlDocument();
            WebClient web = new WebClient();
            doc.LoadHtml(web.DownloadString(SchoolUrl + @"/administration-1/extra-period-roster-epr?ffauth_device_id=" + DeviceID + "&ffauth_secret=" + Token));
            HtmlNode container = doc.GetElementbyId("ffContainer");

            return container.InnerHtml; 
        }

        public bool LoadKey()
        {
            //Make sure user isn't already logged in or invalid file
            if (LoggedIn || !File.Exists(Path))
                return false;
            string tmp = File.ReadAllText(Path);
            if (SSO(tmp))
            {
                Token = tmp;
                loggedIn = true;
                OnLoggedIn(new EventArgs());
                return true;
            }
            return false;
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
                return new int[0];
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
            return new int[0];
        }

        

        public FullTask[] GetAllTasksByIds(int[] Ids)
        {
            if (Ids.Length == 0)
                return new FullTask[0];
            if (Ids.Length <= 50)
                return GetFiftyTasksByIds(Ids);
            else
            {
                int count = 0;
                List<FullTask> AllTasks = new List<FullTask>();
                List<int> tmpList = Ids.ToList<int>();
                foreach (List<int> list in splitList<int>(tmpList,50))
                {
                    AllTasks.AddRange(GetFiftyTasksByIds(list.ToArray()));
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

        public FFEvent[] GetEvents(DateTime start, DateTime end)
        {
            if (!loggedIn)
                return new FFEvent[0];
            try
            {
            JsonSerializerSettings jsonset = new JsonSerializerSettings
            {
                DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"
            };
                string strJson ="data=query Query{events(for_guid:\"" + Guid + "\",start:" + JsonConvert.SerializeObject(start,jsonset) + ",end:" + JsonConvert.SerializeObject(end, jsonset) + "){guid,description,start,end,location,subject,attendees{principal{guid,name,sort_key,group{guid,name,sort_key,personal_colour}},role}}}";
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
                return Events;
            }
            catch(Exception e)
            {
                logger.Error(e, "Fetching of Planner Data Error");
            }
            return new FFEvent[0];
        }

        public void UpdateResponses(ref FullTask task, Response[] responses)
        {
            Dictionary<string,Response> respList = task.allRecipientsResponses[0].responses.ToDictionary(x => x.eventGuid);
            foreach (Response item in responses) {
                if (respList.ContainsKey(item.eventGuid))
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
                    respList[item.eventGuid] = item;
                }
                else
                {
                    //Add new items to list
                    respList.Add(item.eventGuid, item);
                }
            }
        }

        public Response[] GetResponseForID(int ID)
        {
            if (!loggedIn)
                return new Response[0];
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

        private FullTask[] GetFiftyTasksByIds(int[] FiftyIds)
        {
            //max task id size is 50 
            if (!loggedIn)
                return new FullTask[0];
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
            }
            return new FullTask[0];
        }

        

        

        private void LoginWindow_OnResult(object sender, Login.OnResultEventArgs e)
        {
            //if successful login and double check
            if (e.Result&& SSO(e.Token))
            {
                //Set variables
                loggedIn = true;
                Token = e.Token;
                SaveKey();
                //Trigger Successful login event
                OnLoggedIn(new EventArgs());
            }
        }

        private bool SSO(string key)
        {
            if (SchoolUrl == "")
                return false;
            //Create webrequest for User details
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SchoolUrl + @"/login/api/sso?ffauth_device_id=" + DeviceID +"&ffauth_secret=" + key);
            string html;
            //catch unauthorised error
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
            }
            //Only catch webexception errors
            catch(System.Net.WebException e)
            {
                logger.Warn(e, "SSO Failure");
                return false;
            }
            //Parse results
            Match result = Regex.Match(html, "identifier=\"(.*)\" username=\"(.*)\" name=\"(.*)\" email=\"(.*)\" canSetTask=\"(.*)\"");
            if (result.Success)
            {
                //Set user GUID
                guid = result.Groups[1].Value;
                //Set Username aka id name
                username = result.Groups[2].Value;
                //Set Name aka full name
                name = result.Groups[3].Value;
                //Set User Email
                email = result.Groups[4].Value;
                //Check if cansetTasks is true or yes since its returned as a string and needs to be converted to bool
                canSetTasks = (result.Groups[1].Value.ToLower() == "yes" || result.Groups[1].Value.ToLower() == "true");
                return true;
            }
            return false;
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
