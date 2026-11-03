#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// The automation peers of the WinUI controls library (controls/dev) return the namespace-qualified
/// runtime class name from GetClassNameCore (winrt::hstring_name_of), unlike the core (dxaml) peers.
/// </summary>
[TestClass]
public class Given_ControlsLibraryAutomationPeerClassName
{
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24992")]
	[DataRow(typeof(AnimatedVisualPlayer), "Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer")]
	[DataRow(typeof(BreadcrumbBarItem), "Microsoft.UI.Xaml.Controls.BreadcrumbBarItem")]
	[DataRow(typeof(ColorSpectrum), "Microsoft.UI.Xaml.Controls.Primitives.ColorSpectrum")]
	[DataRow(typeof(DropDownButton), "Microsoft.UI.Xaml.Controls.DropDownButton")]
	[DataRow(typeof(Expander), "Microsoft.UI.Xaml.Controls.Expander")]
	[DataRow(typeof(InfoBar), "Microsoft.UI.Xaml.Controls.InfoBar")]
	[DataRow(typeof(ItemContainer), "Microsoft.UI.Xaml.Controls.ItemContainer")]
	[DataRow(typeof(ItemsView), "Microsoft.UI.Xaml.Controls.ItemsView")]
	[DataRow(typeof(MenuBarItem), "Microsoft.UI.Xaml.Controls.MenuBarItem")]
	[DataRow(typeof(NavigationViewItem), "Microsoft.UI.Xaml.Controls.NavigationViewItem")]
	[DataRow(typeof(NumberBox), "Microsoft.UI.Xaml.Controls.NumberBox")]
	[DataRow(typeof(PagerControl), "Microsoft.UI.Xaml.Controls.PagerControl")]
	[DataRow(typeof(PersonPicture), "Microsoft.UI.Xaml.Controls.PersonPicture")]
	[DataRow(typeof(PipsPager), "Microsoft.UI.Xaml.Controls.PipsPager")]
	[DataRow(typeof(ProgressBar), "Microsoft.UI.Xaml.Controls.ProgressBar")]
	[DataRow(typeof(ProgressRing), "Microsoft.UI.Xaml.Controls.ProgressRing")]
	[DataRow(typeof(RadioButtons), "Microsoft.UI.Xaml.Controls.RadioButtons")]
	[DataRow(typeof(SelectorBarItem), "Microsoft.UI.Xaml.Controls.SelectorBarItem")]
	[DataRow(typeof(SplitButton), "Microsoft.UI.Xaml.Controls.SplitButton")]
	[DataRow(typeof(TabView), "Microsoft.UI.Xaml.Controls.TabView")]
	[DataRow(typeof(TabViewItem), "Microsoft.UI.Xaml.Controls.TabViewItem")]
	[DataRow(typeof(TeachingTip), "Microsoft.UI.Xaml.Controls.TeachingTip")]
	[DataRow(typeof(TitleBar), "Microsoft.UI.Xaml.Controls.TitleBar")]
	[DataRow(typeof(ToggleSplitButton), "Microsoft.UI.Xaml.Controls.ToggleSplitButton")]
	[DataRow(typeof(TreeViewItem), "Microsoft.UI.Xaml.Controls.TreeViewItem")]
	[DataRow(typeof(WebView2), "Microsoft.UI.Xaml.Controls.WebView2")]
	// MenuBarAutomationPeer does not override GetClassNameCore, so it inherits the empty
	// FrameworkElementAutomationPeer class name.
	[DataRow(typeof(MenuBar), "")]
	public void When_Peer_Created_Then_ClassName_Is_Namespace_Qualified(Type controlType, string expectedClassName)
	{
		var control = (UIElement)Activator.CreateInstance(controlType)!;

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);

		Assert.IsNotNull(peer, $"No automation peer for {controlType.Name}");
		Assert.AreEqual(expectedClassName, peer.GetClassName(), $"{peer.GetType().Name}.GetClassName()");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_FrameworkElementAutomationPeer_Has_No_ClassName_Then_Empty_String()
	{
		var peer = new FrameworkElementAutomationPeer(new Border());

		Assert.AreEqual(string.Empty, peer.GetClassName());
	}
}
