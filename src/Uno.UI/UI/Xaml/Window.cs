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

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private List<WeakEventHelper.GenericEventHandler> _sizeChangedHandlers = new List<WeakEventHelper.GenericEventHandler>();

#pragma warning disable 67
		public event WindowActivatedEventHandler Activated;
		public event WindowClosedEventHandler Closed;
		public event WindowSizeChangedEventHandler SizeChanged;
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

		public Rect Bounds { get; private set; }

		public CoreWindow CoreWindow { get; private set; }

		public CoreDispatcher Dispatcher { get; private set; }

		public bool Visible { get; private set; }

		public static Window Current => InternalGetCurrentWindow();

		public void Activate()
		{
			InternalActivate();
			Activated?.Invoke(this, new WindowActivatedEventArgs(CoreWindowActivationState.CodeActivated));
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
