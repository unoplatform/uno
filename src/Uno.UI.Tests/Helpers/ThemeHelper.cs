using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.Helpers
{
	internal static class ThemeHelper
	{
		internal static
#if NETFX_CORE
		async
#endif
		Task<bool> SwapSystemTheme()
		{
			var currentTheme = Application.Current.RequestedTheme;
			var targetTheme = currentTheme == ApplicationTheme.Light ?
				ApplicationTheme.Dark :
				ApplicationTheme.Light;
#if NETFX_CORE
			if (!UnitTestsApp.App.EnableInteractiveTests || targetTheme == ApplicationTheme.Light)
			{
				return false;
			}

			_swapTask = _swapTask ?? GetSwapTask();

			await _swapTask;
#else
			Application.Current.SetExplicitRequestedTheme(targetTheme);
#endif
			Assert.AreEqual(targetTheme, Application.Current.RequestedTheme);

#if NETFX_CORE
			return true;
#else
			return Task.FromResult(true);
#endif
		}
	}
}
