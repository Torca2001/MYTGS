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
    /// Interaction logic for Period.xaml
    /// </summary>
    public partial class Period : UserControl
    {
        int ExpandedHeight = 80;

        public Period()
        {
            InitializeComponent();
            if (ActualHeight > ExpandedHeight)
            {
                TeacherLabel.Visibility = Visibility.Visible;
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualHeight > ExpandedHeight)
            {
                TeacherLabel.Visibility = Visibility.Visible;
            }
            else
            {
                TeacherLabel.Visibility = Visibility.Collapsed;
            }
        }

        internal void SetValue(Action<MouseButtonEventArgs> onMouseDown, object openColourPicker)
        {
            throw new NotImplementedException();
        }
    }
}
