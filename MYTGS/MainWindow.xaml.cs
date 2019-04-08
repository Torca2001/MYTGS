using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Firefly FF = new Firefly();
        public MainWindow()
        {
            InitializeComponent();
            FF.LoginUI();
            FF.OnLogin += FF_OnLogin;
        }

        private void FF_OnLogin(object sender, EventArgs e)
        {
            UserName.Content = FF.name;
        }
    }
}
