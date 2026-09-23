using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI.Xaml_Automation
{
	[Sample("Automation", Name = nameof(AutomationProperties_AutomationId))]
	public sealed partial class AutomationProperties_AutomationId : UserControl
	{
		public AutomationProperties_AutomationId()
		{
			this.InitializeComponent();

			myList.ItemsSource = new[]
			{
				new MyItem { AutomationId = "Item01", Text = "Item 01" },
				new MyItem { AutomationId = "Item02", Text = "Item 02" },
				new MyItem { AutomationId = "Item03", Text = "Item 03" },
				new MyItem { AutomationId = "Item04", Text = "Item 04" },
				new MyItem { AutomationId = "Item05", Text = "Item 05" },
				new MyItem { AutomationId = "Item06", Text = "Item 06" },
				new MyItem { AutomationId = "Item07", Text = "Item 07" },
				new MyItem { AutomationId = "Item08", Text = "Item 08" },
				new MyItem { AutomationId = "Item09", Text = "Item 09" },
				new MyItem { AutomationId = "Item10", Text = "Item 10" },
			};

			ActionButton.Click += (_, _) => result.Text = "Fixture action invoked.";
			VolumeSlider.ValueChanged += (_, e) =>
			{
				var label = $"Volume {(int)e.NewValue}";
				VolumeValue.Text = label;
				AutomationProperties.SetName(VolumeValue, label);
			};
			ChoiceBox.SelectionChanged += (_, _) =>
			{
				if (ChoiceBox.SelectedItem is ComboBoxItem { Content: string content })
				{
					result.Text = content;
				}
			};
			AutomationProperties.GetControlledPeers(ActionButton).Add(ControlledField);
			AutomationProperties.GetDescribedBy(ControlledField).Add(RelationLabel);
			AutomationProperties.GetFlowsTo(ActionButton).Add(ControlledField);
			AutomationProperties.GetFlowsFrom(ControlledField).Add(ActionButton);

			myList.SelectionChanged += (s, e) =>
			{
				if (myList.SelectedItem is MyItem item)
				{
					result.Text = item.Text;
				}
				else
				{
					result.Text = "Clicked item is not of type MyItem";
				}
			};
		}
	}

	[Bindable]
	public class MyItem
	{
		public string Text { get; set; }
		public string AutomationId { get; set; }
	}
}
