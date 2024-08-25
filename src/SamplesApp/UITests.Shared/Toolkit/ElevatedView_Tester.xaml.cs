using Uno.UI.Samples.Controls;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace UITests.Toolkit
{
	[Sample]
	public sealed partial class ElevatedView_Tester : Page
	{
		private int _elevation;
		private int _radius;
		private string _color = "#FF000000";

		public ElevatedView_Tester()
		{
			this.InitializeComponent();
		}

		public
#if __ANDROID__
		new
#endif
		int Elevation
		{
			get => _elevation;
			set
			{
				_elevation = value;
				OnValuesChanged();
			}
		}

		public int Radius
		{
			get => _radius;
			set
			{
				_radius = value;
				OnValuesChanged();
			}
		}

		public string ColorString
		{
			get => _color;
			set
			{
				_color = value;
				OnValuesChanged();
			}
		}

		private void OnValuesChanged()
		{
			Color color = Microsoft.UI.ColorHelper.ConvertColorFromHexString(ColorString);
			ElevatedElement.ShadowColor = color;
			ElevatedElement.Elevation = Elevation;
			ElevatedElement.CornerRadius = new CornerRadius(Radius);
		}
	}
}
