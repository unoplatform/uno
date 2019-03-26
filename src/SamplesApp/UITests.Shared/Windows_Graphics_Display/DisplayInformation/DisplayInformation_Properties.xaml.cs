using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System.Reflection;
using System;
using System.Linq;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Graphics_Display.DisplayInformation
{
	[SampleControlInfo("DisplayInformation", "DisplayInformation_Properties")]
	public sealed partial class DisplayInformation_Properties : UserControl
	{
		public DisplayInformation_Properties()
		{
			this.InitializeComponent();
			RefreshDisplayInformation();
		}

		public class PropertyInformation
		{
			public string Name { get; set; }
            
			public string Value { get; set; }

		}

		private void Refresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			RefreshDisplayInformation();
		}

		private void RefreshDisplayInformation()
		{
			var info = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
			var type = info.GetType();
			var typeInfo = type.GetTypeInfo();
			var propertyInfos = typeInfo.GetProperties();
			var properties = propertyInfos.Select(p => new PropertyInformation() { Name = p.Name, Value = SafeGetValue(p, info) }).ToArray();
			PropertyListView.ItemsSource = properties;
		}

		private string SafeGetValue(PropertyInfo propertyInfo, Windows.Graphics.Display.DisplayInformation info)
		{
			try
			{
				return Convert.ToString(propertyInfo.GetValue(info));
			}
			catch
			{
				return "N/A";
			}
		}
	}
}
