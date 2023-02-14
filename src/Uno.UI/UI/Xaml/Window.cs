using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno.Disposables;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Represents an application window.
	/// </summary>
	public sealed partial class Window
	{
		private static Window _current;

		private UIElement _content;
		private RootVisual _rootVisual;

		private CoreWindowActivationState? _lastActivationState;
		private Brush _background;

		private List<WeakEventHelper.GenericEventHandler> _sizeChangedHandlers = new List<WeakEventHelper.GenericEventHandler>();
		private List<WeakEventHelper.GenericEventHandler> _backgroundChangedHandlers;

#if HAS_UNO_WINUI
		public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; } = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
#endif

#if HAS_UNO_WINUI
		public Window() : this(false)
		{
		}
#endif

		internal Window(bool internalUse)
		{
			if (!internalUse)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"Creating a secondary Window instance is currently not supported in Uno Platform targets. " +
						"Use the Window.Current property instead (you can use #if HAS_UNO to differentiate " +
						"between Uno Platform targets and Windows App SDK).");
				}
			}

			InitPlatform();

			InitializeCommon();
		}

		partial void InitPlatform();

#pragma warning disable 67
		/// <summary>
		/// Occurs when the window has successfully been activated.
		/// </summary>
		public event WindowActivatedEventHandler Activated;

		/// <summary>
		/// Occurs when the window has closed.
		/// </summary>
		public event WindowClosedEventHandler Closed;

		/// <summary>
		/// Occurs when the app window has first rendered or has changed its rendering size.
		/// </summary>
		public event WindowSizeChangedEventHandler SizeChanged;

		/// <summary>
		/// Occurs when the value of the Visible property changes.
		/// </summary>
		public event WindowVisibilityChangedEventHandler VisibilityChanged;

		private void InitializeCommon()
		{
			InitDragAndDrop();
			if (Application.Current != null)
			{
				Application.Current.RaiseWindowCreated(this);
			}
			else
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn("Unable to raise WindowCreatedEvent, there is no active Application");
				}
			}

			Background = SolidColorBrushHelper.White;
		}

		public UIElement Content
		{
			get => InternalGetContent();
			set
			{
				if (Content == value)
				{
					// Content already set, ignore.
					return;
				}

				var oldContent = Content;
				if (oldContent != null)
				{
					oldContent.IsWindowRoot = false;

					if (oldContent is FrameworkElement oldRoot)
					{
						oldRoot.SizeChanged -= RootSizeChanged;
					}
				}

				if (value != null)
				{
					value.IsWindowRoot = true;
				}

				InternalSetContent(value);

				if (value is FrameworkElement newRoot)
				{
					newRoot.SizeChanged += RootSizeChanged;
				}

				oldContent?.XamlRoot?.NotifyChanged();
				if (value?.XamlRoot != oldContent?.XamlRoot)
				{
					value?.XamlRoot?.NotifyChanged();
				}
			}
		}

		/// <summary>
		/// This is the real root of the **managed** visual tree.
		/// This means its the root panel which contains the <see cref="Content"/>
		/// but also the PopupRoot, the DragRoot and all other internal UI elements.
		/// On platforms like iOS and Android, we might still have few native controls above this.
		/// </summary>
		/// <remarks>This element is flagged with IsVisualTreeRoot.</remarks>
		internal UIElement RootElement => InternalGetRootElement();

		internal PopupRoot PopupRoot => Uno.UI.Xaml.Core.CoreServices.Instance.MainPopupRoot;

		internal FullWindowMediaRoot FullWindowMediaRoot => Uno.UI.Xaml.Core.CoreServices.Instance.MainFullWindowMediaRoot;

		internal Canvas FocusVisualLayer => Uno.UI.Xaml.Core.CoreServices.Instance.MainFocusVisualRoot;

		/// <summary>
		/// Gets a Rect value containing the height and width of the application window in units of effective (view) pixels.
		/// </summary>
		public Rect Bounds { get; private set; }

		/// <summary>
		/// Gets an internal core object for the application window.
		/// </summary>
		public CoreWindow CoreWindow { get; private set; }

		/// <summary>
		/// Gets the CoreDispatcher object for the Window, which is generally the CoreDispatcher for the UI thread.
		/// </summary>
		public CoreDispatcher Dispatcher { get; private set; }

		/// <summary>
		/// Gets a value that reports whether the window is visible.
		/// </summary>
		public bool Visible
		{
			get => CoreWindow.Visible;
			set => CoreWindow.Visible = value;
		}

		/// <summary>
		/// Gets the window of the current thread.
		/// </summary>
		public static Window Current => InternalGetCurrentWindow();

		public void Activate()
		{
			// Currently Uno supports only single window,
			// for compatibility with WinUI we set the first activated
			// as Current #8341
			_current ??= this;

			InternalActivate();

			OnActivated(CoreWindowActivationState.CodeActivated);

			// Initialize visibility on first activation.
			Visible = true;
		}

		partial void InternalActivate();

		public void Close() { }

		public void SetTitleBar(UIElement value) { }

		/// <summary>
		/// Provides a memory-friendly registration to the <see cref="SizeChanged" /> event.
		/// </summary>
		/// <returns>A disposable instance that will cancel the registration.</returns>
		internal IDisposable RegisterSizeChangedEvent(Microsoft.UI.Xaml.WindowSizeChangedEventHandler handler)
		{
			return WeakEventHelper.RegisterEvent(
				_sizeChangedHandlers,
				handler,
				(h, s, e) =>
					(h as Microsoft.UI.Xaml.WindowSizeChangedEventHandler)?.Invoke(s, (WindowSizeChangedEventArgs)e)
			);
		}

		internal void OnActivated(CoreWindowActivationState state)
		{
			if (_lastActivationState != state)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Window activating with {state} state.");
				}

				_lastActivationState = state;
				var activatedEventArgs = new WindowActivatedEventArgs(state);
