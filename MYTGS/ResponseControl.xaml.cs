using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MYTGS
{
    /// <summary>
    /// Interaction logic for ResponseControl.xaml
    /// </summary>
    public partial class ResponseControl : UserControl
    {
        public string Titlet { get; set; }
        public string Datet { get; set; }
        public ResponseControl()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
