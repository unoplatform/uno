using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Includes or excludes a test by drawing backend, composing with <see cref="PlatformConditionAttribute"/> — they
/// are separate axes, so a test can be scoped to a platform, a backend, or both.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class BackendConditionAttribute : ConditionBaseAttribute
{
	public BackendConditionAttribute(ConditionMode mode, RuntimeTestBackends backends)
		: base(mode)
	{
		Backends = backends;
		IgnoreMessage = mode == ConditionMode.Include
			? $"Test is only supported on the {backends} backend"
			: $"Test is skipped on the {backends} backend";
	}

	public RuntimeTestBackends Backends { get; set; }

	// Intersection rather than HasFlag: the current backend is None on the native targets, and HasFlag(None) is
	// true for every value, which would skip an excluded test everywhere.
	public override bool IsConditionMet
		=> (Backends & RuntimeTestsBackendHelper.CurrentBackend) != RuntimeTestBackends.None;

	/// <summary>
	/// Gets the group name for this attribute.
	/// </summary>
	public override string GroupName => nameof(BackendConditionAttribute);
}
