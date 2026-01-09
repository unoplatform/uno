using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// Test page for static event x:Bind in DataTemplate without DataType.
	/// </summary>
	public sealed partial class DataTemplate_StaticEvent_NoDataType_Control : Page
	{
		public DataTemplate_StaticEvent_NoDataType_Control()
		{
			this.InitializeComponent();
		}
	}

	public static class DataTemplate_StaticEvent_NoDataType_Control_Handler
	{
		public static int ClickCount { get; private set; }

		public static void OnClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ClickCount++;
		}
	}
}
