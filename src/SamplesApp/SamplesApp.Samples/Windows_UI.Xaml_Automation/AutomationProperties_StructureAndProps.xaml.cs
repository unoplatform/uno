using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation
{
	[Sample("Automation", Name = "AutomationProperties_StructureAndProps", Description = "IsPeripheral/Culture properties and dynamic StructureChanged add/remove.")]
	public sealed partial class AutomationProperties_StructureAndProps : UserControl
	{
		private int _counter;

		public AutomationProperties_StructureAndProps()
		{
			this.InitializeComponent();

			AnnoTarget.SetValue(
				AutomationProperties.AnnotationsProperty,
				new List<AutomationAnnotation> { new AutomationAnnotation { Type = AnnotationType.Comment, Element = AnnoComment } });
		}

		private void OnAdd(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var index = _counter++;
			var block = new TextBlock { Text = $"Dynamic child {index}" };
			block.SetValue(AutomationProperties.AutomationIdProperty, $"struct-child-{index}");
			DynamicContainer.Children.Add(block);
		}

		private void OnAddCollapsed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var index = _counter++;
			var block = new TextBlock { Text = $"Collapsed child {index}", Visibility = Microsoft.UI.Xaml.Visibility.Collapsed };
			block.SetValue(AutomationProperties.AutomationIdProperty, $"struct-collapsed-{index}");
			DynamicContainer.Children.Add(block);
		}

		private void OnRemove(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			if (DynamicContainer.Children.Count > 0)
			{
				DynamicContainer.Children.RemoveAt(DynamicContainer.Children.Count - 1);
			}
		}

		private int _asbCounter;

		private void OnAsbUpdate(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			_asbCounter++;
			Asb.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
			Asb.ItemsSource = new[] { $"Suggestion {_asbCounter}", $"Suggestion {_asbCounter}-b" };
			Asb.IsSuggestionListOpen = true;
		}
	}
}
