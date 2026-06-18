using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Core;

// Not named ….Theming: a child namespace called "Theming" would shadow the Microsoft.UI.Xaml.Theming
// static class for every test in Uno.UI.Tests.Windows_UI_Xaml.
namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkThemingTests;

[TestClass]
public class Given_FrameworkTheming
{
	// Fake IThemingInterop driving FrameworkTheming without any OS dependency.
	private sealed class FakeThemingInterop : IThemingInterop
	{
		public Theme SystemTheme { get; set; } = Theme.Light;
		public Theme HighContrastTheme { get; set; } = Theme.HighContrastNone;
		// Matches FrameworkTheming's initial m_accentColor (0x00000000) so that creating the
		// instance and calling OnThemeChanged with no theme change is a true no-op.
		public uint AccentColor { get; set; }

		public Theme GetSystemTheme() => SystemTheme;
		public Theme GetSystemHighContrastTheme() => HighContrastTheme;
		public uint GetSystemColor(int colorId) => 0xFF000000;
		public uint GetSystemAccentColor() => AccentColor;
		public uint GetSystemVariantAccentColor(IThemingInterop.VariantAccentColors colorIndex) => AccentColor;
	}

	private static (FrameworkTheming Theming, FakeThemingInterop Interop, List<(bool BaseChanging, bool HighContrastChanging)> Notifications) Create(
		Theme systemTheme = Theme.Light)
	{
		var interop = new FakeThemingInterop { SystemTheme = systemTheme };
		var notifications = new List<(bool, bool)>();
		FrameworkTheming theming = null!;
		// The notify callback observes the changing flags exactly while it runs (like
		// CCoreServices::NotifyThemeChange does in WinUI).
		theming = new FrameworkTheming(interop, () => notifications.Add((theming.IsBaseThemeChanging(), theming.IsHighContrastChanging())));
		return (theming, interop, notifications);
	}

	#region (a) GetTheme composition — (requested ?: system) | highContrast (FrameworkTheming.cpp:119-124)

	[TestMethod]
	public void When_RequestedTheme_None_Follows_SystemTheme()
	{
		var (theming, interop, _) = Create(systemTheme: Theme.Light);

		// The constructor reads the system theme from the interop.
		Assert.AreEqual(Theme.Light, theming.GetTheme());
		Assert.AreEqual(Theme.Light, theming.GetBaseTheme());
		Assert.AreEqual(Theme.HighContrastNone, theming.GetHighContrastTheme());

		interop.SystemTheme = Theme.Dark;
		theming.OnThemeChanged();

		Assert.AreEqual(Theme.Dark, theming.GetTheme());
		Assert.AreEqual(Theme.Dark, theming.GetBaseTheme());
	}

	[TestMethod]
	public void When_RequestedTheme_Set_Overrides_SystemTheme()
	{
		var (theming, interop, _) = Create(systemTheme: Theme.Dark);

		theming.SetRequestedTheme(Theme.Light);

		Assert.AreEqual(Theme.Light, theming.GetTheme());

		// The requested theme keeps winning even when the system theme changes.
		interop.SystemTheme = Theme.Light;
		theming.OnThemeChanged();
		interop.SystemTheme = Theme.Dark;
		theming.OnThemeChanged();

		Assert.AreEqual(Theme.Light, theming.GetTheme());
	}

	[TestMethod]
	public void When_HighContrast_Is_ORed_Onto_System_Theme()
	{
		var (theming, interop, _) = Create(systemTheme: Theme.Light);

		interop.HighContrastTheme = Theme.HighContrast;
		theming.OnThemeChanged();

		Assert.AreEqual(Theme.Light | Theme.HighContrast, theming.GetTheme());
		Assert.AreEqual(Theme.Light, theming.GetBaseTheme());
		Assert.AreEqual(Theme.HighContrast, theming.GetHighContrastTheme());
		Assert.IsTrue(theming.HasHighContrastTheme());
	}

