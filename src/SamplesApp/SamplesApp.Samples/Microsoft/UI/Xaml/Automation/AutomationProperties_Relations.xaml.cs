using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation
{
	[Sample("Automation", Name = "AutomationProperties_Relations", Description = "DescribedBy and ControllerFor UIA relation properties.")]
	public sealed partial class AutomationProperties_Relations : UserControl
	{
		public AutomationProperties_Relations()
		{
			this.InitializeComponent();

			DescribedField.SetValue(AutomationProperties.DescribedByProperty, new List<DependencyObject> { DescriptionText });
			ControllerButton.SetValue(AutomationProperties.ControlledPeersProperty, new List<UIElement> { ControlledField });
		}
	}
}
