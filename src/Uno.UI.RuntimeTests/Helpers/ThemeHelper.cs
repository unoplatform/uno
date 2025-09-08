#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class ThemeHelper
	{
		/// <summary>
		/// Ensure dark theme is applied for the course of a single test.
		/// </summary>
		public static IDisposable UseDarkTheme()
		{
			var root = TestServices.WindowHelper.XamlRoot.Content as FrameworkElement;
			Assert.IsNotNull(root);
			var currentTheme = Application.Current.RequestedTheme;
			root.RequestedTheme = ElementTheme.Dark;

			return new DisposableAction(() =>
			{
				root.RequestedTheme = currentTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
			});
		}

		public static ElementTheme CurrentTheme
		{
			get
			{
				var root = TestServices.WindowHelper.XamlRoot.Content as FrameworkElement;
				Assert.IsNotNull(root);
				return root.ActualTheme;
			}
		}
	}
}
