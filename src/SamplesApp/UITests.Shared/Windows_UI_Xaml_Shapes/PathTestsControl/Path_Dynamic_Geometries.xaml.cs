using System;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace UITests.Windows_UI_Xaml_Shapes.PathTestsControl
{
	[Sample]
    public sealed partial class Path_Dynamic_Geometries : Page
    {
        public Path_Dynamic_Geometries()
        {
            this.InitializeComponent();
        }
    }

	internal class ValueToPointConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if(targetType == typeof(Point))
			{
				return new Point(System.Convert.ToDouble(value), System.Convert.ToDouble(parameter));
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
