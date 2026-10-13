#nullable enable

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Extensibility;
using Uno.UI.Runtime.Wayland.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
[DoNotParallelize]
public sealed class KeyboardInjectionTargetTests
{
	private static readonly FieldInfo _targets = typeof(InputInjector).GetField("_inputManagers", BindingFlags.Static | BindingFlags.NonPublic)!;
	private object? _previousTargets;
	private static readonly FieldInfo _registrationsField = typeof(ApiExtensibility).GetField("_registrations", BindingFlags.Static | BindingFlags.NonPublic)!;
	private Func<object, object>? _previousPolicy;
	private Dictionary<Type, Func<object, object>> Registrations => (Dictionary<Type, Func<object, object>>)_registrationsField.GetValue(null)!;

	[TestInitialize]
	public void SaveTargets()
	{
		_previousTargets = _targets.GetValue(null);
		_targets.SetValue(null, null);
		Registrations.Remove(typeof(IInputInjectorKeyboardTargetPolicy), out _previousPolicy);
	}

	[TestCleanup]
	public void RestoreTargets()
	{
		_targets.SetValue(null, _previousTargets);
		Registrations.Remove(typeof(IInputInjectorKeyboardTargetPolicy));
		if (_previousPolicy is not null)
		{
			Registrations.Add(typeof(IInputInjectorKeyboardTargetPolicy), _previousPolicy);
		}
	}

	[TestMethod]
	public void NoNativeActivationRoutesToReadyWindowInsteadOfStartupRoot()
	{
		var policy = InstallWaylandPolicy();
		var startup = new Target(active: true, ready: false);
		var window = new Target(active: false, ready: true);
		InputInjector.SetTargetForCurrentThread(startup);
		var injector = InputInjector.TryCreate()!;
		InputInjector.SetTargetForCurrentThread(window);
		policy.Activated(window);
		Tap(injector);
		Assert.AreEqual(0, startup.Keys.Count);
		Assert.AreEqual(2, window.Keys.Count);
		Assert.AreEqual(VirtualKey.A, window.Keys[0].VirtualKey);
		Assert.AreEqual(1u, window.Keys[0].KeyStatus.RepeatCount);
		Assert.IsTrue(window.Keys[1].KeyStatus.IsKeyReleased);
	}

	[TestMethod]
	public void ClosedWindowCannotStealInputWithRetainedActivation()
	{
		var closed = new Target(active: true, ready: false);
		var window = new Target(active: true, ready: true);
		InputInjector.SetTargetForCurrentThread(closed);
		InputInjector.SetTargetForCurrentThread(window);
		Tap(InputInjector.TryCreate()!);
		Assert.AreEqual(0, closed.Keys.Count);
		Assert.AreEqual(2, window.Keys.Count);
	}

	[TestMethod]
	public void NativeActiveWindowWinsOverHeadlessFallback()
	{
		var policy = InstallWaylandPolicy();
		var inactive = new Target(active: false, ready: true);
		var active = new Target(active: true, ready: true);
		policy.Focused(inactive);
		policy.NativeActivationChanged(active, active: true);
		InputInjector.SetTargetForCurrentThread(inactive);
		InputInjector.SetTargetForCurrentThread(active);
		Tap(InputInjector.TryCreate()!);
		Assert.AreEqual(0, inactive.Keys.Count);
		Assert.AreEqual(2, active.Keys.Count);
	}

	[TestMethod]
	public void HostWithoutPolicyKeepsOriginalFallback()
	{
		var first = new Target(active: false, ready: true) { IsVisibleForKeyboardInput = false };
		var second = new Target(active: false, ready: true);
		InputInjector.SetTargetForCurrentThread(first);
		InputInjector.SetTargetForCurrentThread(second);
		Tap(InputInjector.TryCreate()!);
		Assert.AreEqual(2, first.Keys.Count);
		Assert.AreEqual(0, second.Keys.Count);
	}

	[TestMethod]
	public void WaylandManagedFocusInSecondWindowWinsOverRegistrationAndActivationOrder()
	{
		var policy = InstallWaylandPolicy();
		var first = new Target(active: true, ready: true);
		var second = new Target(active: true, ready: true);
		InputInjector.SetTargetForCurrentThread(first);
		InputInjector.SetTargetForCurrentThread(second);
		policy.Activated(first);
		policy.Focused(second);
		Tap(InputInjector.TryCreate()!);
		Assert.AreEqual(0, first.Keys.Count);
		Assert.AreEqual(2, second.Keys.Count);
	}