#if HAS_UNO_WINUI
				// There are two "versions" of WindowActivatedEventArgs in Uno currently
				// when using WinUI, we need to use "legacy" version to work with CoreWindow
				// (which will eventually be removed as a legacy API as well.
				var coreWindowActivatedEventArgs = new Windows.UI.Core.WindowActivatedEventArgs(state);
#else
				var coreWindowActivatedEventArgs = activatedEventArgs;
#endif
				CoreWindow.OnActivated(coreWindowActivatedEventArgs);
				Activated?.Invoke(this, activatedEventArgs);
			}
		}

		internal void OnVisibilityChanged(bool newVisibility)
		{
			if (Visible != newVisibility)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Window visibility changing to {newVisibility}");
				}

				Visible = newVisibility;

				var args = new VisibilityChangedEventArgs() { Visible = newVisibility };
				CoreWindow.OnVisibilityChanged(args);
				VisibilityChanged?.Invoke(this, args);
			}
		}

		private void RootSizeChanged(object sender, SizeChangedEventArgs args) => _rootVisual.XamlRoot.NotifyChanged();

		private void RaiseSizeChanged(Windows.UI.Core.WindowSizeChangedEventArgs windowSizeChangedEventArgs)
		{
			var baseSizeChanged = new WindowSizeChangedEventArgs(windowSizeChangedEventArgs.Size) { Handled = windowSizeChangedEventArgs.Handled };

			SizeChanged?.Invoke(this, baseSizeChanged);

			windowSizeChangedEventArgs.Handled = baseSizeChanged.Handled;

			CoreWindow.GetForCurrentThread()?.OnSizeChanged(windowSizeChangedEventArgs);

			baseSizeChanged.Handled = windowSizeChangedEventArgs.Handled;

			foreach (var action in _sizeChangedHandlers)
			{
				action(this, baseSizeChanged);
			}
		}

		internal Brush Background
		{
			get => _background;
			set
			{
				_background = value;

				if (_backgroundChangedHandlers != null)
				{
					foreach (var action in _backgroundChangedHandlers)
					{
						action(this, EventArgs.Empty);
					}
				}
			}
		}

		internal IDisposable RegisterBackgroundChangedEvent(EventHandler handler)
			=> WeakEventHelper.RegisterEvent(
				_backgroundChangedHandlers ??= new(),
				handler,
				(h, s, e) =>
					(h as EventHandler)?.Invoke(s, (EventArgs)e)
			);

		private static Window InternalGetCurrentWindow() => _current ??= new Window(true);

		private UIElement InternalGetContent() => _content!;

		private UIElement InternalGetRootElement() => _rootVisual!;

		#region Drag and Drop
		private DragRoot _dragRoot;

		internal DragDropManager DragDrop { get; private set; }

		private void InitDragAndDrop()
		{
			var coreManager = CoreDragDropManager.CreateForCurrentView(); // So it's ready to be accessed by ui manager and platform extension
			var uiManager = DragDrop = new DragDropManager(this);

			coreManager.SetUIManager(uiManager);
		}

		internal IDisposable OpenDragAndDrop(DragView dragView)
		{
			var rootElement = _rootVisual;

			if (rootElement is null)
			{
				return Disposable.Empty;
			}

			if (_dragRoot is null)
			{
				_dragRoot = new DragRoot();
				rootElement.Children.Add(_dragRoot);
			}

			_dragRoot.Show(dragView);

			return Disposable.Create(Remove);

			void Remove()
			{
				_dragRoot.Hide(dragView);

				if (_dragRoot.PendingDragCount == 0)
				{
					rootElement.Children.Remove(_dragRoot);
					_dragRoot = null;
				}
			}
		}
		#endregion
	}
}
