using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.RoutedEventTests
{
	[TestClass]
	public class Given_TappedRoutedEvent
	{
		[TestMethod]
		public void When_SubscribingOnControlDirectly()
		{
			var events = new List<(object sender, TappedRoutedEventArgs args)>();

			var root = new Border();

			void OnTapped(object snd, TappedRoutedEventArgs evt) => events.Add((snd, evt));

			root.Tapped += OnTapped;

			var evt1 = new TappedRoutedEventArgs();
			root.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeTrue();

			events.Should().HaveCount(1)
				.And.ContainSingle(x => x.sender == root && x.args == evt1);
		}

		[TestMethod]
		public void When_UsingManaged_WithNoHandlers()
		{
			var root = new Border();

			var evt1 = new TappedRoutedEventArgs();
			root.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeTrue();
		}

		[TestMethod]
		public void When_SubscribingUsingAddHandler()
		{
			var events = new List<(object sender, TappedRoutedEventArgs args)>();

			var root = new Border();

			void OnTapped(object snd, TappedRoutedEventArgs evt) => events.Add((snd, evt));

			root.AddHandler(UIElement.TappedEvent, (TappedEventHandler) OnTapped, false);

			var evt1 = new TappedRoutedEventArgs();
			root.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeTrue();

			events.Should().HaveCount(1)
				.And.ContainSingle(x => x.sender == root && x.args == evt1);
		}

		[TestMethod]
		public void When_SubscribingUsingAddHandler_And_BubblingInManagedCode()
		{
			var events = new List<(object sender, TappedRoutedEventArgs args)>();

			var child2 = new Border
			{
				Name = "child2"
			};
			var child1 = new Border
			{
				Child = child2,
				Name = "child1"
			};
			var root = new Border
			{
				Child = child1,
				Name = "root"
			};

			void OnTapped(object snd, TappedRoutedEventArgs evt)
			{
				events.Add((snd, evt));
				evt.Handled = true;
			}

			root.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, false);
			child1.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, false);

			root.Measure(new Size(1, 1));

			var evt1 = new TappedRoutedEventArgs();
			child2.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeTrue();

			events.Should().HaveCount(1)
				.And.ContainSingle(x => x.sender == child1 && x.args == evt1);
		}

		[TestMethod]
		public void When_SubscribingUsingAddHandler_WithHandlesToo_And_BubblingInManagedCode()
		{
			var events = new List<(object sender, TappedRoutedEventArgs args)>();

			var child2 = new Border
			{
				Name = "child2"
			};
			var child1 = new Border
			{
				Child = child2,
				Name = "child1"
			};
			var root = new Border
			{
				Child = child1,
				Name = "root"
			};

			void OnTapped(object snd, TappedRoutedEventArgs evt)
			{
				events.Add((snd, evt));
				evt.Handled = true;
			}

			root.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, true);
			child1.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, false);

			root.Measure(new Size(1, 1));

			var evt1 = new TappedRoutedEventArgs();
			child2.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeTrue();

			events.Should().HaveCount(2);
		}

		[TestMethod]
		public void When_SubscribingUsingAddHandler_And_BubblingInNativeCode()
		{
			var events = new List<(object sender, TappedRoutedEventArgs args)>();

			var child2 = new Border
			{
				Name = "child2"
			};
			var child1 = new Border
			{
				Child = child2,
				Name = "child1"
			};
			var root = new Border
			{
				Child = child1,
				Name = "root"
			};

			void OnTapped(object snd, TappedRoutedEventArgs evt)
			{
				events.Add((snd, evt));
				evt.Handled = true;
			}

			root.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, false);
			child1.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, false);

			root.Measure(new Size(1, 1));

			var evt1 = new TappedRoutedEventArgs()
			{
				CanBubbleNatively = true
			};
			child2.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeFalse();

			events.Should().HaveCount(0);
		}

		[TestMethod]
		public void When_SubscribingUsingAddHandler_WithHandlesToo_And_BubblingInNativeCode()
		{
			var events = new List<(object sender, TappedRoutedEventArgs args)>();

			var child2 = new Border
			{
				Name = "child2"
			};
			var child1 = new Border
			{
				Child = child2,
				Name = "child1"
			};
			var root = new Border
			{
				Child = child1,
				Name = "root"
			};

			void OnTapped(object snd, TappedRoutedEventArgs evt)
			{
				events.Add((snd, evt));
				evt.Handled = true;
			}

			root.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, true);
			child2.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, false);

			root.Measure(new Size(1, 1));

			var evt1 = new TappedRoutedEventArgs()
			{
				CanBubbleNatively = true
			};
			child2.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeTrue();

			events.Should().HaveCount(2);
		}

		[TestMethod]
		public void When_SubscribingUsingAddHandler_WithHandlesToo_And_BubblingInNativeCode_EatenByPlatform()
		{
			var events = new List<(object sender, TappedRoutedEventArgs args)>();

			var child2 = new Border
			{
				Name = "child2"
			};
			var child1 = new Border
			{
				Child = child2,
				Name = "child1"
			};
			var root = new Border
			{
				Child = child1,
				Name = "root"
			};

			void OnTapped(object snd, TappedRoutedEventArgs evt)
			{
				events.Add((snd, evt));
				evt.Handled = true;
			}

			root.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, true);
			child2.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, true);

			root.Measure(new Size(1, 1));

			var evt1 = new TappedRoutedEventArgs()
			{
				CanBubbleNatively = true
			};
			child1.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeFalse();
			// Here the "platform" is eating the native bubbling event, so no handlers should receive it.

			events.Should().HaveCount(0);
		}

		[TestMethod]
		public void When_SubscribingUsingAddHandler_WithHandlesToo_And_BubblingInNativeCode_SetToBubbleInManaged_EatenByPlatform()
		{
			var events = new List<(object sender, TappedRoutedEventArgs args)>();

			var child2 = new Border
			{
				Name = "child2"
			};
			var child1 = new Border
			{
				Child = child2,
				Name = "child1"
			};
			var root = new Border
			{
				Child = child1,
				Name = "root",
				EventsBubblingInManagedCode = RoutedEventFlag.Tapped
			};

			void OnTapped(object snd, TappedRoutedEventArgs evt)
			{
				events.Add((snd, evt));
				evt.Handled = true;
			}

			root.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, true);
			child1.AddHandler(UIElement.TappedEvent, (TappedEventHandler)OnTapped, true);

			root.Measure(new Size(1, 1));

			var evt1 = new TappedRoutedEventArgs()
			{
				CanBubbleNatively = true
			};
			child2.RaiseEvent(UIElement.TappedEvent, evt1).Should().BeTrue();

			events.Should().HaveCount(2); // bubbling in managed code
		}
	}
}
