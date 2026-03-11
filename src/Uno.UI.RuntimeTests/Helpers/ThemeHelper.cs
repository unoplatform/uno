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

		/// <summary>
		/// Ensure dark theme is applied at the application level for the course of a single test.
		/// Unlike <see cref="UseDarkTheme"/> which sets element-level theme on the content root,
		/// this changes the application-level theme affecting all windows and root elements.
		/// </summary>
#if HAS_UNO
		public static IDisposable UseApplicationDarkTheme()
		{
			var originalTheme = Application.Current.RequestedTheme;
			Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);

			return new DisposableAction(() =>
			{
				Application.Current.SetExplicitRequestedTheme(originalTheme);
			});
		}
#endif

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
