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
using System.Text;


namespace Firefly
{
    class Firefly
    {
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
        private string schoolUrl = null;
        private string schoolName;
        //Event when Logged in
        public event EventHandler OnLogin;
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

        public Firefly(string school)
        {
            WebClient web = new WebClient();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(web.DownloadString(@"http://appgateway.ffhost.co.uk/appgateway/school/" + school));
            if (xml.SelectNodes("response") .Count==1&& xml.SelectNodes("response")[0].Attributes["exists"].Value == "true" && xml.SelectNodes("response")[0].Attributes["enabled"].Value=="true")
            {
                XmlNode response = xml.SelectNodes("response")[0];
                schoolName = response.FirstChild.InnerText;
                if (response.LastChild.Attributes["ssl"].Value == "true")
                {
                    schoolUrl = "https://" + response.LastChild.InnerText;
                }
                else
                {
                    schoolUrl = "http://" + response.LastChild.InnerText;
                }
            }
        }

        //file location
        private string Path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MYTGS\Data.dat";

        //Login call returns true if token was sucessfully received
        public void LoginUI()
        {
            if (LoginWindow!=null)
            {
                LoginWindow.Show();
                LoginWindow.Activate();
                return;
            }
            LoginWindow = new Login(schoolUrl + "/login/api/loginui?app_id=android_tasks&device_id=" + DeviceID );
            LoginWindow.Show();
            LoginWindow.OnResult += LoginWindow_OnResult;
        }

        public bool LoadfromFile()
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
            File.WriteAllText(Path, Token);
            File.SetAttributes(Path, FileAttributes.Hidden);
        }

        public int[] GetAllIds()
        {
            //Fail if not logged in
            if (!loggedIn)
                return new int[0];
            try
            {
                WebRequest request = WebRequest.Create(SchoolUrl + @"/api/v2/apps/tasks/ids/filterby?ffauth_device_id=" + DeviceID + "&ffauth_secret=" + Token);
                request.Method = "POST";
                request.ContentType = "application/json";
                string html;
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
                //do nothing
            }
            return new int[0];
        }

        public FullTask[] GetAllTasksByIds(int[] Ids)
        {
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
                
                Console.WriteLine("D " + html);
                FullTask[] Tasks;
                Tasks = JsonConvert.DeserializeObject<FullTask[]>(html, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
                return Tasks;
            }
            catch
            {
                //do nothing
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
                //Return  that program failed
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
            handler?.Invoke(this, e);
        }
    }
}
