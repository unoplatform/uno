using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Helpers
{
	internal static class ThemeHelper
	{
		internal static IDisposable SwapSystemTheme()
		{
			var currentTheme = Application.Current.RequestedTheme;
			var targetTheme = currentTheme == ApplicationTheme.Light ?
				ApplicationTheme.Dark :
				ApplicationTheme.Light;
			var disposable = ThemeHelper.SetExplicitRequestedTheme(targetTheme);
			Assert.AreEqual(targetTheme, Application.Current.RequestedTheme);
			return disposable;
		}

		internal static IDisposable SetExplicitRequestedTheme(ApplicationTheme theme)
		{
			var existingTheme = Application.Current.RequestedTheme;
			var wasSetExplicitly = Application.Current.IsThemeSetExplicitly;
			Application.Current.SetExplicitRequestedTheme(theme);
			return new DisposableAction(() => Application.Current.SetExplicitRequestedTheme(wasSetExplicitly ? existingTheme : null));
		}
	}
}
