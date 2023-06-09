#nullable enable

using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia
{
	internal partial class TizenCoreWindowExtension : ICoreWindowExtension
	{
		private readonly CoreWindow _owner;

		public TizenCoreWindowExtension(object owner, UnoCanvas canvas)
		{
			_owner = (CoreWindow)owner;

			canvas.KeyUp += OnKeyUp;
			canvas.KeyDown += OnKeyDown;
		}

		public bool IsNativeElement(object content)
			=> false;
		public void AttachNativeElement(object owner, object content) { }
		public void DetachNativeElement(object owner, object content) { }
		public void ArrangeNativeElement(object owner, object content, Windows.Foundation.Rect arrangeRect) { }
		public Windows.Foundation.Size MeasureNativeElement(object owner, object content, Windows.Foundation.Size size)
			=> size;
	}
}
