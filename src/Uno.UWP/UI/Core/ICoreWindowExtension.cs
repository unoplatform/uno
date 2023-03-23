using System;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;

namespace Windows.UI.Core
{
	internal interface ICoreWindowExtension
	{
		public CoreCursor PointerCursor { get; set; }

		void ReleasePointerCapture(PointerIdentifier pointer);

		void SetPointerCapture(PointerIdentifier pointer);

		bool IsNativeElement(object content);

		void AttachNativeElement(object owner, object content);

		void DetachNativeElement(object owner, object content);

		void ArrangeNativeElement(object owner, object content, Rect arrangeRect);

		Size MeasureNativeElement(object owner, object content, Size size);
	}
}
