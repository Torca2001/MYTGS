using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace MYTGS
{
    class Firefly
    {
        //variables from SSO
        public bool LoggedIn = false;
        public string name;
        public string username;
        public string guid;
        public string email;
        public bool canSetTasks;
        //Event when Logged in
        public event EventHandler OnLogin;
        //Auth Token for server access
        private string Token;
        //Device ID for access as well
        private string DeviceID = "TT" + Environment.MachineName;
        //Login call returns true if token was sucessfully received
        public void LoginUI()
        {
            Login LoginWindow = new Login("https://mytgs.fireflycloud.net.au/login/api/loginui?app_id=android_tasks&device_id=" + DeviceID );
            LoginWindow.Show();
            LoginWindow.OnResult += LoginWindow_OnResult;
        }

        private void LoginWindow_OnResult(object sender, Login.OnResultEventArgs e)
        {
            if (e.Result)
            {
                LoggedIn = true;
                Token = e.Token;
                SSO();
            }
            OnLoggedIn(new EventArgs());
        }

        private void SSO()
        {
            //Check to ensure Logged in
            if (!LoggedIn)
                return;
            //Create webrequest for User details
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://mytgs.fireflycloud.net.au/login/api/sso?ffauth_device_id=" + DeviceID +"&ffauth_secret=" + Token);
            string html;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
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
                if (result.Groups[1].Value.ToLower() == "yes" || result.Groups[1].Value.ToLower() == "true")
                    canSetTasks = true;
                else
                    canSetTasks = false;
            }
        }

        //Event to fire when a successfull login has occured;
        protected virtual void OnLoggedIn(EventArgs e)
        {
            EventHandler handler = OnLogin;
            handler?.Invoke(this, e);
        }
    }
}
