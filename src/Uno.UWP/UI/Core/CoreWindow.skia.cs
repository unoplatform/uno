#nullable enable

using System;
using Uno.Foundation;
using Windows.Foundation;

namespace Windows.UI.Core
{
	public partial class CoreWindow
	{
		internal event TypedEventHandler<CoreWindow, KeyEventArgs>? NativeKeyDownReceived;
		internal event TypedEventHandler<CoreWindow, KeyEventArgs>? NativeKeyUpReceived;

		internal bool IsNativeElement(object content)
			=> _coreWindowExtension!.IsNativeElement(content);

		internal void AttachNativeElement(object owner, object content)
			=> _coreWindowExtension!.AttachNativeElement(owner, content);

		internal void DetachNativeElement(object owner, object content)
			=> _coreWindowExtension!.DetachNativeElement(owner, content);

		internal void ArrangeNativeElement(object owner, object content, Rect arrangeRect)
			=> _coreWindowExtension!.ArrangeNativeElement(owner, content, arrangeRect);

		internal Size MeasureNativeElement(object owner, object content, Size size)
			=> _coreWindowExtension!.MeasureNativeElement(owner, content, size);

		void ICoreWindowEvents.RaiseNativeKeyDownReceived(KeyEventArgs args)
		{
			NativeKeyDownReceived?.Invoke(this, args);
			if (!args.Handled)
			{
				((ICoreWindowEvents)this).RaiseKeyDown(args);
			}
		}

		void ICoreWindowEvents.RaiseNativeKeyUpReceived(KeyEventArgs args)
		{
			NativeKeyUpReceived?.Invoke(this, args);
			if (!args.Handled)
			{
				((ICoreWindowEvents)this).RaiseKeyUp(args);
			}
		}
	}
}
