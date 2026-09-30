#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Islands;
using Windows.UI;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Window_SystemBackdrop
{
	private const string PageBackgroundKey = "ApplicationPageBackgroundThemeBrush";

	[TestInitialize]
	public void Initialize() => UnitTestsApp.App.EnsureApplication();

	[TestMethod]
	public void When_Head_Renders_No_Material_Then_Root_Keeps_Themed_Background()
	{
		var window = ((UnitTestsApp.App)Application.Current).MainWindow;
		var previousBackdrop = window.SystemBackdrop;

		try
		{
			window.SystemBackdrop = new MicaBackdrop();

			// The unit-test host's wrapper inherits the no-op SetSystemBackdrop, so no material is
			// ever drawn - even on a Windows 11 build where MicaController.IsSupported() says true.
			// Asking the OS instead of the head is what used to leave this window transparent over
			// nothing.
			Assert.IsFalse(window.HasSupportedSystemBackdrop);

			var root = GetRoot(window);
			Assert.IsFalse(root.HasTransparentBackground);
			Assert.AreEqual(BackdropBackgroundMode.Fallback, root.BackdropBackground);

			var background = root.Background as SolidColorBrush;
			Assert.IsNotNull(background);
			Assert.AreEqual(255, background.Color.A, "The fallback background must stay opaque.");

			// The rendered fallback is the window background, not the root visual's flat black/white.
			var expected = Uno.UI.ResourceResolver.ResolveTopLevelResource(PageBackgroundKey, null) as SolidColorBrush;
			Assert.IsNotNull(expected);
			Assert.AreEqual(expected.Color, GetRenderedBackground(root));
		}
		finally
		{
			window.SystemBackdrop = previousBackdrop;
		}
	}

	[TestMethod]
	public void When_Content_Theme_Differs_Then_Fallback_Follows_Content()
	{
		var app = (UnitTestsApp.App)Application.Current;
		var window = app.MainWindow;
		var previousBackdrop = window.SystemBackdrop;
		var content = app.HostView;
		var contentTheme = OppositeOf(content.ActualTheme);

		try
		{
			window.SystemBackdrop = new MicaBackdrop();
			var root = GetRoot(window);

			SetRequestedTheme(content, contentTheme);

			Assert.AreEqual(GetPageBackground(contentTheme), GetRenderedBackground(root));

			SetRequestedTheme(content, OppositeOf(contentTheme));

			Assert.AreEqual(GetPageBackground(OppositeOf(contentTheme)), GetRenderedBackground(root));
		}
		finally
		{
			SetRequestedTheme(content, ElementTheme.Default);
			window.SystemBackdrop = previousBackdrop;
		}
	}

	[TestMethod]
	public void When_Content_Replaced_Then_Fallback_Follows_New_Content()
	{
		var app = (UnitTestsApp.App)Application.Current;
		var window = app.MainWindow;
		var previousBackdrop = window.SystemBackdrop;
		var previousContent = window.Content;
		var newTheme = OppositeOf(app.HostView.ActualTheme);

		try
		{
			window.SystemBackdrop = new MicaBackdrop();
			var root = GetRoot(window);

			window.Content = new Grid { RequestedTheme = newTheme };

			Assert.AreEqual(GetPageBackground(newTheme), GetRenderedBackground(root));
		}
		finally
		{
			window.Content = previousContent;
			window.SystemBackdrop = previousBackdrop;
		}
	}

	/// <summary>
	/// ActualThemeChanged is posted to the dispatcher, as in WinUI, so handlers see the persisted theme.
	/// The unit-test dispatcher runs work inline, so defer it until the theme walk is done.
	/// </summary>
	private static void SetRequestedTheme(FrameworkElement element, ElementTheme theme)
	{
		var previousDispatch = NativeDispatcher.DispatchOverride;
		List<Action> deferred = new();
		NativeDispatcher.DispatchOverride = (action, _) => deferred.Add(action);
		try
		{
			element.RequestedTheme = theme;
		}
		finally
		{
			NativeDispatcher.DispatchOverride = previousDispatch;
		}

		foreach (var action in deferred)
		{
			action();
		}
	}

	private static XamlIslandRoot GetRoot(Window window)
		=> (XamlIslandRoot)window.Content!.XamlRoot!.VisualTree.RootElement!;

	private static Color? GetRenderedBackground(XamlIslandRoot root)
		=> (((IBorderInfoProvider)root).BorderVisual.BackgroundBrush as CompositionColorBrush)?.Color;

	private static Color GetPageBackground(ElementTheme theme)
	{
		var brush = Uno.UI.Xaml.Core.CoreServices.Instance.LookupThemeResource(
			theme == ElementTheme.Dark ? Theme.Dark : Theme.Light,
			PageBackgroundKey) as SolidColorBrush;
		Assert.IsNotNull(brush);
		return brush.Color;
	}

	private static ElementTheme OppositeOf(ElementTheme theme)
		=> theme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
}
