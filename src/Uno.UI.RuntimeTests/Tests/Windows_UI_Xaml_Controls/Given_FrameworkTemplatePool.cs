#nullable disable

using System;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
internal class Given_FrameworkTemplatePool
{
	[TestMethod]
	public async Task TestCheckBox()
	{
		using (FeatureConfigurationHelper.UseTemplatePooling())
		{
			var scrollViewer = new ScrollViewer();
			var elevatedViewChild = new Uno.UI.Toolkit.ElevatedView();
			scrollViewer.Content = elevatedViewChild;

			var c = new CheckBox();
			c.IsChecked = true;
			bool uncheckedFired = false;
			c.Unchecked += (_, _) => uncheckedFired = true;
			elevatedViewChild.ElevatedContent = c;

			WindowHelper.WindowContent = scrollViewer;
			await WindowHelper.WaitForLoaded(scrollViewer);
			var template = scrollViewer.Template;

			scrollViewer.Template = null;
			scrollViewer.Template = template;

			scrollViewer.ApplyTemplate();

			Assert.IsFalse(uncheckedFired);
		}
	}

	[TestMethod]
	public async Task TestTextBox()
	{
		using (FeatureConfigurationHelper.UseTemplatePooling())
		{
			var scrollViewer = new ScrollViewer();

			var textBox = new TextBox();
			bool textChangedFired = false;
			textBox.TextChanged += (_, _) => textChangedFired = true;
			textBox.Text = "Text";

			// TextBox dispatches raising the event. We wait here until the above TextChanged is raised.
			await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { });

			Assert.IsTrue(textChangedFired);
			textChangedFired = false;

			scrollViewer.Content = textBox;

			WindowHelper.WindowContent = scrollViewer;
			await WindowHelper.WaitForLoaded(scrollViewer);
			var template = scrollViewer.Template;

			scrollViewer.Template = null;
			scrollViewer.Template = template;

			scrollViewer.ApplyTemplate();

			await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { });

			Assert.IsFalse(textChangedFired);
		}
	}

	[TestMethod]
	public async Task TestToggleSwitch()
	{
		using (FeatureConfigurationHelper.UseTemplatePooling())
		{
			var scrollViewer = new ScrollViewer();

			var toggleSwitch = new ToggleSwitch();
			toggleSwitch.IsOn = true;
			bool toggledFired = false;
			toggleSwitch.Toggled += (_, _) => toggledFired = true;
			scrollViewer.Content = toggleSwitch;

			WindowHelper.WindowContent = scrollViewer;
			await WindowHelper.WaitForLoaded(scrollViewer);
			var template = scrollViewer.Template;

			scrollViewer.Template = null;
			scrollViewer.Template = template;

			scrollViewer.ApplyTemplate();

			Assert.IsFalse(toggledFired);
		}
	}
}
