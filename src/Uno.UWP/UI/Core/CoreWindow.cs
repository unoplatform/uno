#nullable enable

using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Input;

namespace Windows.UI.Core
{
	/// <summary>
	/// Represents the UWP app with input events and basic user interface behaviors.
	/// </summary>
	public partial class CoreWindow
	{
		[ThreadStatic]
		private static CoreWindow? _current;
		private static Action? _invalidateRender;

		private Point? _pointerPosition;
		private CoreWindowActivationState _lastActivationState;

		internal CoreWindow()
		{
			_current = this;
			Main ??= this;

			InitializePartial();
		}

		/// <summary>
		/// Occurs when the window size is changed.
		/// </summary>
		public event TypedEventHandler<CoreWindow, WindowSizeChangedEventArgs>? SizeChanged;

		/// <summary>
		/// Is fired when the window completes activation or deactivation.
		/// </summary>
		public event TypedEventHandler<CoreWindow, WindowActivatedEventArgs>? Activated;

		/// <summary>
		/// Is fired when the window visibility is changed.
		/// </summary>
		public event TypedEventHandler<CoreWindow, VisibilityChangedEventArgs>? VisibilityChanged;

		internal static CoreWindow? Main { get; private set; }

		/// <summary>
		/// Gets the event dispatcher for the window.
		/// </summary>
		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		/// <summary>
		/// Gets the client coordinates of the pointer.
		/// </summary>
		public Point PointerPosition
		{
			get => _pointerPosition ?? LastPointerEvent?.GetLocation(null).Position ?? new Point();
			set => _pointerPosition = value;
		}

#if !__WASM__ && !__MACOS__ && !__SKIA__
		/// <summary>
		/// Gets or sets the cursor used by the app.
		/// </summary>
		[Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__NETSTD_REFERENCE__")]
		public CoreCursor PointerCursor { get; set; } = new CoreCursor(CoreCursorType.Arrow, 0);
#endif

		/// <summary>
		/// Gets a value that indicates whether the window is visible.
		/// </summary>
		public bool Visible { get; internal set; }

		/// <summary>
		/// Gets a value that indicates the activation state of the window.
		/// </summary>
		public CoreWindowActivationMode ActivationMode { get; private set; } = CoreWindowActivationMode.None;

		internal IPointerEventArgs? LastPointerEvent { get; set; }

		/// <summary>
		/// Gets the CoreWindow instance for the currently active thread.
		/// </summary>
		/// <returns>The CoreWindow for the currently active thread.</returns>
		public static CoreWindow? GetForCurrentThread()
			=> _current; // UWP returns 'null' if on a BG thread

		[Uno.NotImplemented]
		public CoreVirtualKeyStates GetAsyncKeyState(System.VirtualKey virtualKey)
			=> CoreVirtualKeyStates.None;

		[Uno.NotImplemented]
		public CoreVirtualKeyStates GetKeyState(System.VirtualKey virtualKey)
			=> CoreVirtualKeyStates.None;

		internal static void SetInvalidateRender(Action invalidateRender)
			=> _invalidateRender = invalidateRender;

		internal static void QueueInvalidateRender()
			=> _invalidateRender?.Invoke();

		partial void InitializePartial();

		internal void OnActivated(WindowActivatedEventArgs args)
		{
			_lastActivationState = args.WindowActivationState;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"CoreWindow activating with {_lastActivationState} state.");
			}

			UpdateActivationMode();

			Activated?.Invoke(this, args);
		}

		internal void OnSizeChanged(WindowSizeChangedEventArgs windowSizeChangedEventArgs)
			=> SizeChanged?.Invoke(this, windowSizeChangedEventArgs);

		internal interface IPointerEventArgs
		{
			PointerPoint GetLocation(object? relativeTo);
		}

		internal void OnVisibilityChanged(VisibilityChangedEventArgs visibilityChangedEventArgs)
		{
			UpdateActivationMode();

			VisibilityChanged?.Invoke(this, visibilityChangedEventArgs);
		}

		private void UpdateActivationMode()
		{
			if (_lastActivationState == CoreWindowActivationState.Deactivated)
			{
				ActivationMode = CoreWindowActivationMode.Deactivated;
			}
			else
			{
				ActivationMode = Visible ?
					CoreWindowActivationMode.ActivatedInForeground :
					CoreWindowActivationMode.ActivatedNotForeground;
			}
		}
	}
}
