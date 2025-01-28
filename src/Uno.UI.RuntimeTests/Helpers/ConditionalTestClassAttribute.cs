using System;
using Uno.UI.RuntimeTests.Helpers;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public partial class ConditionalTestClassAttribute : TestClassAttribute
{
	public RuntimeTestPlatforms IgnoredPlatforms { get; set; }

	public bool ShouldRun() => !IgnoredPlatforms.HasFlag(RuntimeTestsPlatformHelper.CurrentPlatform);
}
