using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class FeatureConfigurationHelper
	{
#if !WINAPPSDK
		private class MockProvider : FrameworkTemplatePoolDefaultPlatformProvider
		{
			public override bool CanUseMemoryManager => false;
		}
#endif

		/// <summary>
		/// Enable <see cref="FrameworkTemplate"/> pooling (Uno only) for the duration of a single test.
		/// </summary>
		public static IDisposable UseTemplatePooling()
		{
#if WINAPPSDK
			return null;
#else
			var originallyEnabled = FrameworkTemplatePool.InternalIsPoolingEnabled;
			FrameworkTemplatePool.InternalIsPoolingEnabled = true;
			FrameworkTemplatePool.Instance.SetPlatformProvider(new MockProvider());
			return Disposable.Create(() =>
			{
				FrameworkTemplatePool.InternalIsPoolingEnabled = originallyEnabled;
				FrameworkTemplatePool.Instance.SetPlatformProvider(null);
			});
#endif
		}

		/// <summary>
		/// Enables or disables a <see cref="Microsoft.UI.Xaml.Settings.XamlOptionalChanges"/> change for the duration of
		/// a single test, bypassing the lock taken when the app started.
		/// </summary>
		public static IDisposable UseOptionalChange(Microsoft.UI.Xaml.Settings.XamlChangeId changeId, bool enabled)
		{
#if WINAPPSDK
			return null;
#else
			var previous = Microsoft.UI.Xaml.Settings.XamlOptionalChanges.SetChangeEnabledForTesting(changeId, enabled);
			return Disposable.Create(() => Microsoft.UI.Xaml.Settings.XamlOptionalChanges.SetChangeEnabledForTesting(changeId, previous));
#endif
		}
	}
}
