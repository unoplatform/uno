using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class WebViewTestAttribute : ConditionalTestAttribute
{
	public WebViewTestAttribute()
	{
		IgnoredPlatforms = RuntimeTestPlatform.SkiaGtk | RuntimeTestPlatform.SkiaX11 | RuntimeTestPlatform.SkiaMacOS;
	}
}
