#nullable enable

using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ShellSettingsView
{
	[TestMethod]
	[DataRow(300d, false, true, DisplayName = "Phone width, ComboBox")]
	[DataRow(300d, true, false, DisplayName = "Phone width, toggle")]
	[DataRow(359d, false, true, DisplayName = "Just under the breakpoint")]
	[DataRow(360d, false, false, DisplayName = "At the breakpoint")]
	[DataRow(600d, false, false, DisplayName = "Wide")]
	public void When_Card_Width_Changes_Stacking_Follows(double width, bool isToggle, bool isStacked)
		=> Assert.AreEqual(isStacked, ShellSettingsCard.ShouldStack(width, isToggle ? new ToggleSwitch() : new ComboBox()));

	[TestMethod]
	[DataRow(320d, true, DisplayName = "Browser pane width")]
	[DataRow(480d, false, DisplayName = "Wider than the breakpoint")]
	public async Task When_View_Is_Narrow_Controls_Move_Below_Text(double width, bool isStacked)
	{
		var view = await LoadSettingsView(width);

		var comboCard = FindCard(view, "ShellSettingsStartupPageComboBox");
		var toggleCard = FindCard(view, "ShellSettingsRtlToggle");
		var combo = (FrameworkElement)view.FindName("ShellSettingsStartupPageComboBox");
		var toggle = (FrameworkElement)view.FindName("ShellSettingsRtlToggle");

		Assert.AreEqual(isStacked, comboCard.IsStacked);
		Assert.IsFalse(toggleCard.IsStacked, "A toggle always fits beside the text.");

		// Stacked: the control sits under the header text; inline: on the same row, right of it.
		var comboTop = combo.TransformToVisual(comboCard).TransformPoint(default).Y;
		var toggleTop = toggle.TransformToVisual(toggleCard).TransformPoint(default).Y;
		Assert.AreEqual(isStacked, comboTop > toggleTop + 16, $"ComboBox top {comboTop}, toggle top {toggleTop}");
		Assert.IsTrue(combo.ActualWidth > 0 && combo.TransformToVisual(comboCard).TransformPoint(default).X + combo.ActualWidth <= comboCard.ActualWidth + 0.5, "The ComboBox stays inside its card.");
	}

	[TestMethod]
	public async Task When_Theme_Changes_Combo_Selection_Follows()
	{
		var vm = SampleChooserViewModel.Instance;
		var view = await LoadSettingsView(320);
		var combo = (ComboBox)view.FindName("ShellSettingsThemeComboBox");
		var wasTheme = vm.AppThemeIndex;

		try
		{
			vm.IsShellPersistenceSuspended = true;

			vm.AppThemeIndex = 2;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(2, combo.SelectedIndex);

			combo.SelectedIndex = 1;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(1, vm.AppThemeIndex);
			Assert.IsTrue(vm.IsAppThemeLight);
		}
		finally
		{
			vm.AppThemeIndex = wasTheme;
			vm.IsShellPersistenceSuspended = false;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Pane_Is_Narrow_Theme_Card_Stays_Compact()
	{
		var view = await LoadSettingsView(320);
		var themeCard = FindCard(view, "ShellSettingsThemeComboBox");
		var startupCard = FindCard(view, "ShellSettingsStartupPageComboBox");

		// Same shape as the startup page picker below it, instead of a tall list of radios.
		Assert.AreEqual(startupCard.ActualHeight, themeCard.ActualHeight, 1, "Theme and startup cards differ in height.");
		Assert.IsTrue(themeCard.ActualHeight < 120, $"Theme card is {themeCard.ActualHeight} high.");
	}

	[TestMethod]
	public async Task When_Card_Holds_Button_Name_Says_What_It_Does()
	{
		var view = await LoadSettingsView(320);
		var names = new[] { "ShellSettingsClearFavoritesButton", "ShellSettingsClearHistoryButton", "ShellSettingsRecordButton", "ShellSettingsLogViewDumpButton" }
			.Select(name => AutomationProperties.GetName((DependencyObject)view.FindName(name)))
			.ToArray();

		CollectionAssert.AreEqual(new[] { "Clear favorites", "Clear history", "Record all screenshots", "Log view dump" }, names);
	}

	[TestMethod]
	public async Task When_Startup_Page_Picked_View_Model_Follows()
	{
		var vm = SampleChooserViewModel.Instance;
		var view = await LoadSettingsView(320);
		var combo = (ComboBox)view.FindName("ShellSettingsStartupPageComboBox");
		var wasPage = vm.StartupPage;

		try
		{
			vm.IsShellPersistenceSuspended = true;

			Assert.AreEqual((int)wasPage, combo.SelectedIndex);

			combo.SelectedIndex = 2;
			Assert.AreEqual(StartupPage.LastSample, vm.StartupPage);

			vm.StartupPage = StartupPage.Playground;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(1, combo.SelectedIndex);

			// A ComboBox clearing its selection must not reset the setting.
			vm.StartupPageIndex = -1;
			Assert.AreEqual(StartupPage.Playground, vm.StartupPage);
		}
		finally
		{
			vm.StartupPage = wasPage;
			vm.IsShellPersistenceSuspended = false;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Mica_Toggled_Window_Backdrop_And_Shell_Follow()
	{
		if (!SampleChooserViewModel.CanUseMica)
		{
			Assert.Inconclusive("Mica is not supported here.");
		}

		var vm = SampleChooserViewModel.Instance;
		var window = App.MainWindow ?? throw new AssertInconclusiveException("No main window.");
		var shellRoot = (Panel)vm.Owner.FindName("ShellRoot");
		var states = VisualStateManager.GetVisualStateGroups(shellRoot).Single(g => g.Name == "ShellBackdropStates");

		try
		{
			vm.IsShellPersistenceSuspended = true;

			vm.UseMicaBackdrop = true;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsInstanceOfType(window.SystemBackdrop, typeof(MicaBackdrop));
			StringAssert.StartsWith(states.CurrentState?.Name, "BackdropMica");
			Assert.AreEqual(Microsoft.UI.Colors.Transparent, (shellRoot.Background as SolidColorBrush)?.Color);

			vm.UseMicaBackdrop = false;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsNull(window.SystemBackdrop);
			Assert.AreEqual("BackdropNoneState", states.CurrentState?.Name);
			Assert.AreNotEqual(Microsoft.UI.Colors.Transparent, (shellRoot.Background as SolidColorBrush)?.Color);
		}
		finally
		{
			vm.UseMicaBackdrop = false;
			vm.IsShellPersistenceSuspended = false;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Recording_Screenshots_Mica_Is_Kept_Out_Of_The_Shell()
	{
		if (!SampleChooserViewModel.CanUseMica)
		{
			Assert.Inconclusive("Mica is not supported here.");
		}

		var vm = SampleChooserViewModel.Instance;
		var shellRoot = (Panel)vm.Owner.FindName("ShellRoot");
		var states = VisualStateManager.GetVisualStateGroups(shellRoot).Single(g => g.Name == "ShellBackdropStates");
		var wasRecording = vm.IsRecordAllTests;

		try
		{
			vm.IsShellPersistenceSuspended = true;
			vm.UseMicaBackdrop = true;
			await TestServices.WindowHelper.WaitForIdle();

			// RenderTargetBitmap never sees the DWM backdrop, so a transparent root would record as see-through.
			vm.IsRecordAllTests = true;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual("BackdropNoneState", states.CurrentState?.Name);
			Assert.AreNotEqual(Microsoft.UI.Colors.Transparent, (shellRoot.Background as SolidColorBrush)?.Color);

			vm.IsRecordAllTests = false;
			await TestServices.WindowHelper.WaitForIdle();
			StringAssert.StartsWith(states.CurrentState?.Name, "BackdropMica");
		}
		finally
		{
			vm.IsRecordAllTests = wasRecording;
			vm.UseMicaBackdrop = false;
			vm.IsShellPersistenceSuspended = false;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Copy_Diagnostics_Clicked_Button_Confirms()
	{
		var view = await LoadSettingsView(320);
		var button = (Button)view.FindName("ShellCopyDiagnosticsButton");
		var text = (TextBlock)view.FindName("ShellCopyDiagnosticsText");
		var setClipboard = SampleChooserViewModel.SetClipboardContent;
		var copied = false;

		Assert.AreEqual("Copy diagnostics", text.Text);

		try
		{
			// Leaves the real clipboard alone.
			SampleChooserViewModel.SetClipboardContent = _ => copied = true;
			Invoke(button);
			await TestServices.WindowHelper.WaitForIdle();
		}
		finally
		{
			SampleChooserViewModel.SetClipboardContent = setClipboard;
		}

		Assert.IsTrue(copied);
		Assert.AreEqual("Copied", text.Text);
	}

	[TestMethod]
	public async Task When_Clipboard_Is_Busy_Copy_Diagnostics_Does_Not_Throw()
	{
		var view = await LoadSettingsView(320);
		var button = (Button)view.FindName("ShellCopyDiagnosticsButton");
		var text = (TextBlock)view.FindName("ShellCopyDiagnosticsText");
		var setClipboard = SampleChooserViewModel.SetClipboardContent;

		try
		{
			// CLIPBRD_E_CANT_OPEN, as WinAppSDK throws while another process holds the clipboard.
			SampleChooserViewModel.SetClipboardContent = _ => throw new COMException("Clipboard busy", unchecked((int)0x800401D0));
			Invoke(button);
			await TestServices.WindowHelper.WaitForIdle();
		}
		finally
		{
			SampleChooserViewModel.SetClipboardContent = setClipboard;
		}

		Assert.AreEqual("Copy diagnostics", text.Text);
	}

	[TestMethod]
	[DataRow(".NETCoreApp,Version=v10.0", "Android36.0", "net10.0-android36.0")]
	[DataRow(".NETCoreApp,Version=v11.0", "Windows10.0.19041.0", "net11.0-windows10.0.19041.0")]
	[DataRow(".NETCoreApp,Version=v10.0", "Desktop1.0", "net10.0-desktop")]
	[DataRow(".NETCoreApp,Version=v10.0", null, "net10.0")]
	[DataRow(null, null, "unknown")]
	public void When_Target_Platform_Is_Known_Framework_Includes_It(string? frameworkName, string? platformName, string expected)
		=> Assert.AreEqual(expected, AppInfo.FormatTargetFramework(frameworkName, platformName));

	[TestMethod]
	public void When_About_Is_Shown_Running_Framework_Has_A_Platform()
	{
		var framework = new AppInfo().TargetFramework;

		// Every SamplesApp head builds for a platform TFM.
		StringAssert.Matches(framework, new Regex(@"^net[0-9]+[.][0-9]+-[a-z]"));
	}

	[TestMethod]
	[DataRow("255.255.255.255+0123456789abcdef0123456789abcdef01234567", "255.255.255.255+0123456")]
	[DataRow("7.0.0-dev.12+abc", "7.0.0-dev.12+abc")]
	[DataRow("7.0.0", "7.0.0")]
	public void When_Version_Has_Commit_Display_Shortens_It(string version, string expected)
		=> Assert.AreEqual(expected, AppInfo.ShortenVersion(version));

	[TestMethod]
	public void When_Repository_Is_Shown_It_Breaks_Between_Folders()
	{
		const string ZeroWidthSpace = "\u200B";
		var display = AppInfo.AddPathBreaks(@"D:\Work\uno/src");

		Assert.AreEqual(@"D:\" + ZeroWidthSpace + @"Work\" + ZeroWidthSpace + "uno/" + ZeroWidthSpace + "src", display);
	}

	[TestMethod]
	public async Task When_Card_Holds_Toggle_Text_Keeps_The_Room()
	{
		var view = await LoadSettingsView(320);
		var toggle = (ToggleSwitch)view.FindName("ShellSettingsRtlToggle");

		// The stock 154 DIP minimum would squeeze the header to a word per line in the browser pane.
		Assert.IsTrue(toggle.ActualWidth is > 0 and < 80, $"Toggle is {toggle.ActualWidth} wide.");

		// A re-applied template (Fluent styles toggled) brings the minimum back.
		var template = toggle.Template;
		toggle.Template = null;
		await TestServices.WindowHelper.WaitForIdle();
		toggle.Template = template;

		// Uno does not re-measure on a Template change (WinUI does).
		toggle.InvalidateMeasure();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(toggle.ActualWidth is > 0 and < 80, $"Toggle is {toggle.ActualWidth} wide after the template is re-applied.");
	}

	private static void Invoke(Button button)
	{
		var invoker = new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke) as IInvokeProvider;
		Assert.IsNotNull(invoker);
		invoker.Invoke();
	}

	private static async Task<SettingsView> LoadSettingsView(double width)
	{
		SettingsView view = new() { DataContext = SampleChooserViewModel.Instance };
		Border host = new() { Width = width, Height = 600, Child = view };

		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(view);
		await TestServices.WindowHelper.WaitForIdle();

		return view;
	}

	private static ShellSettingsCard FindCard(SettingsView view, string controlName)
	{
		DependencyObject? current = (DependencyObject)view.FindName(controlName);
		while (current is not null and not ShellSettingsCard)
		{
			current = VisualTreeHelper.GetParent(current);
		}

		return current as ShellSettingsCard ?? throw new AssertFailedException($"{controlName} is not inside a card.");
	}
}
