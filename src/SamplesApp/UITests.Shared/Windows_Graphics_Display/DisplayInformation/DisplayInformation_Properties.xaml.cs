using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System.Reflection;
using System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Graphics_Display.DisplayInformation
{
	[SampleControlInfo("DisplayInformation", "DisplayInformation_Properties")]
	public sealed partial class DisplayInformation_Properties : UserControl
	{
		public DisplayInformation_Properties()
		{
			this.InitializeComponent();
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
			//var info = new Windows.Graphics.Display.DisplayInformation.();
			//var propertyInfos = info.GetTypeInfo().GetProperties(BindingFlags.Public);
			//var properties = propertyInfos.Select(p => new PropertyInformation() { Name = p.Name });
            
		}
	}
}
