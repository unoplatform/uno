#nullable enable

using System;
using System.Linq;
using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal interface IDragEventSource
	{
		long Id { get; }

		uint FrameId { get; }

		(Point location, DragDropModifiers modifier) GetState();

		Point GetPosition(object? relativeTo);
	}
}
