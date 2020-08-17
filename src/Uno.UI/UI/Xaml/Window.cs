using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno.Disposables;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
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
			get { return InternalGetContent(); }
			set
			{
				var oldContent = Content;
				if (oldContent != null)
				{
					oldContent.IsWindowRoot = false;
				}
				if (value != null)
				{
					value.IsWindowRoot = true;
				}

				InternalSetContent(value);
			}
		}

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

		private void RaiseSizeChanged(Windows.UI.Core.WindowSizeChangedEventArgs windowSizeChangedEventArgs)
		{
			SizeChanged?.Invoke(this, windowSizeChangedEventArgs);
			CoreWindow.GetForCurrentThread()?.OnSizeChanged(windowSizeChangedEventArgs);

			foreach (var action in _sizeChangedHandlers)
			{
				action(this, windowSizeChangedEventArgs);
			}
		}
	}
}
