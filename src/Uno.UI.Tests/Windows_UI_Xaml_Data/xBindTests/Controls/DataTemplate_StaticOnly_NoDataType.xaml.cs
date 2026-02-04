using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public sealed partial class DataTemplate_StaticOnly_NoDataType : Page
	{
		public DataTemplate_StaticOnly_NoDataType()
		{
			this.InitializeComponent();
		}
	}

	public static class DataTemplate_StaticOnly_NoDataType_Static
	{
		public static string StaticProperty => "Static Value";
		public static int ClickCount { get; set; }

		public static void OnStaticClick(object sender, RoutedEventArgs e)
		{
			ClickCount++;
		}
	}
}
