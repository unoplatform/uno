#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.Buffers;
using Windows.Graphics.Capture;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI;

[TestClass]
public partial class Given_WeakEventHelper
{
	[TestMethod]
	public void When_Explicit_Dispose()
	{
		WeakEventHelper.WeakEventCollection SUT = new();

		var invoked = 0;
		Action action = () => invoked++;

		var disposable = WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke());

		SUT.Invoke(this, null);

		Assert.AreEqual(1, invoked);

		disposable.Dispose();

		// When disposed invoking events won't call the original action
		// the registration has been disposed.
		SUT.Invoke(this, null);

		Assert.AreEqual(1, invoked);
	}

	[TestMethod]
	public void When_Registration_Collected()
	{
		WeakEventHelper.WeakEventCollection SUT = new();

		var invoked = 0;
		Action action = () => invoked++;

		void Do()
		{
			var disposable = WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke());

			SUT.Invoke(this, null);

			Assert.AreEqual(1, invoked);

			disposable = null;
		}

		Do();

		GC.Collect(2);
		GC.WaitForPendingFinalizers();

		// Even if the disposable is collected, the event should still be invoked
		// as the disposable does not track the event registration.
		SUT.Invoke(this, null);

		Assert.AreEqual(2, invoked);
	}

	[TestMethod]
	public void When_Target_Collected()
	{
		WeakEventHelper.WeakEventCollection SUT = new();

		var invoked = 0;
		IDisposable disposable = null;

		void Do()
		{
			Action action = () => invoked++;

			disposable = WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke());

			SUT.Invoke(this, null);

			Assert.AreEqual(1, invoked);
		}

		Do();

		GC.Collect(2);
		GC.WaitForPendingFinalizers();

		SUT.Invoke(this, null);

		Assert.AreEqual(2, invoked);

		disposable.Dispose();
		disposable = null;

		GC.Collect(2);
		GC.WaitForPendingFinalizers();

		SUT.Invoke(this, null);

		Assert.AreEqual(2, invoked);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_UIElement_Target_Collected()
	{
		WeakEventHelper.WeakEventCollection SUT = new();

		var invoked = 0;
		IDisposable disposable = null;

		void Do()
		{
			Action action = () => invoked++;

			// Wrapping the action and registering the one on the target
			// allows for the WeakEventHelper to check for collection native
			// objects on android.
			MyCollectibleObject target = new(action);

			disposable = WeakEventHelper.RegisterEvent(SUT, target.MyAction, (s, e, a) => (s as Action).Invoke());

			SUT.Invoke(this, null);

			Assert.AreEqual(1, invoked);
		}

		Do();

		GC.Collect(2);
		GC.WaitForPendingFinalizers();

		SUT.Invoke(this, null);

		Assert.AreEqual(2, invoked);

		disposable.Dispose();
		disposable = null;

		GC.Collect(2);
		GC.WaitForPendingFinalizers();

		SUT.Invoke(this, null);

		Assert.AreEqual(2, invoked);
	}

	[TestMethod]
	public void When_Many_Targets_Collected()
	{
		WeakEventHelper.WeakEventCollection SUT = new();

		var invoked = 0;
		List<IDisposable> disposable = new();

		void Do()
		{
			Action action = () => invoked++;

			disposable.Add(WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke()));

			SUT.Invoke(this, null);
		}

		for (int i = 0; i < 100; i++)
		{
			Do();
		}

		SUT.Invoke(this, null);

		Assert.IsGreaterThanOrEqualTo(5150, invoked);

		disposable.Clear();

		GC.Collect(2);
		GC.WaitForPendingFinalizers();

		// Ensure that everything has been collected.
		SUT.Invoke(this, null);

		Assert.IsGreaterThanOrEqualTo(5150, invoked);
	}

	[TestMethod]
	public void When_Collection_Disposed()
	{
		WeakEventHelper.WeakEventCollection SUT = new();

		var invoked = 0;

		Action action = () => invoked++;

		var disposable = WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke());

		SUT.Invoke(this, null);

		Assert.AreEqual(1, invoked);

		SUT.Dispose();
	}

	[TestMethod]
	public async Task When_Collection_Collected()
	{
		WeakReference actionRef = null;
		WeakReference collectionRef = null;

		void Do()
		{
			WeakEventHelper.WeakEventCollection SUT = new();
			collectionRef = new(SUT);

			var invoked = 0;

			Action action = () => invoked++;
			actionRef = new(actionRef);

			var disposable = WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke());

			SUT.Invoke(this, null);

			Assert.AreEqual(1, invoked);

			SUT.Dispose();
		}

		Do();

		var sw = Stopwatch.StartNew();

		while ((actionRef.IsAlive || collectionRef.IsAlive) && sw.ElapsedMilliseconds < 5000)
		{
			await Task.Delay(10);
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
		}

		Assert.IsFalse(actionRef.IsAlive);
		Assert.IsFalse(collectionRef.IsAlive);
	}

	[TestMethod]
	public void When_Empty_Trim_Stops()
	{
		TestPlatformProvider trimProvider = new();
		WeakEventHelper.WeakEventCollection SUT = new(trimProvider);

		var invoked = 0;

		Action action = () => invoked++;

		Assert.IsNull(trimProvider.Invoke());

		var disposable = WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke());

		Assert.IsTrue(trimProvider.Invoke());

		SUT.Invoke(this, null);

		Assert.AreEqual(1, invoked);

		disposable.Dispose();

		Assert.IsFalse(trimProvider.Invoke());

		Assert.AreEqual(1, invoked);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25590")]
	public async Task When_First_Subscriber_Not_Referenced_Then_Collected()
	{
		// Keeps the trim callback strongly referenced, as Gen2GcCallback does.
		TestPlatformProvider trimProvider = new();
		WeakEventHelper.WeakEventCollection SUT = new(trimProvider);

		// The first registration on an empty collection is the one that registers the trim callback.
		var firstRef = CreateSubscriber(SUT, trackResurrection: false);
		var second = new Subscriber(SUT);

		await AssertCollected(firstRef);

		GC.KeepAlive(second);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25590")]
	public async Task When_First_Subscriber_Not_Referenced_Then_Collected_With_Default_Trim_Provider()
	{
		WeakEventHelper.WeakEventCollection SUT = new();

		// What only Gen2GcCallback references survives each GC through its finalizer, but short weak
		// references to it are cleared anyway, so only a long weak reference shows the leak.
		var firstRef = CreateSubscriber(SUT, trackResurrection: true);
		var second = new Subscriber(SUT);

		await AssertCollected(firstRef);

		GC.KeepAlive(second);
	}

	[TestMethod]
	public void When_Registered_Again_Before_Trim_Then_Single_Trim_Callback()
	{
		SimulatedGen2GcTrimProvider trimProvider = new();
		WeakEventHelper.WeakEventCollection SUT = new(trimProvider);

		Action action = () => { };

		WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke()).Dispose();

		// The collection went empty, but its trim callback has not run since and is still active.
		var disposable = WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke());

		Assert.AreEqual(1, trimProvider.ActiveCallbacks);

		trimProvider.Collect();

		Assert.AreEqual(1, trimProvider.ActiveCallbacks);

		disposable.Dispose();
		trimProvider.Collect();

		Assert.AreEqual(0, trimProvider.ActiveCallbacks);
	}

	[TestMethod]
	public void When_Registered_After_Trim_Stopped_Then_Trim_Callback_Registered()
	{
		SimulatedGen2GcTrimProvider trimProvider = new();
		WeakEventHelper.WeakEventCollection SUT = new(trimProvider);

		Action action = () => { };

		WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke()).Dispose();
		trimProvider.Collect();

		Assert.AreEqual(0, trimProvider.ActiveCallbacks);

		var disposable = WeakEventHelper.RegisterEvent(SUT, action, (s, e, a) => (s as Action).Invoke());

		Assert.AreEqual(1, trimProvider.ActiveCallbacks);

		trimProvider.Collect();

		Assert.AreEqual(1, trimProvider.ActiveCallbacks);

		disposable.Dispose();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateSubscriber(WeakEventHelper.WeakEventCollection source, bool trackResurrection)
		=> new(new Subscriber(source), trackResurrection);

	private static async Task AssertCollected(WeakReference reference)
	{
		var sw = Stopwatch.StartNew();

		while (reference.IsAlive && sw.ElapsedMilliseconds < 5000)
		{
			await Task.Delay(10);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		Assert.IsFalse(reference.IsAlive, "The subscriber should have been collected.");
	}

	private class Subscriber
	{
		public Subscriber(WeakEventHelper.WeakEventCollection source)
		{
			// Like the Brush.SetupBrushChanged callers, the subscriber owns its registration and never disposes it.
			Registration = WeakEventHelper.RegisterEvent(source, new Action(OnRaised), (s, e, a) => (s as Action).Invoke());
		}

		public IDisposable Registration { get; }

		private void OnRaised()
		{
		}
	}

	private partial class MyCollectibleObject : Grid
	{
		private Action _action;

		public MyCollectibleObject(Action action)
		{
			_action = action;
		}

		public void MyAction() => _action.Invoke();
	}

	private class TestPlatformProvider : WeakEventHelper.ITrimProvider
	{
		private object _target;
		private Func<object, bool> _callback;

		public void RegisterTrimCallback(Func<object, bool> callback, object target)
		{
			_target = target;
			_callback = callback;
		}

		public bool? Invoke() => _callback?.Invoke(_target);
	}

	/// <summary>
	/// Like Gen2GcCallback, keeps each callback strongly referenced and runs it on every collection
	/// until it returns false or its target is collected.
	/// </summary>
	private class SimulatedGen2GcTrimProvider : WeakEventHelper.ITrimProvider
	{
		private readonly List<(Func<object, bool> Callback, WeakReference Target)> _callbacks = [];

		public int ActiveCallbacks => _callbacks.Count;

		public void RegisterTrimCallback(Func<object, bool> callback, object target)
			=> _callbacks.Add((callback, new WeakReference(target)));

		public void Collect()
			=> _callbacks.RemoveAll(c => c.Target.Target is not { } target || !c.Callback(target));
	}
}
#endif
