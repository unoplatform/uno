#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Presentation;
using SamplesApp.Samples.Help;
using Uno.UI.RuntimeTests;
using Windows.System;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_HelpPage
{
	[TestMethod]
	public void When_Building_Rows_From_Catalogue()
	{
		var rows = HelpPage.BuildShortcutRows(ShellCommands.All);

		var primary = ShellCommands.All.Where(c => !c.IsAlias).ToArray();
		CollectionAssert.AreEqual(primary.Select(c => c.Id).ToArray(), rows.Select(r => r.Id).ToArray());
		Assert.IsFalse(rows.Any(r => r.Id is ShellCommands.FocusSearchAlias or ShellCommands.ShowRecentsAlias), "Aliases do not get their own row.");
	}

	[TestMethod]
	[DataRow(ShellCommands.FocusSearch, "Ctrl+F or / or Ctrl+K")]
	[DataRow(ShellCommands.ShowRecents, "Ctrl+H or Alt+R")]
	[DataRow(ShellCommands.ToggleFavorite, "Ctrl+Shift+D")]
	[DataRow(ShellCommands.ShowSettings, "Ctrl+,")]
	public void When_Row_Has_Alias_Or_Modifiers(string id, string expected)
		=> Assert.AreEqual(expected, HelpPage.BuildShortcutRows(ShellCommands.All).Single(r => r.Id == id).Shortcut);

	[TestMethod]
	public void When_Search_Row_Lists_Slash()
	{
		var row = HelpPage.BuildShortcutRows(ShellCommands.All).Single(r => r.Id == ShellCommands.FocusSearch);

		CollectionAssert.AreEqual(
			new[] { "Ctrl", "F", "or", "/", "or", "Ctrl", "K" },
			row.Keys.Select(k => k.Text).ToArray());
		Assert.AreEqual(ShellCommands.SearchCharacter.ToString(), row.Keys[3].Text);
	}

	[TestMethod]
	public void When_Every_Alias_Maps_To_One_Primary_Row()
	{
		var rows = HelpPage.BuildShortcutRows(ShellCommands.All);

		foreach (var alias in ShellCommands.All.Where(c => c.IsAlias))
		{
			Assert.AreEqual(1, ShellCommands.All.Count(c => !c.IsAlias && c.Id == alias.AliasOf), $"{alias.Id} must point at one primary entry.");
			var row = rows.Single(r => r.Id == alias.AliasOf);
			CollectionAssert.Contains(row.Shortcut.Split(" or "), alias.Shortcut, $"{alias.Id} is missing from the {row.Id} row.");
		}
	}

	[TestMethod]
	public void When_Alias_Label_Differs_It_Stays_On_Its_Row()
	{
		var commands = new[]
		{
			new ShellCommand("Primary", "Search samples", VirtualKey.F, VirtualKeyModifiers.Control, (_, _) => { }),
			new ShellCommand("Alias", "Search", VirtualKey.K, VirtualKeyModifiers.Control, (_, _) => { }, aliasOf: "Primary"),
		};

		var row = HelpPage.BuildShortcutRows(commands).Single();

		Assert.AreEqual("Ctrl+F or Ctrl+K", row.Shortcut);
	}

	[TestMethod]
	public void When_Splitting_Keys_Into_Keycaps()
	{
		var row = HelpPage.BuildShortcutRows(ShellCommands.All).Single(r => r.Id == ShellCommands.ShowRecents);

		CollectionAssert.AreEqual(
			new[] { "Ctrl", "H", "or", "Alt", "R" },
			row.Keys.Select(k => k.Text).ToArray());
		Assert.AreEqual(Visibility.Collapsed, row.Keys.Single(k => k.Text == "or").CapVisibility);
		Assert.AreEqual(Visibility.Visible, row.Keys.First().CapVisibility);
	}

	[TestMethod]
	[DataRow(ShellCommands.OpenRuntimeTests, "In a browser, use the rail or menu")]
	[DataRow(ShellCommands.OpenPlayground, "In a browser, use the rail or menu")]
	[DataRow(ShellCommands.ReloadSample, "In a browser, use the header buttons")]
	[DataRow(ShellCommands.ToggleFocusMode, "In a browser, use the more menu")]
	[DataRow(ShellCommands.FocusNextRegion, "Not available in a browser")]
	[DataRow(ShellCommands.ShowRecents, "In a browser, use Alt+R")]
	[DataRow(ShellCommands.FocusSearch, "In a browser, use Ctrl+F or /")]
	[DataRow(ShellCommands.ShowFavorites, "")]
	public void When_Shortcut_Is_Unsafe_In_Browsers(string id, string expected)
	{
		var row = HelpPage.BuildShortcutRows(ShellCommands.All).Single(r => r.Id == id);

		Assert.AreEqual(expected, row.Note);
		Assert.AreEqual(expected.Length > 0 ? Visibility.Visible : Visibility.Collapsed, row.NoteVisibility);
	}

	[TestMethod]
	public void When_Shortcut_Is_Unsafe_Note_Never_Says_Desktop()
		=> Assert.IsFalse(HelpPage.BuildShortcutRows(ShellCommands.All).Any(r => r.Note.Contains("Desktop")), "Unsafe means a browser takes the key, not that only desktops have it.");

	[TestMethod]
	public void When_FeatureConfiguration_Row_Asks_For_Separate_Arguments()
	{
		HelpPage page = new();

		var row = page.CommandLine.Single(r => r.Syntax.StartsWith("--FeatureConfiguration."));

		StringAssert.Contains(row.Description, "own, space-separated argument");
		Assert.IsFalse(page.CommandLine.Any(r => r.Description.Contains("--")), "Argument names belong in the monospace lines, not in wrapping prose.");
	}

	[TestMethod]
	[DataRow(200, true)]
	[DataRow(600, false)]
	public async Task When_Row_Is_Narrow_Keys_Go_Under_Label(double width, bool expectStacked)
	{
		var row = HelpPage.BuildShortcutRows(ShellCommands.All).Single(r => r.Id == ShellCommands.FocusSearch);
		HelpShortcutRowPanel panel = new() { Width = width };
		panel.Children.Add(new TextBlock { Text = row.Label, TextWrapping = TextWrapping.Wrap });
		StackPanel keys = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
		foreach (var key in row.Keys)
		{
			keys.Children.Add(new Border { Padding = new Thickness(6, 2, 6, 2), Child = new TextBlock { Text = key.Text } });
		}

		panel.Children.Add(keys);
		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(expectStacked, panel.IsStacked);
		var label = panel.Children[0].TransformToVisual(panel).TransformPoint(default);
		var keysOrigin = keys.TransformToVisual(panel).TransformPoint(default);
		Assert.IsTrue(keysOrigin.X + keys.ActualWidth <= panel.ActualWidth + 0.5, "The keycaps must stay inside the row.");
		Assert.AreEqual(expectStacked, keysOrigin.Y >= label.Y + ((FrameworkElement)panel.Children[0]).ActualHeight);
	}

	[TestMethod]
	public async Task When_Page_Is_Shown()
	{
		HelpPage page = new();
		TestServices.WindowHelper.WindowContent = page;
		await TestServices.WindowHelper.WaitForLoaded(page);
		await TestServices.WindowHelper.WaitForIdle();

		var shortcuts = (ItemsControl)page.FindName("ShellHelpShortcuts");
		Assert.AreEqual(page.Shortcuts.Count, shortcuts.Items.Count);
		Assert.IsTrue(shortcuts.ActualHeight > 0, "The shortcut list should be laid out.");

		var commandLine = (ItemsControl)page.FindName("ShellHelpCommandLine");
		var syntax = page.CommandLine.Select(r => r.Syntax).ToArray();
		Assert.IsTrue(syntax.Any(s => s.StartsWith("sample=")));
		Assert.IsTrue(syntax.Any(s => s.StartsWith("theme=")));
		Assert.IsTrue(syntax.Any(s => s.StartsWith("--runtime-tests=")));
		Assert.AreEqual(page.CommandLine.Count, commandLine.Items.Count);

		Assert.AreEqual(SampleChooserViewModel.CanCreateNewWindow ? Visibility.Visible : Visibility.Collapsed, page.NewWindowVisibility);
		Assert.IsFalse(Descendants(page).OfType<TextBlock>().Any(t => AutomationProperties.GetHeadingLevel(t) == AutomationHeadingLevel.Level1), "The shell header already titles the page.");
	}

	[TestMethod]
	public async Task When_Page_Is_Shown_Screen_Readers_Hear_The_Keys()
	{
		HelpPage page = new();
		TestServices.WindowHelper.WindowContent = page;
		await TestServices.WindowHelper.WaitForLoaded(page);
		await TestServices.WindowHelper.WaitForIdle();

		var label = Descendants(page).OfType<TextBlock>().Single(t => AutomationProperties.GetAutomationId(t) == ShellCommands.ReloadSample);
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(label);

		Assert.IsNotNull(peer, "The element carrying the row name needs an automation peer.");
		Assert.AreEqual("Reload sample, F5. In a browser, use the header buttons", peer.GetName());
		Assert.AreEqual(AutomationControlType.Text, peer.GetAutomationControlType());
	}

	private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			yield return child;
			foreach (var descendant in Descendants(child))
			{
				yield return descendant;
			}
		}
	}
}
