#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.UI.Extensions;
using Windows.Foundation;

namespace Windows.UI.ViewManagement;

partial class InputPane
{
	private Lazy<IInputPaneExtension?>? _inputPaneExtension;
	private IDisposable? _padScrollContentPresenter;
	private ScrollContentPresenter? _paddedScrollContentPresenter;

	partial void InitializePlatform()
	{
		_inputPaneExtension = new(() =>
		{
			ApiExtensibility.CreateInstance<IInputPaneExtension>(this, out var extension);
			return extension;
		});
	}

	private bool TryShowPlatform() => _inputPaneExtension?.Value?.TryShow() ?? false;

	private bool TryHidePlatform() => _inputPaneExtension?.Value?.TryHide() ?? false;

	partial void EnsureFocusedElementInViewPartial()
	{
		var initialWindow = Window.InitialWindow;
		if (initialWindow is null)
		{
			return;
		}

		var xamlRoot = initialWindow.Content?.XamlRoot;

		UIElement? focusedElement = null;
		ScrollContentPresenter? scp = null;

		if (xamlRoot is not null && Visible)
		{
			focusedElement = FocusManager.GetFocusedElement(xamlRoot) as UIElement;
			scp = focusedElement?.FindFirstParent<ScrollContentPresenter>();

			// ScrollViewer can be nested, but the outer-most SV isn't necessarily the one to handle this "padded" scroll.
			// Only the first SV that is constrained would be the one, as unconstrained SV can just expand freely.
			while (scp is not null
				&& double.IsPositiveInfinity(scp.m_previousAvailableSize.Height)
				&& scp.FindFirstParent<ScrollContentPresenter>(includeCurrent: false) is { } outerScv)
			{
				scp = outerScv;
			}
		}

		if (_paddedScrollContentPresenter is not null && _paddedScrollContentPresenter != scp)
		{
			// The occlusion no longer targets this presenter (focus moved or the pane hid): restore it.
			_padScrollContentPresenter?.Dispose();
			_padScrollContentPresenter = null;
			_paddedScrollContentPresenter = null;
		}

		if (focusedElement is null)
		{
			return;
		}

		if (scp is not null)
		{
			// Deliberately no restore-then-re-pad for the same presenter: the occlusion is reported
			// continuously while the keyboard animates, and restoring first would make Pad measure a
			// viewport whose layout still reflects the previous padding. Pad compensates internally.
			scp.UpdateLayout();
			_padScrollContentPresenter = scp.Pad(OccludedRect);
			_paddedScrollContentPresenter = scp;
		}

		// As we changed the layout properties of the ScrollContentPresenter, we need to wait for the next layout pass for
		// the scrollable height to be updated.
		_ = UI.Core.CoreDispatcher.Main.RunAsync(
			UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				focusedElement.UpdateLayout();
				focusedElement.StartBringIntoView();
			}
		);
	}
}
