using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Helpers
{
	public static class FeatureConfigurationHelper
	{
		/// <summary>
		/// Enable <see cref="FrameworkTemplate"/> pooling (Uno only) for the duration of a single test.
		/// </summary>
		public static IDisposable UseTemplatePooling()
		{
#if NETFX_CORE
			return null;
#else
			var originallyEnabled = FrameworkTemplatePool.IsPoolingEnabled;
			FrameworkTemplatePool.IsPoolingEnabled = true;
			return Disposable.Create(() => FrameworkTemplatePool.IsPoolingEnabled = originallyEnabled); 
#endif
		}
	}
}
