using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //set to use only MYTGS firefly cloud 
        Firefly.Firefly FF = new Firefly.Firefly("MYTGS");
        public MainWindow()
        {
            InitializeComponent();
            FF.OnLogin += FF_OnLogin;
            if (!FF.LoadfromFile())
            {
                FF.LoginUI();
            }
        }

        private void FF_OnLogin(object sender, EventArgs e)
        {
            UserName.Content = FF.Name;
            TasksBlock.Text = "";
            int x = 0;
            string TasksPath = Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["TasksPath"]);
            if (!Directory.Exists(TasksPath))
                Directory.CreateDirectory(TasksPath);
            if (TasksPath[TasksPath.Length - 1] != '\\' || TasksPath[TasksPath.Length - 1] != '/')
                TasksPath += "\\";
            foreach (Firefly.FullTask task in FF.GetAllTasksByIds(FF.GetAllIds()))
            {
                x++;
                TasksBlock.Text += "\r\n" + task.title + " - " + task.id + " -- " + x;
                if (!Directory.Exists(TasksPath + task.id))
                    Directory.CreateDirectory(TasksPath + task.id);
                File.WriteAllText(TasksPath + task.id +@"\Task.json",JsonConvert.SerializeObject(task, Formatting.Indented));
            }
            //Environment.ExpandEnvironmentVariables((string)Properties.Settings.Default["TasksPath"])
        }
    }
}
