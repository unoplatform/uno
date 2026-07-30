using System.Collections.Generic;
using System.ComponentModel;
using Windows.System;
using Windows.UI.Input;

namespace Windows.UI.Core
{
	public partial class PointerEventArgs : ICoreWindowEventArgs
	{
		internal PointerEventArgs(PointerPoint currentPoint, VirtualKeyModifiers keyModifiers)
		{
			CurrentPoint = currentPoint;
			KeyModifiers = keyModifiers;
		}

		public bool Handled { get; set; }

		public PointerPoint CurrentPoint { get; }

		public VirtualKeyModifiers KeyModifiers { get; }

		public IList<PointerPoint> GetIntermediatePoints()
			=> [CurrentPoint];

#nullable enable
		/// <summary>
		/// Gets the dispatch result of this event, if any.
		/// This is defined by the InputManager if the event goes though it.
		/// </summary>
		internal object? DispatchResult { get; set; }

		/// <summary>
		/// Optional UI element that scopes hit-testing for injected pointer events.
		/// When non-null, the input manager uses this element as the hit-test search
		/// root instead of the <see cref="Microsoft.UI.Xaml.XamlRoot"/>'s root element.
		/// Used to bypass design-time overlays (e.g. Hot Design) when automation
		/// tools need to drive the inner application beneath the overlay.
		/// </summary>
		/// <remarks>
		/// Typed as <c>object?</c> because <c>Windows.UI.Core</c> cannot reference
		/// <c>Microsoft.UI.Xaml.UIElement</c> without creating a circular dependency.
		/// Consumers cast via <c>as UIElement</c> at the point of use.
		/// Visible to <c>Uno.UI</c> via <c>InternalsVisibleTo</c>.
		/// </remarks>
		internal object? RelativeRoot { get; set; }
#nullable restore

		/// <inheritdoc />
		public override string ToString()
			=> $"{CurrentPoint} | modifiers: {KeyModifiers}";
	}
}
