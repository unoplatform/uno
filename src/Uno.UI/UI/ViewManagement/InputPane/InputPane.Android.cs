using System;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views.InputMethods;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.UI.Input;

namespace Windows.UI.ViewManagement
{
	public partial class InputPane
	{
		private IDisposable _padScrollContentPresenter;

		private bool TryShowPlatform() => InputPaneInterop.TryShow(ContextHelper.Current as Activity);

		private bool TryHidePlatform() => InputPaneInterop.TryHide(ContextHelper.Current as Activity);

		partial void EnsureFocusedElementInViewPartial()
		{
			_padScrollContentPresenter?.Dispose(); // Restore padding

			var initialWindow = Window.InitialWindow;
			if (initialWindow is null)
			{
				return;
			}

			var xamlRoot = initialWindow.Content?.XamlRoot;

			if (xamlRoot is not null && Visible && FocusManager.GetFocusedElement(xamlRoot) is UIElement focusedElement)
			{
				if (focusedElement.FindFirstParent<ScrollContentPresenter>() is { } scp)
				{
					// ScrollViewer can be nested, but the outer-most SV isn't necessarily the one to handle this "padded" scroll.
					// Only the first SV that is constrained would be the one, as unconstrained SV can just expand freely.
					while (double.IsPositiveInfinity(scp.m_previousAvailableSize.Height)
						&& scp.FindFirstParent<ScrollContentPresenter>(includeCurrent: false) is { } outerScv)
					{
						scp = outerScv;
					}

					_padScrollContentPresenter = scp.Pad(OccludedRect);
				}
				focusedElement.StartBringIntoView();
			}
		}
	}
}
