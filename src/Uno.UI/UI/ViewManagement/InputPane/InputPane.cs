using System;
using Windows.Foundation;
using Uno.UI;
using Uno;
using Uno.UI.Controls;

namespace Windows.UI.ViewManagement
{
	public partial class InputPane
	{
		#region Static

		private static InputPane _instance = new InputPane();

		public static InputPane GetForCurrentView() => _instance;

		#endregion

		private Rect _occludedRect = new Rect(0, 0, 0, 0);

		public event TypedEventHandler<InputPane, InputPaneVisibilityEventArgs> Hiding;
		public event TypedEventHandler<InputPane, InputPaneVisibilityEventArgs> Showing;

		public Rect OccludedRect
		{
			get => _occludedRect;
			internal set
			{
				if (_occludedRect != value)
				{
					_occludedRect = value;
					OnOccludedRectChanged();
				}
			}
		}

		public bool Visible
		{
			get => OccludedRect.Height > 0;
			set
			{
				if (value)
				{
					TryShow();
				}
				else
				{
					TryHide();
				}
			}
		}

#if __IOS__
		[NotImplemented]
#endif
		public bool TryShow()
		{
			if (Visible)
			{
				return false;
			}

			TryShowPartial();
			return true;
		}

		partial void TryShowPartial();

		public bool TryHide()
		{
			if (!Visible)
			{
				return false;
			}

			TryHidePartial();
			return true;
		}

		partial void TryHidePartial();

		internal void OnOccludedRectChanged()
		{
			var args = new InputPaneVisibilityEventArgs(OccludedRect);

			if (Visible)
			{
				Showing?.Invoke(this, args);
			}
			else
			{
				Hiding?.Invoke(this, args);
			}

			if (!args.EnsuredFocusedElementInView)
			{
				// Wait for proper element to be focused
				UI.Core.CoreDispatcher.Main.RunAsync(
					UI.Core.CoreDispatcherPriority.Normal,
					() => EnsureFocusedElementInViewPartial()
				);
			}
		}

		partial void EnsureFocusedElementInViewPartial();
	}
}
