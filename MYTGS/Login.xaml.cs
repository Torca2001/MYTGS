using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public event OnResultEventHandler OnResult;
        private bool Success = false;
        public Login(string Url)
        {
            InitializeComponent();
            //Set browser to load up login page
            Browser.Source = new Uri(Url);
        }

        protected virtual void PushResult(OnResultEventArgs e)
        {
            OnResultEventHandler handler = OnResult;
            handler?.Invoke(this, e);
        }

        public delegate void OnResultEventHandler(object sender, OnResultEventArgs e);

        public class OnResultEventArgs : EventArgs
        {
            public bool Result { get; set; }
            public string Token { get; set; }
        }

        private void Browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //Check if url contains token
            Match result = Regex.Match(e.Uri.ToString(), @"token=(.*)");
            if (result.Success)
            {
                //Redo Layout to indicate that login was successfull
                TopRow.Height = new GridLength(0); //remove the browser
                MainGrid.Background = Brushes.Green; 
                Status_Label.Content = "Login Successful";
                //Set variable so closing event doesn't resend result event
                Success = true;
                //Create event
                
                OnResultEventArgs ev = new OnResultEventArgs();
                ev.Token = result.Groups[1].Value;
                ev.Result = true;
                //Send event
                PushResult(ev);
                //Close window
                Close();
                return;
            }
            //Checks if content is the redirect page for the token to prevent misfiring login message
            dynamic doc = Browser.Document;
            string htmlText = doc.documentElement.InnerHtml;
            //If html contains Working... which is only shown in the redirect page for a successful login
            if (htmlText.Contains("Working..."))
                return;
            MainGrid.Background = Brushes.Orange;
            Status_Label.Content = "Login Required - *Password will not be saved";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //If success bypass rest of code
            if (Success)
            {
                return;
            }
            //Push failed event
            OnResultEventArgs ev = new OnResultEventArgs();
            ev.Result = false;
            PushResult(ev);
        }
        
    }
}
