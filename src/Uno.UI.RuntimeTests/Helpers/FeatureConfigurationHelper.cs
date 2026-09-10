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
		/// Enables or disables the Grid-less <see cref="Microsoft.UI.Xaml.Controls.IconElement"/> visual tree
		/// (Uno only) for the duration of a single test.
		/// </summary>
		public static IDisposable UseIconElementNoGridContainer(bool enabled)
		{
#if WINAPPSDK
			return null;
#else
			var originalSetting = FeatureConfiguration.Perf2026.IconElementNoGridContainer;
			FeatureConfiguration.Perf2026.IconElementNoGridContainer = enabled;
			return Disposable.Create(() => FeatureConfiguration.Perf2026.IconElementNoGridContainer = originalSetting);
#endif
		}
	}
}
