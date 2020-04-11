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
using System.Windows.Markup;
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

        public double SecondaryFontSize { get; set; } = 12;

        public Period()
        {
            InitializeComponent();
            if (ActualHeight > ExpandedHeight)
            {
                TeacherLabel.Visibility = Visibility.Visible;
                PeriodLabel.Visibility = Visibility.Visible;
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualHeight > ExpandedHeight)
            {
                TeacherLabel.Visibility = Visibility.Visible;
                PeriodLabel.Visibility = Visibility.Visible;
            }
            else
            {
                TeacherLabel.Visibility = Visibility.Collapsed;
                PeriodLabel.Visibility = Visibility.Collapsed;
            }
        }

    }

    public class MultiplyConverter : MarkupExtension, IValueConverter
    {
        public double Multiplier { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double bound = System.Convert.ToDouble(value);
            return bound * Multiplier;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
