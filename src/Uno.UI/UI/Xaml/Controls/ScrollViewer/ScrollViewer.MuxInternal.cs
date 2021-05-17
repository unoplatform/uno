#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Automation.Peers;
using DirectUI;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		private IDisposable? _directManipulationHandlerSubscription;

		internal bool m_templatedParentHandlesMouseButton;

		internal bool IsInDirectManipulation { get; }
		internal bool TemplatedParentHandlesScrolling { get; set; }
		internal Func<AutomationPeer>? AutomationPeerFactoryIndex { get; set; }

		internal void SetDirectManipulationStateChangeHandler(IDirectManipulationStateChangeHandler? handler)
		{
			_directManipulationHandlerSubscription?.Dispose();

			if (handler is null)
			{
				return;
			}

			var weakHandler = WeakReferencePool.RentWeakReference(this, handler);
			UpdatesMode = ScrollViewerUpdatesMode.Synchronous;
			ViewChanged += OnViewChanged;
			_directManipulationHandlerSubscription = Disposable.Create(() =>
			{
				ViewChanged -= OnViewChanged;
				WeakReferencePool.ReturnWeakReference(this, weakHandler);
			});

			void OnViewChanged(object sender, ScrollViewerViewChangedEventArgs args)
			{
				if (args.IsIntermediate)
				{
					return;
				}

				if (weakHandler.Target is IDirectManipulationStateChangeHandler h)
				{
					h.NotifyStateChange(DMManipulationState.DMManipulationCompleted, default, default, default, default, default, default, default, default);
				}
			}
		}
	}

	internal interface IDirectManipulationStateChangeHandler
	{
		// zCumulativeFactor: if the zoom factor was 1.5 at the beginning of the manipulation,
		// and the current zoom factor is 3.0, then zCumulativeFactor is 2.0.
		// xCenter/yCenter: these coordinates are in relation to the top/left corner of the
		// manipulated element. They might be negative if the ScrollViewer content is smaller
		// than the viewport.
		void NotifyStateChange(
			DMManipulationState state,
			float xCumulativeTranslation,
			float yCumulativeTranslation,
			float zCumulativeFactor,
			float xCenter,
			float yCenter,
			bool isInertial,
			bool isTouchConfigurationActivated,
			bool isBringIntoViewportConfigurationActivated);
	}
}
