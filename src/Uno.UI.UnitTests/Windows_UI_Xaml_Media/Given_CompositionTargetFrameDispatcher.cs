#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_CompositionTargetFrameDispatcher
{
	[TestMethod]
	public void When_Dispatch_Then_All_Handlers_Invoked()
	{
		var dispatcher = new CompositionTargetFrameDispatcher();
		var invoked = new List<int>();
		var handlers = new List<EventHandler<object>>
		{
			(_, _) => invoked.Add(0),
			(_, _) => invoked.Add(1),
			(_, _) => invoked.Add(2),
		};

		dispatcher.Dispatch(handlers);

		CollectionAssert.AreEqual(new[] { 0, 1, 2 }, invoked);
	}

	[TestMethod]
	public void When_Dispatch_Completes_Then_Buffer_Is_Cleared()
	{
		var dispatcher = new CompositionTargetFrameDispatcher();
		var handlers = new List<EventHandler<object>> { (_, _) => { }, (_, _) => { } };

		dispatcher.Dispatch(handlers);

		Assert.IsTrue(
			dispatcher.Snapshot.All(h => h is null),
			"The reused snapshot buffer must not root any handler past its dispatch.");
	}

	[TestMethod]
	public void When_Handler_Throws_Then_Buffer_Is_Still_Cleared()
	{
		var dispatcher = new CompositionTargetFrameDispatcher();
		var laterInvoked = false;
		var handlers = new List<EventHandler<object>>
		{
			(_, _) => throw new InvalidOperationException("boom"),
			(_, _) => laterInvoked = true,
		};

		Assert.ThrowsExactly<InvalidOperationException>(() => dispatcher.Dispatch(handlers));

		Assert.IsFalse(laterInvoked, "Dispatch stops at the throwing handler.");
		Assert.IsTrue(
			dispatcher.Snapshot.All(h => h is null),
			"A throwing handler must not leave the remaining (not-yet-dispatched) handlers rooted in the static buffer.");
	}

	[TestMethod]
	public void When_Handler_List_Shrinks_Then_No_Stale_References_Beyond_Count()
	{
		var dispatcher = new CompositionTargetFrameDispatcher();

		// A large frame grows the buffer.
		var large = new List<EventHandler<object>>
		{
			(_, _) => { },
			(_, _) => { },
			(_, _) => { },
			(_, _) => { },
		};
		dispatcher.Dispatch(large);

		var bufferLength = dispatcher.Snapshot.Count;
		Assert.IsTrue(bufferLength >= large.Count, "Pre-condition: buffer grew to hold the large frame.");

		// A subsequent smaller frame must not leave the large frame's delegates rooted in the
		// tail slots [count, length) of the reused buffer.
		var small = new List<EventHandler<object>> { (_, _) => { } };
		dispatcher.Dispatch(small);

		Assert.AreEqual(bufferLength, dispatcher.Snapshot.Count, "Buffer is reused, not reallocated, for a smaller frame.");
		Assert.IsTrue(
			dispatcher.Snapshot.All(h => h is null),
			"Entries beyond the current handler count must not keep stale delegate references from a previous, larger frame.");
	}
}
