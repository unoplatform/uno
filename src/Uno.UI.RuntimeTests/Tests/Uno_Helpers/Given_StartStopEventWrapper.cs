#if HAS_UNO
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Helpers;
using Windows.Devices.Sensors;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Uno_Helpers
{
	[TestClass]
	public class Given_StartStopEventWrapper
	{
		[TestMethod]
		public void When_Default_Event_Null()
		{
			var wrapper = new StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>>(() => { }, () => { });
			Assert.IsNull(wrapper.Event);
		}

		[TestMethod]
		public void When_Add_Handler_Event_Not_Null()
		{
			var wrapper = new StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>>(() => { }, () => { });
			wrapper.AddHandler((s, e) => { });
			Assert.IsNotNull(wrapper.Event);
		}

		[TestMethod]
		public void When_Add_Remove_Event_Null()
		{
			void Handler(Accelerometer a, AccelerometerReadingChangedEventArgs e)
			{
			}
			var wrapper = new StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>>(() => { }, () => { });
			wrapper.AddHandler(Handler);
			wrapper.RemoveHandler(Handler);
			Assert.IsNull(wrapper.Event);
		}

		[TestMethod]
		public void When_Remove_On_Empty()
		{
			var wrapper = new StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>>(() => { }, () => { });
			wrapper.RemoveHandler((s, e) => { });
			Assert.IsNull(wrapper.Event);
		}


		[TestMethod]
		public void When_Remove_Nonexistent()
		{
			var wrapper = new StartStopEventWrapper<TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>>(() => { }, () => { });
			wrapper.AddHandler((s, e) => { });
			wrapper.RemoveHandler((s, e) => { });
			Assert.IsNotNull(wrapper.Event);
		}

		[TestMethod]
		public void When_Single_Invoked()
		{
			var invoked = false;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => { }, () => { });
			wrapper.AddHandler((s, e) => invoked = true);
			Assert.IsFalse(invoked);
			wrapper.Event?.Invoke(null, EventArgs.Empty);
			Assert.IsTrue(invoked);
		}

		[TestMethod]
		public void When_Multiple_Invoked()
		{
			var firstInvoked = false;
			var secondInvoked = false;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => { }, () => { });
			wrapper.AddHandler((s, e) => firstInvoked = true);
			wrapper.AddHandler((s, e) => secondInvoked = true);
			Assert.IsFalse(firstInvoked);
			Assert.IsFalse(secondInvoked);
			wrapper.Event?.Invoke(null, EventArgs.Empty);
			Assert.IsTrue(firstInvoked);
			Assert.IsTrue(secondInvoked);
		}

		[TestMethod]
		public void When_Multiple_First_Removed_Invoked()
		{
			var firstInvoked = false;
			void FirstHandler(object sender, EventArgs args)
			{
				firstInvoked = true;
			}
			var secondInvoked = false;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => { }, () => { });
			wrapper.AddHandler(FirstHandler);
			wrapper.AddHandler((s, e) => secondInvoked = true);
			Assert.IsFalse(firstInvoked);
			Assert.IsFalse(secondInvoked);
			wrapper.RemoveHandler(FirstHandler);
			wrapper.Event?.Invoke(null, EventArgs.Empty);
			Assert.IsFalse(firstInvoked);
			Assert.IsTrue(secondInvoked);
		}

		[TestMethod]
		public void When_First_Subscriber_OnFirst_Invoked()
		{
			var onFirst = false;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => onFirst = true, () => { });
			Assert.IsFalse(onFirst);
			wrapper.AddHandler((s, e) => { });
			Assert.IsTrue(onFirst);
		}

		[TestMethod]
		public void When_Multiple_Subscribers_OnFirst_Once()
		{
			var onFirstCounter = 0;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => onFirstCounter++, () => { });
			Assert.AreEqual(0, onFirstCounter);
			wrapper.AddHandler((s, e) => { });
			wrapper.AddHandler((s, e) => { });
			Assert.AreEqual(1, onFirstCounter);
		}

		[TestMethod]
		public void When_Multiple_Subscribers_Sequence_OnFirst()
		{
			void FirstSubscriber(object sender, EventArgs e)
			{
			}

			void SecondSubscriber(object sender, EventArgs e)
			{
			}

			var onFirstCounter = 0;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => onFirstCounter++, () => { });
			Assert.AreEqual(0, onFirstCounter);
			wrapper.AddHandler(FirstSubscriber);
			wrapper.AddHandler(SecondSubscriber);
			Assert.AreEqual(1, onFirstCounter);
			wrapper.RemoveHandler(FirstSubscriber);
			wrapper.RemoveHandler(SecondSubscriber);
			Assert.AreEqual(1, onFirstCounter);
			wrapper.AddHandler(FirstSubscriber);
			Assert.AreEqual(2, onFirstCounter);
		}

		[TestMethod]
		public void When_First_Subscriber_OnLast_Invoked()
		{
			void FirstSubscriber(object sender, EventArgs e)
			{
			}
			var onLast = false;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => { }, () => onLast = true);
			wrapper.AddHandler(FirstSubscriber);
			Assert.IsFalse(onLast);
			wrapper.RemoveHandler(FirstSubscriber);
			Assert.IsTrue(onLast);
		}

		[TestMethod]
		public void When_Multiple_Subscribers_OnLast_Once()
		{
			void FirstSubscriber(object sender, EventArgs e)
			{
			}

			void SecondSubscriber(object sender, EventArgs e)
			{
			}
			var onLastCounter = 0;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => { }, () => onLastCounter++);
			wrapper.AddHandler(FirstSubscriber);
			wrapper.AddHandler(SecondSubscriber);
			Assert.AreEqual(0, onLastCounter);
			wrapper.RemoveHandler(FirstSubscriber);
			wrapper.RemoveHandler(SecondSubscriber);
			Assert.AreEqual(1, onLastCounter);
		}

		[TestMethod]
		public void When_Fake_Unsubscribe_OnLast_Once()
		{
			void FirstSubscriber(object sender, EventArgs e)
			{
			}

			var onLastCounter = 0;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => { }, () => onLastCounter++);
			wrapper.AddHandler(FirstSubscriber);
			Assert.AreEqual(0, onLastCounter);
			wrapper.RemoveHandler(FirstSubscriber);
			wrapper.RemoveHandler(FirstSubscriber);
			Assert.AreEqual(1, onLastCounter);
		}

		[TestMethod]
		public void When_Multiple_Subscribers_Sequence_OnLast()
		{
			void FirstSubscriber(object sender, EventArgs e)
			{
			}

			void SecondSubscriber(object sender, EventArgs e)
			{
			}

			var onLastCounter = 0;
			var wrapper = new StartStopEventWrapper<EventHandler<EventArgs>>(() => { }, () => onLastCounter++);
			wrapper.AddHandler(FirstSubscriber);
			wrapper.AddHandler(SecondSubscriber);
			Assert.AreEqual(0, onLastCounter);
			wrapper.RemoveHandler(FirstSubscriber);
			wrapper.RemoveHandler(SecondSubscriber);
			Assert.AreEqual(1, onLastCounter);
			wrapper.AddHandler(FirstSubscriber);
			wrapper.RemoveHandler(FirstSubscriber);
			Assert.AreEqual(2, onLastCounter);
		}
	}
}
#endif
