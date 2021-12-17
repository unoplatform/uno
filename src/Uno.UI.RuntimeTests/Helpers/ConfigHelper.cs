using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Disposables;

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class ConfigHelper
	{
		/// <summary>
		/// On Android, ensure that managed popups are used for the duration of the test. On other platforms this is a no-op.
		/// </summary>
		public static IDisposable UseManagedPopups()
		{
#if !__ANDROID__
			return null;
#else
			Assert.IsTrue(FeatureConfiguration.Popup.UseNativePopup); // If/when the default changes in SamplesApp, supplementary tests should be modified to test the new non-default
			FeatureConfiguration.Popup.UseNativePopup = false;
			return Disposable.Create(() => FeatureConfiguration.Popup.UseNativePopup = true);
#endif
		}
	}
}
