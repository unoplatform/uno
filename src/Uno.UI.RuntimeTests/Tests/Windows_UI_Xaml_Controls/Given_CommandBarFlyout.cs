using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_CommandBarFlyout
{
	[TestMethod]
	[Ignore("Flaky on all targets - https://github.com/unoplatform/uno/issues/22862")]
	public async Task When_CommandBarFlyoutCommandBar_AlwaysExpanded()
	{
		var commandBarFlyout = new CommandBarFlyout
		{
			AlwaysExpanded = true
		};
		commandBarFlyout.PrimaryCommands.Add(new AppBarButton { Label = "Primary Command" });
		commandBarFlyout.SecondaryCommands.Add(new AppBarButton { Label = "Secondary Command" });

		var button = new Button
		{
			Content = "Open CommandBarFlyout",
		};

		FlyoutBase.SetAttachedFlyout(button, commandBarFlyout);

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);

		FlyoutBase.ShowAttachedFlyout(button);
		await TestServices.WindowHelper.WaitForIdle();

		var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);

		var commandBarPopup = popups.FirstOrDefault(p => VisualTreeUtils.FindVisualChildByType<CommandBarFlyoutCommandBar>(p.Child) is not null);
		Assert.IsNotNull(commandBarPopup);
		var commandBar = VisualTreeUtils.FindVisualChildByType<CommandBarFlyoutCommandBar>(commandBarPopup?.Child);
		Assert.IsNotNull(commandBar);

		bool wasClosed = false;
		commandBar.Closed += (s, e) => wasClosed = true;
		commandBar.IsOpen = false;

		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsTrue(commandBar.IsOpen, "CommandBarFlyout should remain open when AlwaysExpanded is true.");
		Assert.IsFalse(wasClosed, "CommandBarFlyout should not close when AlwaysExpanded is true.");
	}

#if HAS_UNO
	[TestMethod]
	public async Task When_CommandBarFlyout_Without_Secondary_Commands()
	{
		var commandBarFlyout = new CommandBarFlyout();
		commandBarFlyout.PrimaryCommands.Add(new AppBarButton { Icon = new SymbolIcon(Symbol.Home), Label = "Primary Command" });

		var button = new Button
		{
			Content = "Open CommandBarFlyout",
		};

		FlyoutBase.SetAttachedFlyout(button, commandBarFlyout);

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);

		FlyoutBase.ShowAttachedFlyout(button);
		await TestServices.WindowHelper.WaitForIdle();

		var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);

		var commandBar = popups.Select(p => (p.Child as FlyoutPresenter)?.Content as CommandBarFlyoutCommandBar).Where(c => c is not null).FirstOrDefault();
		Assert.IsNotNull(commandBar);

		var state = VisualStateManager.GetCurrentState(commandBar, "PrimaryLabelStates");
		Assert.AreEqual("HasPrimaryLabels", state.Name);
		Assert.AreEqual(Visibility.Visible, commandBar.CommandBarTemplateSettings.EffectiveOverflowButtonVisibility);
	}
