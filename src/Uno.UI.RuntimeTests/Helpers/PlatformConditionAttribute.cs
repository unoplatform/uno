using System;
using System.Linq;
using Uno.UI.RuntimeTests.Helpers;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PlatformConditionAttribute : ConditionBaseAttribute
{
	public PlatformConditionAttribute(ConditionMode mode, RuntimeTestPlatforms platforms)
		: base(mode)
	{
		Platforms = platforms;
		IgnoreMessage = mode == ConditionMode.Include
			? $"Test is only supported on {platforms}"
			: $"Test is skipped on {platforms}";
	}

	public RuntimeTestPlatforms Platforms { get; set; }

	public override bool IsConditionMet => Platforms.HasFlag(RuntimeTestsPlatformHelper.CurrentPlatform);

	/// <summary>
	/// Gets the group name for this attribute.
	/// </summary>
	public override string GroupName => nameof(PlatformConditionAttribute);
}
