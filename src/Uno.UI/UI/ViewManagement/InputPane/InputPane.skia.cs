#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Extensibility;
using Uno.UI.Extensions;

namespace Windows.UI.ViewManagement;

partial class InputPane
{
	private Lazy<IInputPaneExtension?>? _inputPaneExtension;
	private IDisposable _padScrollContentPresenter;

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

			// As we changed the layout properties of the ScrollContentPresenter, we need to wait for the next layout pass for
			// the scrollable height to be updated.
			_ = UI.Core.CoreDispatcher.Main.RunAsync(
				UI.Core.CoreDispatcherPriority.Normal, () => focusedElement.StartBringIntoView()
			);
		}
	}
}
