#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Helpers;

[TestClass]
public class Given_WeakEventManager
{
	private sealed class EventPublisher
	{
		private readonly WeakEventManager _manager = new();

		internal event Action Event
		{
			add => _manager.AddEventHandler(value);
			remove => _manager.RemoveEventHandler(value);
		}

		public void RaiseEvent() => _manager.HandleEvent("Event");
	}

	private sealed class EventSubscriber
	{
		public void M() { }
	}

	[TestMethod]
	public void When_ManySubscriptions_Then_DoesNotLeak()
	{
		var publisher = new EventPublisher();
		var weakRefs = new List<WeakReference<EventSubscriber>>();
		Subscribe(publisher, weakRefs);

		for (var i = 0; i < 10; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		foreach (var x in weakRefs)
		{
			Assert.IsFalse(x.TryGetTarget(out _));
		}
	}

	[TestMethod]
	public void When_ReEnter_Then_AllHandlersInvokedProperly()
	{
		var sut = new WeakEventManager();
		int handler1Count = 0, handler2Count = 0;

		sut.AddEventHandler(Handler1, "Event1");
		sut.AddEventHandler(Handler2, "Event1");
		sut.AddEventHandler(Handler1_2, "Event2");
		sut.AddEventHandler(Handler2_2, "Event2");

		sut.HandleEvent("Event1");

		Assert.AreEqual(3, handler1Count);
		Assert.AreEqual(3, handler2Count);

		sut.HandleEvent("Event2");

		Assert.AreEqual(4, handler1Count);
		Assert.AreEqual(4, handler2Count);

		void Handler1()
		{
			handler1Count++;
			sut.HandleEvent("Event2");
		}

		void Handler1_2()
		{
			handler1Count++;
		}

		void Handler2()
		{
			handler2Count++;
			sut.HandleEvent("Event2");
		}

		void Handler2_2()
		{
			handler2Count++;
		}
	}

	[TestMethod]
	public void When_UnSubscribeInHandler()
	{
		var sut = new WeakEventManager();
		int handler1Count = 0, handler2Count = 0;

		sut.AddEventHandler(Handler1, "Event1");
		sut.AddEventHandler(Handler2, "Event1");

		sut.HandleEvent("Event1");
		sut.HandleEvent("Event1");

		Assert.AreEqual(1, handler1Count);
		Assert.AreEqual(2, handler2Count);

		void Handler1()
		{
			handler1Count++;
			sut.RemoveEventHandler(Handler1, "Event1");
		}

		void Handler2()
		{
			handler2Count++;
		}
	}

	[TestMethod]
	public void When_UnSubscribeInHandler2()
	{
		var pub = new EventPublisher();
		int handler1Count = 0, handler2Count = 0;

		pub.Event += Handler1;
		pub.Event += Handler2;

		pub.RaiseEvent();
		pub.RaiseEvent();

		Assert.AreEqual(1, handler1Count);
		Assert.AreEqual(2, handler2Count);

		void Handler1()
		{
			handler1Count++;
			pub.Event -= Handler1;
		}

		void Handler2()
		{
			handler2Count++;
		}
	}

	private void Subscribe(EventPublisher publisher, List<WeakReference<EventSubscriber>> weakRefs)
	{
		for (var i = 0; i < 1000; i++)
		{
			var subscriber = new EventSubscriber();
			publisher.Event += subscriber.M;
			weakRefs.Add(new WeakReference<EventSubscriber>(subscriber));
		}
	}
}