	[TestMethod]
	public void When_HighContrast_Is_ORed_Onto_Requested_Theme()
	{
		var (theming, interop, notifications) = Create(systemTheme: Theme.Light);

		interop.HighContrastTheme = Theme.HighContrastBlack;
		theming.OnThemeChanged();
		var notificationCount = notifications.Count;

		// "Don't notify about theme switches when we're in high contrast." — the requested theme
		// is still recorded and composed, but no notification is raised.
		theming.SetRequestedTheme(Theme.Dark);

		Assert.AreEqual(Theme.Dark | Theme.HighContrastBlack, theming.GetTheme());
		Assert.AreEqual(Theme.Dark, theming.GetBaseTheme());
		Assert.AreEqual(Theme.HighContrastBlack, theming.GetHighContrastTheme());
		Assert.AreEqual(notificationCount, notifications.Count);
	}

	#endregion

	#region (b) HasRequestedTheme / UnsetRequestedTheme

	[TestMethod]
	public void When_RequestedTheme_Set_And_Unset()
	{
		var (theming, _, notifications) = Create(systemTheme: Theme.Dark);

		Assert.IsFalse(theming.HasRequestedTheme());

		theming.SetRequestedTheme(Theme.Light);

		Assert.IsTrue(theming.HasRequestedTheme());
		Assert.AreEqual(Theme.Light, theming.GetTheme());

		var notificationCount = notifications.Count;
		theming.UnsetRequestedTheme();

		// UnsetRequestedTheme only resets the field — it does not notify (FrameworkTheming.cpp:337-340).
		Assert.IsFalse(theming.HasRequestedTheme());
		Assert.AreEqual(Theme.Dark, theming.GetTheme());
		Assert.AreEqual(notificationCount, notifications.Count);
	}

	[TestMethod]
	public void When_ClearRequestedTheme_Base_Flips_Notifies_With_BaseChanging_Flag()
	{
		// Uno-specific: Application.SetExplicitRequestedTheme(null) returns the app to
		// follow-the-system mode through ClearRequestedTheme, which must walk (notify) when dropping
		// the override changes the effective base theme.
		var (theming, _, notifications) = Create(systemTheme: Theme.Dark);

		theming.SetRequestedTheme(Theme.Light);
		var notificationCount = notifications.Count;

		theming.ClearRequestedTheme();

		Assert.IsFalse(theming.HasRequestedTheme());
		Assert.AreEqual(Theme.Dark, theming.GetTheme());
		Assert.AreEqual(notificationCount + 1, notifications.Count);
		// The app-theme axis is what changed (the override was dropped), observed by the callback.
		Assert.AreEqual((true, false), notifications[^1]);
		Assert.IsFalse(theming.IsBaseThemeChanging());
	}

	[TestMethod]
	public void When_ClearRequestedTheme_Base_Unchanged_Does_Not_Notify()
	{
		var (theming, _, notifications) = Create(systemTheme: Theme.Light);

		// Override matches the system theme, so dropping it does not change the effective theme.
		theming.SetRequestedTheme(Theme.Light);
		var notificationCount = notifications.Count;

		theming.ClearRequestedTheme();

		Assert.IsFalse(theming.HasRequestedTheme());
		Assert.AreEqual(Theme.Light, theming.GetTheme());
		Assert.AreEqual(notificationCount, notifications.Count);
	}

	#endregion

	#region (c) OnThemeChanged — interop refresh + changing flags around the notify callback

	[TestMethod]
	public void When_SystemTheme_Changes_Notifies_With_BaseChanging_Flag()
	{
		var (theming, interop, notifications) = Create(systemTheme: Theme.Light);

		interop.SystemTheme = Theme.Dark;
		theming.OnThemeChanged();

		Assert.AreEqual(1, notifications.Count);
		Assert.AreEqual((true, false), notifications[0]);
		// The scope guard clears the flags once the notification completes.
		Assert.IsFalse(theming.IsBaseThemeChanging());
		Assert.IsFalse(theming.IsHighContrastChanging());
		Assert.AreEqual(Theme.Dark, theming.GetTheme());
	}

