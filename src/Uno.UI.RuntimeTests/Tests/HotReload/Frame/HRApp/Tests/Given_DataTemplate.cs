
using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_DataTemplate : BaseTestClass
{
	/// <summary>
	/// Change the Text of a TextBlock inside a UserControl, where the UserControl 
	/// is nested inside a Viewbox
	/// </summary>
	[TestMethod]
	public async Task When_Change_DataTemplate()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

		// We're not storing the instance explicitly, as the HR engine replaces
		// the top level content of the window. We keep poking at the UnitTestsUIContentHelper.Content
		// as it gets updated with reloaded content.
		UnitTestsUIContentHelper.Content = new HR_Frame_Pages_DataTemplate();

		var originalText = "** Original Text **";
		var updatedText = "** Updated Text **";

		// Check the initial text of the TextBlock
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 0);

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_DataTemplate>(
			originalText,
			updatedText,
			() => UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(updatedText, 0),
			ct);

		// Validate that content been returned to the original text
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 0);

		await Task.Yield();
	}
}
