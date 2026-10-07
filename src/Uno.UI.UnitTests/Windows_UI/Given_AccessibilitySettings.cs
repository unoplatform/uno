#nullable enable

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Helpers.Theming;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Tests.Windows_UI;

[TestClass]
public class Given_AccessibilitySettings
{
	private static readonly FieldInfo _changesObservedField =
		typeof(SystemThemeHelper).GetField("_changesObserved", BindingFlags.Static | BindingFlags.NonPublic)!;

	private static bool IsObservingThemeChanges => (int)_changesObservedField.GetValue(null)! != 0;

	[TestMethod]
	public void When_HighContrastChanged_Subscribed_Then_Theme_Changes_Observed()
	{
		// Another test may already have started observation; reset so this subscription is the trigger.
		_changesObservedField.SetValue(null, 0);
		Assert.IsFalse(IsObservingThemeChanges);

		AccessibilitySettings settings = new();
		TypedEventHandler<AccessibilitySettings, object> handler = (s, e) => { };

		settings.HighContrastChanged += handler;
		try
		{
			Assert.IsTrue(IsObservingThemeChanges);
		}
		finally
		{
			settings.HighContrastChanged -= handler;
		}
	}

	[TestMethod]
	public void When_HighContrast_Notified_Then_Subscriber_Raised()
	{
		AccessibilitySettings settings = new();
		var raised = 0;
		AccessibilitySettings? sender = null;
		TypedEventHandler<AccessibilitySettings, object> handler = (s, e) =>
		{
			raised++;
			sender = s;
		};

		settings.HighContrastChanged += handler;
		try
		{
			AccessibilitySettings.OnHighContrastChanged();
		}
		finally
		{
			settings.HighContrastChanged -= handler;
		}

		Assert.AreEqual(1, raised);
		Assert.AreSame(settings, sender);
	}

	[TestMethod]
	public void When_HighContrastChanged_Unsubscribed_Then_Not_Raised()
	{
		AccessibilitySettings settings = new();
		var raised = 0;
		TypedEventHandler<AccessibilitySettings, object> handler = (s, e) => raised++;

		settings.HighContrastChanged += handler;
		settings.HighContrastChanged -= handler;
		AccessibilitySettings.OnHighContrastChanged();

		Assert.AreEqual(0, raised);
	}
}
