#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.Foundation;

namespace Windows.UI.Core
{
	/// <summary>
	/// Specifies an interface for a window object and its input events as well as basic user interface behaviors.
	/// </summary>
	public partial interface ICoreWindow
	{
		/// <summary>
		/// Specifies a property that gets the event dispatcher for the window.
		/// </summary>
		CoreDispatcher Dispatcher { get; }

		/// <summary>
		/// Specifies the property that gets whether the window is visible or not.
		/// </summary>
		bool Visible { get; }

#if __WASM__ || __MACOS__ || __SKIA__
		global::Windows.UI.Core.CoreCursor PointerCursor { get; set; }
#endif

		global::Windows.Foundation.Point PointerPosition { get; set; }

		/// <summary>
		/// Specifies the event that is fired when the window completes activation or deactivation.
		/// </summary>
		event TypedEventHandler<CoreWindow, WindowActivatedEventArgs> Activated;

		/// <summary>
		/// Specifies the event that raises when the window size is changed.
		/// </summary>
		event TypedEventHandler<CoreWindow, WindowSizeChangedEventArgs> SizeChanged;

		/// <summary>
		/// Specifies the event that occurs when the window visibility is changed.
		/// </summary>
		event TypedEventHandler<CoreWindow, VisibilityChangedEventArgs> VisibilityChanged;

#if __IOS__
		/// <summary>
		/// Specifies the event that occurs when the window Key is Down.
		/// </summary>
		public event TypedEventHandler<CoreWindow, KeyEventArgs> KeyDown;

		/// <summary>
		/// Specifies the event that occurs when the window Key is Up.
		/// </summary>
		public event TypedEventHandler<CoreWindow, KeyEventArgs> KeyUp;
#endif
	}
}
