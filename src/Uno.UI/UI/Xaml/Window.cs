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
using Windows.UI.Xaml.Controls;
using Uno.Logging;
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Represents an application window.
	/// </summary>
	public sealed partial class Window
	{
		private CoreWindowActivationState? _lastActivationState;

		private List<WeakEventHelper.GenericEventHandler> _sizeChangedHandlers = new List<WeakEventHelper.GenericEventHandler>();

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
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
				{
					this.Log().Warn("Unable to raise WindowCreatedEvent, there is no active Application");
				}
			}
		}

		internal Canvas FocusVisualLayer { get; private set; }

		public UIElement Content
		{
			get => InternalGetContent();
			set
			{
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
				XamlRoot.Current.NotifyChanged();
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
		internal IDisposable RegisterSizeChangedEvent(Windows.UI.Xaml.WindowSizeChangedEventHandler handler)
		{
			return WeakEventHelper.RegisterEvent(
				_sizeChangedHandlers,
				handler,
				(h, s, e) =>
					(h as Windows.UI.Xaml.WindowSizeChangedEventHandler)?.Invoke(s, (Windows.UI.Core.WindowSizeChangedEventArgs)e)
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

				var args = new VisibilityChangedEventArgs();
				CoreWindow.OnVisibilityChanged(args);
				VisibilityChanged?.Invoke(this, args);
			}
		}

		private void RootSizeChanged(object sender, SizeChangedEventArgs args)
		{
			XamlRoot.Current.NotifyChanged();
		}

		private void RaiseSizeChanged(Windows.UI.Core.WindowSizeChangedEventArgs windowSizeChangedEventArgs)
		{
			SizeChanged?.Invoke(this, windowSizeChangedEventArgs);
			CoreWindow.GetForCurrentThread()?.OnSizeChanged(windowSizeChangedEventArgs);

			foreach (var action in _sizeChangedHandlers)
			{
				action(this, windowSizeChangedEventArgs);
			}
		}

#region Drag and Drop
		private DragRoot _dragRoot;

		internal DragDropManager DragDrop { get; private set; }

		private void InitDragAndDrop()
		{
			DragDrop = new DragDropManager(this);
			CoreDragDropManager.SetForCurrentView(DragDrop);
		}

		internal IDisposable OpenDragAndDrop(DragView dragView)
		{
#if __WASM__ || __SKIA__
			Grid rootElement = _window;
#else
			Grid rootElement = _main;
#endif

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
