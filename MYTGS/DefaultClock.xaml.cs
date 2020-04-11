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
    /// Interaction logic for DefaultClock.xaml
    /// </summary>
    public partial class DefaultClock : UserControl
    {
        public event EventHandler MouseHoveringHide;

        protected virtual void OnMouseHoveringHide()
        {
            if (MouseHoveringHide != null) MouseHoveringHide(this, EventArgs.Empty);
        }

        public void MouseHoverHide(object sender, MouseEventArgs e)
        {
            OnMouseHoveringHide();
        }

        public DefaultClock()
        {
            InitializeComponent();
        }
    }
}
