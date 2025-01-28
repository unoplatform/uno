using System;
using System.Linq;
using Uno.UI.RuntimeTests.Helpers;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;



[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public partial class ConditionalTestAttribute : TestMethodAttribute
{
	public RuntimeTestPlatforms IgnoredPlatforms { get; set; }

	public bool ShouldRun() => !IgnoredPlatforms.HasFlag(RuntimeTestsPlatformHelper.CurrentPlatform);
}
