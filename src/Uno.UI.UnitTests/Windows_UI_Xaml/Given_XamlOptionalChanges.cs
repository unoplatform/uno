#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[UITestClass]
public class Given_XamlOptionalChanges
{
	private static readonly XamlChangeId[] _changes =
	[
		XamlChangeId.IconNoGridOptimization,
		XamlChangeId.OptimizeApplyStyles,
		XamlChangeId.DefaultStyleOptimizations,
		XamlChangeId.DeferContextFlyoutInit,
	];

	private bool[] _previousStates = [];
	private bool _wasLocked;

	[TestInitialize]
	public void Init()
	{
		_previousStates = _changes.Select(XamlOptionalChanges.IsChangeEnabled).ToArray();
		_wasLocked = XamlOptionalChanges.IsLocked();
		XamlOptionalChanges.ResetInternal();
	}

	[TestCleanup]
	public void Cleanup()
	{
		XamlOptionalChanges.ResetInternal();
		for (var i = 0; i < _changes.Length; i++)
		{
			XamlOptionalChanges.SetChangeEnabledForTesting(_changes[i], _previousStates[i]);
		}

		if (_wasLocked)
		{
			XamlOptionalChanges.LockInternal();
		}
	}

	[TestMethod]
	public void When_Default_Then_All_Changes_Disabled()
	{
		foreach (var change in _changes)
		{
			Assert.IsFalse(XamlOptionalChanges.IsChangeEnabled(change), change.ToString());
		}

		Assert.IsFalse(XamlOptionalChanges.IsLocked());
	}

	[TestMethod]
	public void When_EnableChange_Then_Enabled()
	{
		Assert.IsTrue(XamlOptionalChanges.EnableChange(XamlChangeId.DefaultStyleOptimizations));

		Assert.IsTrue(XamlOptionalChanges.IsChangeEnabled(XamlChangeId.DefaultStyleOptimizations));
		Assert.IsTrue(XamlOptionalChanges.AreOptimizedStylesEnabled);
		Assert.IsFalse(XamlOptionalChanges.IsChangeEnabled(XamlChangeId.OptimizeApplyStyles));

		Assert.IsTrue(XamlOptionalChanges.DisableChange(XamlChangeId.DefaultStyleOptimizations));
		Assert.IsFalse(XamlOptionalChanges.IsChangeEnabled(XamlChangeId.DefaultStyleOptimizations));
	}

	[TestMethod]
	public void When_Unknown_Change_Then_Not_Mutated()
	{
		Assert.IsFalse(XamlOptionalChanges.EnableChange((XamlChangeId)42));
		Assert.IsFalse(XamlOptionalChanges.IsChangeEnabled((XamlChangeId)42));
		Assert.IsFalse(XamlOptionalChanges.EnableChange(XamlChangeId._Reserved));
	}

	[TestMethod]
	public void When_Locked_Then_Changes_Throw()
	{
		XamlOptionalChanges.EnableChange(XamlChangeId.IconNoGridOptimization);

		Assert.IsTrue(XamlOptionalChanges.Lock());
		Assert.IsFalse(XamlOptionalChanges.Lock(), "Only the first call performs the lock.");
		Assert.IsTrue(XamlOptionalChanges.IsLocked());

		Assert.ThrowsExactly<InvalidOperationException>(() => XamlOptionalChanges.EnableChange(XamlChangeId.OptimizeApplyStyles));
		Assert.ThrowsExactly<InvalidOperationException>(() => XamlOptionalChanges.DisableChange(XamlChangeId.IconNoGridOptimization));
		Assert.ThrowsExactly<InvalidOperationException>(() => XamlOptionalChanges.EnableChange((XamlChangeId)42));

		Assert.IsTrue(XamlOptionalChanges.IsChangeEnabled(XamlChangeId.IconNoGridOptimization));
		Assert.IsFalse(XamlOptionalChanges.IsChangeEnabled(XamlChangeId.OptimizeApplyStyles));
	}
}
