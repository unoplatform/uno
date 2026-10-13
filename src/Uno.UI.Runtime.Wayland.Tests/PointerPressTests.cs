#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Input;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class PointerPressTests
{
	[TestMethod]
	public void QueuedPressesKeepTheirOwnSerials()
	{
		var queue = new Queue<Action>();
		var press = new WaylandPointerPress(queue.Enqueue);
		var serials = new List<uint?>();
		press.Queue(42, 0x110);
		queue.Enqueue(() => serials.Add(press.Take(0x110)));
		press.Queue(43, 0x110);
		queue.Enqueue(() => serials.Add(press.Take(0x110)));
		while (queue.TryDequeue(out var action))
		{
			action();
		}
		CollectionAssert.AreEqual(new uint?[] { 42, 43 }, serials);
		Assert.IsNull(press.Take(0x110));
	}

	[TestMethod]
	public void PressOfAnotherButtonHasNoSerial()
	{
		var queue = new Queue<Action>();
		var press = new WaylandPointerPress(queue.Enqueue);
		press.Queue(42, 0x110);
		queue.Dequeue()();
		Assert.IsNull(press.Take(0x111));
	}
}
