using System;
using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using Windows.UI;
using Microsoft.UI;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_Dictionary : BaseTestClass
{
	[TestMethod]
	public async Task When_Change_AppResource_String()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		// We're not storing the instance explicitly, as the HR engine replaces
		// the top level content of the window. We keep poking at the UnitTestsUIContentHelper.Content
		// as it gets updated with reloaded content.
		UnitTestsUIContentHelper.Content = new HR_Frame_Pages_AppResources();

		var originalText = "** HR_Frame_Pages_AppResources Original String **";
		var updatedText = "** HR_Frame_Pages_AppResources Updated String **";

		// Check the initial text of the TextBlock
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 0);

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateProjectFileAndRevert(
			"AppResources.xaml",
			originalText,
			updatedText,
			() => UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(updatedText, 0),
			ct);

		// Validate that content been returned to the original text
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 0);

		await Task.Yield();
	}

	[TestMethod]
	public async Task When_Change_AppResource_DataTemplate()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		// We're not storing the instance explicitly, as the HR engine replaces
		// the top level content of the window. We keep poking at the UnitTestsUIContentHelper.Content
		// as it gets updated with reloaded content.
		UnitTestsUIContentHelper.Content = new HR_Frame_Pages_AppResources_DataTemplate();

		var originalText = "** HR_Frame_Pages_AppResources_DataTemplate_Resource01 Original String **";
		var updatedText = "** HR_Frame_Pages_AppResources_DataTemplate_Resource01 Updated String **";

		// Check the initial text of the TextBlock
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 0);

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateProjectFileAndRevert(
			"AppResources.xaml",
			originalText,
			updatedText,
			() => UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(updatedText, 0),
			ct);

		// Validate that content been returned to the original text
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 0);

		await Task.Yield();
	}

	[TestMethod]
	public async Task When_Change_AppResource_Color()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		// We're not storing the instance explicitly, as the HR engine replaces
		// the top level content of the window. We keep poking at the UnitTestsUIContentHelper.Content
		// as it gets updated with reloaded content.
		UnitTestsUIContentHelper.Content = new HR_Frame_Pages_AppResources_Color();

		var originalText = "x:Key=\"HR_Frame_Pages_AppResources_Color_Resource01\" Color=\"Red\"";
		var updatedText = "x:Key=\"HR_Frame_Pages_AppResources_Color_Resource01\" Color=\"Blue\"";

		bool ValidateColor(FrameworkElement root, Color color)
		{
			if (root.FindName("tb01") is TextBlock tb)
			{
				if (tb.Foreground is SolidColorBrush scb)
				{
					return scb.Color == color;
				}
			}

			return false;
		}

		// Check the initial text of the TextBlock
		await UnitTestsUIContentHelper.Content.ValidateChildElement<FrameworkElement>(e => ValidateColor(e, Colors.Red));

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateProjectFileAndRevert(
			"AppResources.xaml",
			originalText,
			updatedText,
			() => UnitTestsUIContentHelper.Content.ValidateChildElement<FrameworkElement>(e => ValidateColor(e, Colors.Blue)),
			ct);

		// Validate that content been returned to the original text
		await UnitTestsUIContentHelper.Content.ValidateChildElement<FrameworkElement>(e => ValidateColor(e, Colors.Red));

		await Task.Yield();
	}

	[TestMethod]
	public async Task When_Change_SubDictionary_String()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		// We're not storing the instance explicitly, as the HR engine replaces
		// the top level content of the window. We keep poking at the UnitTestsUIContentHelper.Content
		// as it gets updated with reloaded content.
		UnitTestsUIContentHelper.Content = new HR_Frame_Pages_AppResources();

		var originalText = "** HR_Frame_Pages_AppResources_SubDictionary Original String **";
		var updatedText = "** HR_Frame_Pages_AppResources_SubDictionary Updated String **";

		// Check the initial text of the TextBlock
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 1);

		// Check the updated text of the TextBlock
		await HotReloadHelper.UpdateProjectFileAndRevert(
			"SubResourceDictionary.xaml",
			originalText,
			updatedText,
			() => UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(updatedText, 1),
			ct);

		// Validate that content been returned to the original text
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 1);

		await Task.Yield();
	}

	[TestMethod]
	//[Ignore("Failing ramdomly on CI")]
	public async Task When_Change_AppResource_LotOfTimes()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(90)).Token;

		// We're not storing the instance explicitly, as the HR engine replaces
		// the top level content of the window. We keep poking at the UnitTestsUIContentHelper.Content
		// as it gets updated with reloaded content.
		UnitTestsUIContentHelper.Content = new HR_Frame_Pages_AppResources();

		var originalText = "** HR_Frame_Pages_AppResources Original String **";
		Func<int, string> updatedText = i => $"** HR_Frame_Pages_AppResources Updated String#{i:D2} **";

		// Check the initial text of the TextBlock
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 0);

		// Check the updated text of the TextBlock
		for (int i = 0; i < 15; i++)
		{
			await HotReloadHelper.UpdateProjectFileAndRevert(
				"AppResources.xaml",
				originalText,
				updatedText(i),
				() => UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(updatedText(i), 0),
				ct);

		}

		// Validate that content been returned to the original text
		await UnitTestsUIContentHelper.Content.ValidateTextOnChildTextBlock(originalText, 0);

		await Task.Yield();
	}
}
