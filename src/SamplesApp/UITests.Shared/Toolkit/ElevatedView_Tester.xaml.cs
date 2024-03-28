using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
			Color color = ConvertColorFromHexString(ColorString);
			ElevatedElement.ShadowColor = color;
			ElevatedElement.Elevation = Elevation;
			ElevatedElement.CornerRadius = new CornerRadius(Radius);
		}

		private Color ConvertColorFromHexString(string colorString)
		{
			try
			{
				//Target hex string
				colorString = colorString.Replace("#", string.Empty);
				// from #RRGGBB string
				var a = (byte)System.Convert.ToUInt32(colorString.Substring(0, 2), 16);
				var r = (byte)System.Convert.ToUInt32(colorString.Substring(2, 2), 16);
				var g = (byte)System.Convert.ToUInt32(colorString.Substring(4, 2), 16);
				var b = (byte)System.Convert.ToUInt32(colorString.Substring(6, 2), 16);
				//get the color
				return Color.FromArgb(a, r, g, b);
			}
			catch
			{
				return Colors.Black;
			}
		}
	}
}
