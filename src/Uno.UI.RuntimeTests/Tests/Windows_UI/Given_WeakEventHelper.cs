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

		Assert.IsGreaterThanOrEqual(5150, invoked);

		disposable.Clear();

		GC.Collect(2);
		GC.WaitForPendingFinalizers();

		// Ensure that everything has been collected.
		SUT.Invoke(this, null);

		Assert.IsGreaterThanOrEqual(5150, invoked);
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
}
#endif
