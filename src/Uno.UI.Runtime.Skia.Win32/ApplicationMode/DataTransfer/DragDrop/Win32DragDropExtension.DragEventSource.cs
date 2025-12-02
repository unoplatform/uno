using System;
using System.Threading;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Win32.System.SystemServices;
using Microsoft.UI.Xaml;
using Uno.Extensions;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32DragDropExtension
{
	private readonly struct DragEventSource(Point point, MODIFIERKEYS_FLAGS modifierFlags) : IDragEventSource
	{
		private static long _nextFrameId;
		private readonly Point _location = point;

		public long Id => _fakePointerId;

		public uint FrameId { get; } = (uint)Interlocked.Increment(ref _nextFrameId);

		/// <inheritdoc />
		public (Point location, DragDropModifiers modifier) GetState()
		{
			var flags = DragDropModifiers.None;
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_SHIFT) != 0)
			{
				flags |= DragDropModifiers.Shift;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_CONTROL) != 0)
			{
				flags |= DragDropModifiers.Control;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_LBUTTON) != 0)
			{
				flags |= DragDropModifiers.LeftButton;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_RBUTTON) != 0)
			{
				flags |= DragDropModifiers.RightButton;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_MBUTTON) != 0)
			{
				flags |= DragDropModifiers.MiddleButton;
			}
			return (_location, flags);
		}

		/// <inheritdoc />
		public Point GetPosition(object? relativeTo)
		{
			if (relativeTo is null)
			{
				return _location;
			}

			if (relativeTo is UIElement elt)
			{
				var eltToRoot = UIElement.GetTransform(elt, null);
				var rootToElt = eltToRoot.Inverse();

				return rootToElt.Transform(_location);
			}

			throw new InvalidOperationException("The relative to must be a UIElement.");
		}
	}
}
