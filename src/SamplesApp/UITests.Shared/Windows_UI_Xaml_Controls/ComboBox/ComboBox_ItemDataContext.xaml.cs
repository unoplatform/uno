using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ComboBox
{
	[SampleControlInfo("ComboBox", nameof(ComboBox_ItemDataContext))]
	public sealed partial class ComboBox_ItemDataContext : Page
	{
		public ComboBox_ItemDataContext()
		{
			this.InitializeComponent();

			DataContext = new ControlDataContext();
		}

		public class ControlDataContext
		{
			public DataContextItem[] Items { get; } =
			{
				new DataContextItem("Item A"),
				new DataContextItem("Item B"),
				new DataContextItem("Item C")
			};

			public string Value { get; } = "This value should NEVER be in the ComboBox";
		}

		public class DataContextItem
		{
			public DataContextItem(string value)
			{
				Value = value;
			}

			public string Value { get; }
		}
	}
}
