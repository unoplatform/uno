using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// Test page for static x:Bind in DataTemplate without DataType.
	/// </summary>
	public sealed partial class DataTemplate_StaticProperty_NoDataType_Control : Page
	{
		public DataTemplate_StaticProperty_NoDataType_Control()
		{
			this.InitializeComponent();
		}
	}

	public class DataTemplate_StaticProperty_NoDataType_Control_Data
	{
		public static string TestString => "StaticValue";
	}
}
