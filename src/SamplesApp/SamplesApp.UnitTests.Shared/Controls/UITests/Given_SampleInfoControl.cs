#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Entities;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_SampleInfoControl
{
	[TestMethod]
	public void When_Copy_Parameter_Is_Not_Text_Nothing_Throws()
	{
		SampleInfoControl control = new();

		foreach (object? parameter in new object?[] { null, string.Empty, 42 })
		{
			control.CopyContentClick(new Button { CommandParameter = parameter }, new RoutedEventArgs());
		}

		control.CopyContentClick(new Border(), new RoutedEventArgs());
	}

	[TestMethod]
	[DataRow("Buttons/Button_Events.xaml", "Button_Events.xaml")]
	[DataRow(@"Buttons\Button_Events.xaml", "Button_Events.xaml")]
	[DataRow("Button_Events.xaml", "Button_Events.xaml")]
	[DataRow("", "")]
	[DataRow(null, "")]
	public void When_Source_Path_Is_Given_File_Name_Is_Last_Segment(string? path, string expected)
		=> Assert.AreEqual(expected, SampleInfoControl.GetFileName(path));

	[TestMethod]
	[DataRow("?sample=Buttons/Button_Events", null, "-- sample=Buttons/Button_Events")]
	[DataRow("?sample=Buttons/Button_Events", "UITests.Buttons.Button_Events", "-- sample=Buttons/Button_Events")]
	[DataRow("?sample=Lottie/Lottie Progress", "UITests.Lottie.LottieProgressPage", "-- sample=UITests.Lottie.LottieProgressPage")]
	[DataRow("?sample=Lottie/AnimatedVisualPlayer Visibility (#23189)", "UITests.Lottie.AnimatedVisualPlayerVisibility", "-- sample=UITests.Lottie.AnimatedVisualPlayerVisibility")]
	[DataRow("?sample=Lottie/Lottie Progress", null, "-- \"sample=Lottie/Lottie Progress\"")]
	[DataRow("", null, "")]
	[DataRow(null, null, "")]
	public void When_Query_String_Is_Given_Launch_Argument_Drops_Question_Mark(string? query, string? typeName, string expected)
		=> Assert.AreEqual(expected, SampleInfoControl.GetLaunchArgument(query, typeName));

	[TestMethod]
	public void When_Sample_Has_Flags_Chips_List_Categories_First()
	{
		var chips = SampleInfoControl.GetChips(new SampleChooserContent
		{
			Categories = ["Buttons", "Input", " "],
			IsManualTest = true,
			UsesFrame = true,
			DisableKeyboardShortcuts = true,
			IgnoreInSnapshotTests = true,
		});

		CollectionAssert.AreEqual(
			new[] { "Buttons", "Input", "Manual", "Uses Frame", "Shortcuts disabled", "Not in snapshots" },
			chips.ToArray());
		Assert.AreEqual(0, SampleInfoControl.GetChips(new SampleChooserContent()).Count);
	}

	[TestMethod]
	[DataRow(1280d, 400d)]
	[DataRow(412d, 348d)]
	[DataRow(345d, 281d)]
	[DataRow(200d, 240d)]
	public void When_Fitted_To_Window_Width_Stays_Within_400_And_Window(double windowWidth, double expected)
	{
		SampleInfoControl control = new();
		control.FitToWindow(new Windows.Foundation.Size(windowWidth, 800));
		Assert.AreEqual(expected, control.Width);
	}

	[TestMethod]
	public async Task When_No_Sample_Is_Selected_Panel_Shows_Empty_State()
	{
		var control = await Load(null);

		Assert.AreEqual(Visibility.Visible, Find<UIElement>(control, "ShellInfoEmptyText").Visibility);
		Assert.AreEqual(Visibility.Collapsed, Find<UIElement>(control, "ShellInfoContent").Visibility);
	}

	[TestMethod]
	public async Task When_Sample_Is_Shown_Rows_Copy_Their_Values_And_Glyph_Flips()
	{
		var control = await Load(CreateSample());
		var copied = new List<DataPackageView>();
		var original = SampleChooserViewModel.SetClipboardContent;
		SampleChooserViewModel.SetClipboardContent = package => copied.Add(package.GetView());
		try
		{
			var expected = new (string Name, string Text)[]
			{
				("ShellCopyTypeButton", typeof(Button).FullName!),
				("ShellCopyLinkInfoButton", "?sample=Buttons/Button_Info"),
				("ShellCopyLaunchButton", "-- sample=Buttons/Button_Info"),
				("ShellCopySourceButton", "Buttons/Button_Info.xaml"),
			};

			foreach (var (name, text) in expected)
			{
				var button = Find<Button>(control, name);
				Assert.AreEqual(Visibility.Visible, button.Visibility, name);
				Assert.AreEqual(text, button.CommandParameter, name);

				control.CopyContentClick(button, new RoutedEventArgs());
				Assert.AreEqual(text, await copied[^1].GetTextAsync(), name);
				Assert.AreEqual("\uE73E", ((FontIcon)button.Content).Glyph, $"{name} shows the check");
				Assert.AreEqual("Copied", Find<TextBlock>(control, "ShellInfoCopiedAnnouncement").Text, name);
			}

			Assert.AreEqual("Button_Info.xaml", Find<TextBlock>(control, "ShellInfoSourceValue").Text, "The source row shows the file name only.");

			Assert.AreEqual(4, copied.Count);
			Assert.AreEqual("\uE8C8", ((FontIcon)Find<Button>(control, "ShellCopyTypeButton").Content).Glyph, "A later copy restores the earlier glyph.");
		}
		finally
		{
			SampleChooserViewModel.SetClipboardContent = original;
		}
	}

	[TestMethod]
	public async Task When_Clipboard_Throws_Copy_Keeps_Its_Glyph()
	{
		var control = await Load(CreateSample());
		var original = SampleChooserViewModel.SetClipboardContent;
		SampleChooserViewModel.SetClipboardContent = _ => throw new InvalidOperationException("clipboard busy");
		try
		{
			var button = Find<Button>(control, "ShellCopyTypeButton");
			control.CopyContentClick(button, new RoutedEventArgs());
			Assert.AreEqual("\uE8C8", ((FontIcon)button.Content).Glyph);
		}
		finally
		{
			SampleChooserViewModel.SetClipboardContent = original;
		}
	}

	[TestMethod]
	public async Task When_Sample_Has_No_Type_Or_Source_Those_Rows_Hide()
	{
		var sample = CreateSample();
		sample.ControlType = null!;
		sample.SourceFilePath = null!;
		var control = await Load(sample);

		foreach (var name in new[] { "ShellInfoTypeLabel", "ShellInfoTypeValue", "ShellCopyTypeButton", "ShellInfoSourceLabel", "ShellCopySourceButton", "GitHubLinkButton" })
		{
			Assert.AreEqual(Visibility.Collapsed, Find<UIElement>(control, name).Visibility, name);
		}

		Assert.AreEqual(Visibility.Visible, Find<UIElement>(control, "ShellCopyLinkInfoButton").Visibility);
		Assert.AreEqual(Visibility.Visible, Find<UIElement>(control, "ShellInfoNoDescription").Visibility);
	}

	[TestMethod]
	public async Task When_Shown_Copied_Announcement_Adds_No_Gap()
	{
		var control = await Load(CreateSample());
		var content = Find<StackPanel>(control, "ShellInfoContent");
		var announcement = Find<TextBlock>(control, "ShellInfoCopiedAnnouncement");

		Assert.IsFalse(content.Children.Contains(announcement), "A child of the spaced stack adds a 12 DIP strip.");
		Assert.AreEqual(content.ActualHeight, control.ActualHeight, 0.5);
	}

	[TestMethod]
	public async Task When_Platform_Has_One_Window_Open_In_New_Window_Is_Hidden()
	{
		var control = await Load(CreateSample());
#if HAS_UNO
		Assert.AreEqual(Uno.UI.Xaml.Controls.NativeWindowFactory.SupportsMultipleWindows, SampleChooserViewModel.CanCreateNewWindow);
#endif
		Assert.AreEqual(
			SampleChooserViewModel.CanCreateNewWindow ? Visibility.Visible : Visibility.Collapsed,
			Find<UIElement>(control, "ShellInfoOpenNewWindowButton").Visibility);
	}

	[TestMethod]
	public async Task When_Window_Is_Phone_Width_Links_Stack_And_Panel_Fits()
	{
		var control = await Load(CreateSample());
		control.FitToWindow(new Windows.Foundation.Size(345, 700));
		// WaitForIdle does not run layout on WinUI.
		control.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(Orientation.Vertical, Find<StackPanel>(control, "ShellInfoLinks").Orientation);
		Assert.IsTrue(control.ActualWidth <= 281.5, $"Panel width {control.ActualWidth}");

		control.FitToWindow(new Windows.Foundation.Size(412, 800));
		Assert.AreEqual(Orientation.Horizontal, Find<StackPanel>(control, "ShellInfoLinks").Orientation, "Both links fit side by side on a 412 phone.");

		control.FitToWindow(new Windows.Foundation.Size(1280, 800));
		Assert.AreEqual(Orientation.Horizontal, Find<StackPanel>(control, "ShellInfoLinks").Orientation);
	}

	private static SampleChooserContent CreateSample() => new()
	{
		ControlName = "Button_Info",
		ControlType = typeof(Button),
		Categories = ["Buttons"],
		SourceFilePath = "Buttons/Button_Info.xaml",
	};

	private static async Task<SampleInfoControl> Load(SampleChooserContent? sample)
	{
		SampleInfoControl control = new() { DataContext = sample };
		TestServices.WindowHelper.WindowContent = control;
		await TestServices.WindowHelper.WaitForLoaded(control);
		await TestServices.WindowHelper.WaitForIdle();
		return control;
	}

	private static T Find<T>(SampleInfoControl control, string name) where T : class
		=> (control.FindName(name) as T) ?? throw new AssertFailedException($"{name} is missing.");
}
