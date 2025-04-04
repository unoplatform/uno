﻿using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Tests.Helpers;
using System;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	[TestClass]
	public partial class Given_Element_Theme
	{
		[TestInitialize]
		public void Initialize()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void RequestedTheme_Default()
		{
			var border = new Border();
			Assert.AreEqual(ElementTheme.Default, border.RequestedTheme);
		}

		[TestMethod]
		public void ActualTheme_Matching_App_Theme()
		{
			var initialTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark ?
				ElementTheme.Dark : ElementTheme.Light;

			var border = new Border();
			Assert.AreEqual(initialTheme, border.ActualTheme);

			using var _ = ThemeHelper.SwapSystemTheme();

			var updatedTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark ?
				ElementTheme.Dark : ElementTheme.Light;

			Assert.AreEqual(updatedTheme, border.ActualTheme);
		}

		[TestMethod]
		public void ActualTheme_With_Explicit_RequestedTheme()
		{
			using var _ = ThemeHelper.SetExplicitRequestedTheme(ApplicationTheme.Dark);

			var border = new Border();
			Assert.AreEqual(ElementTheme.Dark, border.ActualTheme);

			border.RequestedTheme = ElementTheme.Light;
			Assert.AreEqual(ElementTheme.Light, border.ActualTheme);

			border.RequestedTheme = ElementTheme.Default;
			Assert.AreEqual(ElementTheme.Dark, border.ActualTheme);
		}

		[TestMethod]
		public void ActualThemeChanged_Called_With_RequestedTheme()
		{
			using var _ = ThemeHelper.SetExplicitRequestedTheme(ApplicationTheme.Dark);

			int callCounter = 0;
			var border = new Border();
			border.ActualThemeChanged += (s, e) =>
			{
				callCounter++;
			};

			border.RequestedTheme = ElementTheme.Dark;
			Assert.AreEqual(0, callCounter);

			border.RequestedTheme = ElementTheme.Light;
			Assert.AreEqual(1, callCounter);

			border.RequestedTheme = ElementTheme.Default;
			Assert.AreEqual(2, callCounter);
		}
	}
}