	[TestMethod]
	public void When_HighContrast_Changes_Notifies_With_HighContrastChanging_Flag()
	{
		var (theming, interop, notifications) = Create(systemTheme: Theme.Light);

		interop.HighContrastTheme = Theme.HighContrast;
		theming.OnThemeChanged();

		Assert.AreEqual(1, notifications.Count);
		Assert.AreEqual((false, true), notifications[0]);
		Assert.IsFalse(theming.IsBaseThemeChanging());
		Assert.IsFalse(theming.IsHighContrastChanging());

		interop.HighContrastTheme = Theme.HighContrastNone;
		theming.OnThemeChanged();

		Assert.AreEqual(2, notifications.Count);
		Assert.AreEqual((false, true), notifications[1]);
		Assert.IsFalse(theming.HasHighContrastTheme());
	}

	[TestMethod]
	public void When_Nothing_Changed_OnThemeChanged_Does_Not_Notify_Unless_Forced()
	{
		var (theming, _, notifications) = Create(systemTheme: Theme.Light);

		theming.OnThemeChanged();

		Assert.AreEqual(0, notifications.Count);

		theming.OnThemeChanged(forceUpdate: true);

		Assert.AreEqual(1, notifications.Count);
		// Nothing actually changed on either axis, so neither flag is set during the forced notify.
		Assert.AreEqual((false, false), notifications[0]);
	}

	[TestMethod]
	public void When_AccentColor_Changes_Notifies()
	{
		var (theming, interop, notifications) = Create(systemTheme: Theme.Light);

		interop.AccentColor = 0xFF123456;
		theming.OnThemeChanged();

		Assert.AreEqual(1, notifications.Count);
		Assert.AreEqual((false, false), notifications[0]);
	}

	#endregion

	#region (d) SetRequestedTheme — no-op when unchanged, triggers OnThemeChanged when changed

	[TestMethod]
	public void When_SetRequestedTheme_Unchanged_NoOps()
	{
		var (theming, _, notifications) = Create(systemTheme: Theme.Light);

		theming.SetRequestedTheme(Theme.Dark);

		Assert.AreEqual(1, notifications.Count);

		theming.SetRequestedTheme(Theme.Dark);

		Assert.AreEqual(1, notifications.Count);
	}

	[TestMethod]
	public void When_SetRequestedTheme_Changed_Triggers_OnThemeChanged()
	{
		var (theming, _, notifications) = Create(systemTheme: Theme.Dark);

		theming.SetRequestedTheme(Theme.Light);

		Assert.AreEqual(1, notifications.Count);
		// The app theme is changing (Dark => Light), so IsBaseThemeChanging is observed by the callback.
		Assert.AreEqual((true, false), notifications[0]);
		Assert.IsFalse(theming.IsBaseThemeChanging());
		Assert.AreEqual(Theme.Light, theming.GetTheme());
	}

	[TestMethod]
	public void When_SetRequestedTheme_Matches_SystemTheme_Still_Notifies_Forced()
	{
		var (theming, _, notifications) = Create(systemTheme: Theme.Light);

		// Setting a requested theme forces the update even though the effective theme is unchanged
		// (FrameworkTheming.cpp:90-96), but the base theme is not changing so the flag stays false.
		theming.SetRequestedTheme(Theme.Light);

		Assert.AreEqual(1, notifications.Count);
		Assert.AreEqual((false, false), notifications[0]);
	}

	[TestMethod]
	public void When_SetRequestedTheme_With_DoNotifyThemeChange_False_Does_Not_Notify()
	{
		var (theming, _, notifications) = Create(systemTheme: Theme.Dark);

		theming.SetRequestedTheme(Theme.Light, doNotifyThemeChange: false);

		Assert.AreEqual(0, notifications.Count);
		Assert.AreEqual(Theme.Light, theming.GetTheme());
	}

	[TestMethod]
	public void When_SetRequestedTheme_ApplicationTheme_Overload_Maps_To_Base_Theme()
	{
		var (theming, _, notifications) = Create(systemTheme: Theme.Dark);

		theming.SetRequestedTheme(ApplicationTheme.Light);

		Assert.IsTrue(theming.HasRequestedTheme());
		Assert.AreEqual(Theme.Light, theming.GetTheme());
		Assert.AreEqual(1, notifications.Count);

		theming.SetRequestedTheme(ApplicationTheme.Dark);

		Assert.AreEqual(Theme.Dark, theming.GetTheme());
		Assert.AreEqual(2, notifications.Count);
	}

	#endregion
}
