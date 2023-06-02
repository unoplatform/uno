#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public partial interface ICorePointerInputSource 
	{
		#if false || false || false || false || false || false || false
		bool HasCapture
		{
			get;
		}
		#endif
		#if false || false || false || false || false || false || false
		global::Windows.UI.Core.CoreCursor PointerCursor
		{
			get;
			set;
		}
		#endif
		#if false || false || false || false || false || false || false
		global::Windows.Foundation.Point PointerPosition
		{
			get;
		}
		#endif
		#if false || false || false || false || false || false || false
		void ReleasePointerCapture();
		#endif
		#if false || false || false || false || false || false || false
		void SetPointerCapture();
		#endif
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.HasCapture.get
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerPosition.get
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerCursor.get
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerCursor.set
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerCaptureLost.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerCaptureLost.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerEntered.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerEntered.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerExited.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerExited.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerMoved.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerMoved.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerPressed.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerPressed.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerReleased.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerReleased.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerWheelChanged.add
		// Forced skipping of method Windows.UI.Core.ICorePointerInputSource.PointerWheelChanged.remove
		#if false || false || false || false || false || false || false
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerCaptureLost;
		#endif
		#if false || false || false || false || false || false || false
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerEntered;
		#endif
		#if false || false || false || false || false || false || false
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerExited;
		#endif
		#if false || false || false || false || false || false || false
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerMoved;
		#endif
		#if false || false || false || false || false || false || false
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerPressed;
		#endif
		#if false || false || false || false || false || false || false
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerReleased;
		#endif
		#if false || false || false || false || false || false || false
		 event global::Windows.Foundation.TypedEventHandler<object, global::Windows.UI.Core.PointerEventArgs> PointerWheelChanged;
		#endif
	}
}
