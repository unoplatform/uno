using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System.Reflection;
using System;
using System.Linq;
using DisplayInfo = Windows.Graphics.Display.DisplayInformation;

namespace UITests.Shared.Windows_Graphics_Display.DisplayInformation
{
	[SampleControlInfo("DisplayInformation", "DisplayInformation_Properties", description: "Shows the values from DisplayInformation class properties. N/A for not implemented.")]
	public sealed partial class DisplayInformation_Properties : Page
	{
		public DisplayInformation_Properties()
		{
			this.InitializeComponent();
			RefreshDisplayInformation();
		}

		public class PropertyInformation
		{
			public PropertyInformation( string name, string value)
			{
				Name = name;
				Value = value;
			}

			public string Name { get; set; }
            
			public string Value { get; set; }

		}

		private void Refresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			RefreshDisplayInformation();
		}

		private void RefreshDisplayInformation()
		{
			var info = DisplayInfo.GetForCurrentView();
			var properties = new PropertyInformation[]
			{
				new PropertyInformation(nameof(DisplayInfo.AutoRotationPreferences), SafeGetValue(()=>DisplayInfo.AutoRotationPreferences)),
				new PropertyInformation(nameof(info.CurrentOrientation), SafeGetValue(()=>info.CurrentOrientation)),
				new PropertyInformation(nameof(info.NativeOrientation), SafeGetValue(()=>info.NativeOrientation)),
				new PropertyInformation(nameof(info.ScreenHeightInRawPixels), SafeGetValue(()=>info.ScreenHeightInRawPixels)),
				new PropertyInformation(nameof(info.ScreenWidthInRawPixels), SafeGetValue(()=>info.ScreenWidthInRawPixels)),
				new PropertyInformation(nameof(info.LogicalDpi), SafeGetValue(()=>info.LogicalDpi)),
				new PropertyInformation(nameof(info.DiagonalSizeInInches), SafeGetValue(()=>info.DiagonalSizeInInches)),
				new PropertyInformation(nameof(info.RawPixelsPerViewPixel), SafeGetValue(()=>info.RawPixelsPerViewPixel)),
				new PropertyInformation(nameof(info.RawDpiX), SafeGetValue(()=>info.RawDpiX)),
				new PropertyInformation(nameof(info.RawDpiY), SafeGetValue(()=>info.RawDpiY)),
				new PropertyInformation(nameof(info.ResolutionScale), SafeGetValue(()=>info.ResolutionScale)),
			};
			PropertyListView.ItemsSource = properties;
		}

		private string SafeGetValue<T>(Func<T> getter)
		{
			try
			{
				var value = getter();
				if ( value == null)
				{
					return "(null)";
				}
				return Convert.ToString(value);
			}
			catch (NotImplementedException)
			{
				return "(Not implemented)";
			}
			catch (NotSupportedException)
			{
				return "(Not supported)";
			}
		}
	}
}
