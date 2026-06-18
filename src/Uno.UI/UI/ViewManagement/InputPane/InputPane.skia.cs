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
	private IDisposable _padScrollContentPresenter;

	/// <summary>
	/// The window the pane currently targets, set by the focusing control. There is a single
	/// OS keyboard, so the pane stays a singleton but addresses the focused control's window
	/// (correct placement/occlusion in multi-window and multi-display setups).
	/// </summary>
	internal XamlRoot? TargetXamlRoot { get; set; }

	// Test seam: when set, platform show/hide route here instead of the registered extension.
	internal static IInputPaneExtension? ExtensionForTesting { get; private set; }

	internal static IDisposable SetExtensionForTesting(IInputPaneExtension extension)
	{
		var previous = ExtensionForTesting;
		ExtensionForTesting = extension;
		GetForCurrentView().OccludedRect = default;
		return Uno.Disposables.Disposable.Create(() =>
		{
			ExtensionForTesting = previous;
			GetForCurrentView().OccludedRect = default;
		});
	}

	partial void InitializePlatform()
	{
		_inputPaneExtension = new(() =>
		{
			ApiExtensibility.CreateInstance<IInputPaneExtension>(this, out var extension);
			return extension;
		});
	}

	private bool TryShowPlatform() => (ExtensionForTesting ?? _inputPaneExtension?.Value)?.TryShow() ?? false;

	private bool TryHidePlatform() => (ExtensionForTesting ?? _inputPaneExtension?.Value)?.TryHide() ?? false;

	partial void EnsureFocusedElementInViewPartial()
	{
		_padScrollContentPresenter?.Dispose(); // Restore padding

		// Use the window the pane currently targets (set by the focusing control), falling back
		// to the initial window for direct API callers. This keeps pan-into-view correct in
		// multi-window apps instead of always acting on the initial window.
		var xamlRoot = TargetXamlRoot ?? Window.InitialWindow?.Content?.XamlRoot;

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

				var focusedElementPoint = focusedElement.TransformToVisual(scp).TransformPoint(new Point());
				Size focusedElementSize = focusedElement.ActualSize.ToSize();
				Rect focusedElementRect = new(
					focusedElementPoint.X,
					focusedElementPoint.Y,
					focusedElementSize.Width,
					focusedElementSize.Height
				);

				_padScrollContentPresenter = scp.Pad(OccludedRect, focusedElementRect);
			}

			// As we changed the layout properties of the ScrollContentPresenter, we need to wait for the next layout pass for
			// the scrollable height to be updated.
			_ = UI.Core.CoreDispatcher.Main.RunAsync(
				UI.Core.CoreDispatcherPriority.Normal, () => focusedElement.StartBringIntoView()
			);
		}
	}
}