#endif

	// ------------------------------------------------------------------
	// CommandBarFlyout first-open flash — kahua-private #480.
	//
	// Mirrors Given_MenuFlyout's Scenario C for CommandBarFlyout / TextCommandBarFlyout
	// (used by the TextBox edit context menu — Cut / Copy / Paste — where the user
	// originally reported the visible flash). The flyout's PrimaryCommands /
	// SecondaryCommands are logical-only at popup-open time — they have not yet been
	// reparented under the presenter's visual tree, which doesn't happen until the
	// upcoming layout pass templates the CommandBarFlyoutCommandBar. Any
	// {ThemeResource} brush resolved against the application's global active theme
	// at XAML parse time would otherwise render one frame with the wrong-theme
	// brush before the corrective theme walk converges. This test verifies the
	// command's Foreground is already correct at the synchronous return point of
	// ShowAt (the last instruction before the first measure / render).
	// ------------------------------------------------------------------
	[TestMethod]
	[RequiresFullWindow]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/480")]
	public async Task When_CommandBarFlyout_Opens_First_Time_Foreground_Should_Not_Flash_Wrong_Theme()
	{
#if HAS_UNO
		using var darkApp = ThemeHelper.UseApplicationDarkTheme();
		await TestServices.WindowHelper.WaitForIdle();
#endif

		var host = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					RequestedTheme="Light">
				<Border.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="Light">
								<SolidColorBrush x:Key="MenuItemBrush" Color="Green" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Dark">
								<SolidColorBrush x:Key="MenuItemBrush" Color="Red" />
							</ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Border.Resources>
				<Button x:Name="Owner" Content="Owner">
					<Button.Flyout>
						<CommandBarFlyout>
							<CommandBarFlyout.PrimaryCommands>
								<AppBarButton x:Name="Command" Label="Item"
										Foreground="{ThemeResource MenuItemBrush}" />
							</CommandBarFlyout.PrimaryCommands>
						</CommandBarFlyout>
					</Button.Flyout>
				</Button>
			</Border>
			""");

		var owner = (Button)host.FindName("Owner");
		var flyout = (CommandBarFlyout)owner.Flyout;
		// AppBarButton inside CommandBarFlyout.PrimaryCommands doesn't share the
		// host's namescope, so reach it through the collection directly.
		var command = (AppBarButton)flyout.PrimaryCommands[0];

		var observed = new List<(string Checkpoint, Color? Color)>();
		void Snapshot(string checkpoint)
			=> observed.Add((checkpoint, (command.Foreground as SolidColorBrush)?.Color));

		var root = new Border { Child = host };
		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForLoaded(root);

		var token = command.RegisterPropertyChangedCallback(
			Control.ForegroundProperty,
			(s, dp) => Snapshot("callback"));

		try
		{
			flyout.ShowAt(owner);
			Snapshot("after-show-sync");

			await TestServices.WindowHelper.WaitForIdle();
			Snapshot("after-first-idle");

			await TestServices.WindowHelper.WaitForLoaded(command);
			Snapshot("after-command-loaded");

			await TestServices.WindowHelper.WaitForIdle();
			Snapshot("after-second-idle");

			Assert.AreEqual(ElementTheme.Light, command.ActualTheme,
				"AppBarButton should inherit the owner's Light theme.");
			Assert.AreEqual(Colors.Green, (command.Foreground as SolidColorBrush)?.Color,
				"Final AppBarButton Foreground must be the Light sentinel (Green).");

			var visibleCheckpoints = observed.Where(o => o.Checkpoint != "callback").ToList();
			var visibleRed = visibleCheckpoints.Where(o => o.Color == Colors.Red).ToList();
			Assert.AreEqual(0, visibleRed.Count,
				$"AppBarButton Foreground was the Dark sentinel (Red) at user-visible " +
				$"checkpoint(s) {string.Join(", ", visibleRed.Select(c => c.Checkpoint))}, " +
				$"producing the visible first-open flash before the corrective theme walk " +
				$"converged. All observations (checkpoint → color): " +
				$"[{string.Join(", ", observed.Select(o => $"{o.Checkpoint}={o.Color}"))}]");
		}
		finally
		{
			command.UnregisterPropertyChangedCallback(Control.ForegroundProperty, token);
			flyout.Hide();
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
		}
	}

	// CommandBarFlyout first-open flash via COMPILED XAML — kahua-private #480.
	//
	// The XamlReader.Load test above exercises the EAGER ResourceResolver path
	// (immediateResolution=false), which the parse-time dictionary pinning fully
	// covers. Kahua ships COMPILED XAML, whose {ThemeResource} assignments take
	// the DEFERRED branch (immediateResolution=true): an unpinned
	// ThemeResourceReference resolved by the load-time _resourceBindings tree
	// walk. This test loads a compiled page (CommandBarFlyout_FirstOpen_Flash)
	// to exercise that path.
	//
	// It also inspects the element the user actually SEES: the label TextBlock
	// materialised inside the AppBarButton's template, not just the
	// AppBarButton.Foreground DP. The label resolves its (template-bound /
	// inherited) foreground when the template materialises on the first layout
	// pass — a moment the AppBarButton.Foreground-only assertion never checks.
	[TestMethod]
	[RequiresFullWindow]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/480")]
	public async Task When_CommandBarFlyout_Compiled_Opens_First_Time_Label_Should_Not_Flash_Wrong_Theme()
	{
#if HAS_UNO
		using var darkApp = ThemeHelper.UseApplicationDarkTheme();
		await TestServices.WindowHelper.WaitForIdle();

		var page = new CommandBarFlyout_FirstOpen_Flash();
		var owner = (Button)page.FindName("Owner");
		var flyout = (CommandBarFlyout)owner.Flyout;
		var command = (AppBarButton)flyout.PrimaryCommands[0];

		var observed = new List<(string Checkpoint, Color? Button, Color? Label)>();

		TextBlock FindLabel()
			=> EnumerateDescendants(command)
				.OfType<TextBlock>()
				.FirstOrDefault(tb => tb.Text == "Item");

		void Snapshot(string checkpoint)
		{
			var label = FindLabel();
			observed.Add((
				checkpoint,
				(command.Foreground as SolidColorBrush)?.Color,
				(label?.Foreground as SolidColorBrush)?.Color));
		}

		TestServices.WindowHelper.WindowContent = page;
		await TestServices.WindowHelper.WaitForLoaded(page);

		try
		{
			// Mirror the real TextBox selection/context flyout show path
			// (TextControlFlyoutHelper.ShowAt): positioned + Transient show mode,
			// which can take a different open/layout path than Standard.
			flyout.ShowAt(owner, new FlyoutShowOptions
			{
				Position = new Windows.Foundation.Point(10, 10),
				ShowMode = FlyoutShowMode.Transient,
			});
			Snapshot("after-show-sync");

			await TestServices.WindowHelper.WaitForIdle();
			Snapshot("after-first-idle");

			await TestServices.WindowHelper.WaitForLoaded(command);
			Snapshot("after-command-loaded");

			await TestServices.WindowHelper.WaitForIdle();
			Snapshot("after-second-idle");

			var label = FindLabel();
			Assert.IsNotNull(label, "AppBarButton label TextBlock should have materialised. [transient]");
			Assert.AreEqual(ElementTheme.Light, command.ActualTheme,
				"AppBarButton should inherit the owner's Light theme.");
			Assert.AreEqual(Colors.Green, (label.Foreground as SolidColorBrush)?.Color,
				"Final visible label Foreground must be the Light sentinel (Green).");

			// The label only exists from after-command-loaded onwards. Any Red at a
			// user-visible checkpoint once it exists is the first-open flash.
			var visibleRed = observed
				.Where(o => o.Label == Colors.Red)
				.ToList();
			Assert.AreEqual(0, visibleRed.Count,
				$"Visible label Foreground was the Dark sentinel (Red) at checkpoint(s) " +
				$"{string.Join(", ", visibleRed.Select(c => c.Checkpoint))}, producing the " +
				$"first-open flash. All observations (checkpoint → button/label): " +
				$"[{string.Join(", ", observed.Select(o => $"{o.Checkpoint}=btn:{o.Button}/lbl:{o.Label}"))}]");
		}
		finally
		{
			flyout.Hide();
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
		}
#else
		await Task.CompletedTask;
#endif
	}

#if HAS_UNO
	private static IEnumerable<DependencyObject> EnumerateDescendants(DependencyObject root)
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			yield return child;
			foreach (var descendant in EnumerateDescendants(child))
			{
				yield return descendant;
			}
		}
	}
#endif
}
