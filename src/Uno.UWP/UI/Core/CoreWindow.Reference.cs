#nullable enable

using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Uno.Foundation;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Windows.UI.Input;

namespace Windows.UI.Core;

public partial class CoreWindow
{
	internal void SetPointerInputSource(IUnoCorePointerInputSource source)
		=> throw new NotSupportedException();

	internal void SetKeyboardInputSource(IUnoKeyboardInputSource source)
		=> throw new NotSupportedException();

	internal bool IsNativeElement(object content)
		=> throw new NotSupportedException();

	internal void AttachNativeElement(object owner, object content)
		=> throw new NotSupportedException();

	internal void DetachNativeElement(object owner, object content)
		=> throw new NotSupportedException();

	internal void ArrangeNativeElement(object owner, object content, Rect arrangeRect)
		=> throw new NotSupportedException();

	internal Size MeasureNativeElement(object owner, object content, Size size)
		=> throw new NotSupportedException();

	internal bool IsNativeElementAttached(object owner, object nativeElement)
		=> throw new NotSupportedException();

	internal IUnoCorePointerInputSource? PointersSource
		=> throw new NotSupportedException();

	internal void RaiseNativeKeyDownReceived(KeyEventArgs args)
		=> throw new NotSupportedException();

	internal void RaiseNativeKeyUpReceived(KeyEventArgs args)
		=> throw new NotSupportedException();
}