	[TestMethod]
	public void WaylandLastActivationWinsWithoutManagedFocus()
	{
		var policy = InstallWaylandPolicy();
		var first = new Target(active: false, ready: true);
		var second = new Target(active: false, ready: true);
		InputInjector.SetTargetForCurrentThread(first);
		InputInjector.SetTargetForCurrentThread(second);
		policy.Activated(first);
		policy.Activated(second);
		Tap(InputInjector.TryCreate()!);
		Assert.AreEqual(0, first.Keys.Count);
		Assert.AreEqual(2, second.Keys.Count);
	}

	[TestMethod]
	public void HiddenOrClosedManagedFocusFallsBackToLastLiveActivation()
	{
		var policy = InstallWaylandPolicy();
		var first = new Target(active: false, ready: true);
		var second = new Target(active: false, ready: true);
		policy.Activated(first);
		policy.Activated(second);
		policy.Focused(second);
		Assert.AreSame(second, policy.GetFallbackTarget());
		second.IsVisibleForKeyboardInput = false;
		Assert.AreSame(first, policy.GetFallbackTarget());
		second.IsVisibleForKeyboardInput = true;
		second.IsReadyForKeyboardInput = false;
		Assert.AreSame(first, policy.GetFallbackTarget());
		first.IsReadyForKeyboardInput = false;
		Assert.IsNull(policy.GetFallbackTarget());
	}

	[TestMethod]
	public void RemovingWindowForgetsItsFocusActivationAndHistory()
	{
		var policy = InstallWaylandPolicy();
		var first = new Target(active: false, ready: true);
		var second = new Target(active: false, ready: true);
		policy.Activated(first);
		policy.Activated(second);
		policy.Focused(second);
		policy.NativeActivationChanged(second, active: true);
		policy.Remove(second);
		Assert.IsFalse(policy.IsActive(second));
		Assert.AreSame(first, policy.GetFallbackTarget());
		policy.Remove(first);
		Assert.IsNull(policy.GetFallbackTarget());
	}

	[TestMethod]
	public void ClearedManagedFocusUsesLastActivation()
	{
		var policy = InstallWaylandPolicy();
		var first = new Target(active: false, ready: true);
		var second = new Target(active: false, ready: true);
		policy.Activated(first);
		policy.Focused(second);
		policy.Unfocused(first);
		Assert.AreSame(second, policy.GetFallbackTarget());
		policy.Unfocused(second);
		Assert.AreSame(first, policy.GetFallbackTarget());
	}

	[TestMethod]
	public void NoPolicyCannotInjectIntoUninitializedStartupRoot()
	{
		var startup = new Target(active: true, ready: false);
		InputInjector.SetTargetForCurrentThread(startup);
		Assert.ThrowsExactly<InvalidOperationException>(() => Tap(InputInjector.TryCreate()!));
		Assert.AreEqual(0, startup.Keys.Count);
	}

	private static WaylandKeyboardInjectionPolicy InstallWaylandPolicy()
	{
		var policy = new WaylandKeyboardInjectionPolicy();
		ApiExtensibility.Register(typeof(IInputInjectorKeyboardTargetPolicy), _ => policy);
		return policy;
	}

	private static void Tap(InputInjector injector)
		=> injector.InjectKeyboardInput(new[]
		{
			new InjectedInputKeyboardInfo { VirtualKey = (ushort)VirtualKey.A },
			new InjectedInputKeyboardInfo { VirtualKey = (ushort)VirtualKey.A, KeyOptions = InjectedInputKeyOptions.KeyUp }
		});

	private sealed class Target(bool active, bool ready) : IInputInjectorTarget
	{
		internal List<KeyEventArgs> Keys { get; } = new();
		public bool IsActive => active;
		public bool IsReadyForKeyboardInput { get; set; } = ready;
		public bool IsVisibleForKeyboardInput { get; set; } = ready;
		public void InjectKeyDown(KeyEventArgs args) => Keys.Add(args);
		public void InjectKeyUp(KeyEventArgs args) => Keys.Add(args);
		public void InjectPointerAdded(PointerEventArgs args) => throw new NotSupportedException();
		public void InjectPointerUpdated(PointerEventArgs args) => throw new NotSupportedException();
		public void InjectPointerRemoved(PointerEventArgs args) => throw new NotSupportedException();
	}
}
