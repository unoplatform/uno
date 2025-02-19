using System.Collections.Generic;
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
			=> new List<PointerPoint> { CurrentPoint };

#nullable enable
		/// <summary>
		/// Gets the dispatch result of this event, if any.
		/// This is defined by the InputManager if the event goes though it.
		/// </summary>
		internal object? DispatchResult { get; set; }
#nullable restore

		/// <inheritdoc />
		public override string ToString()
			=> $"{CurrentPoint} | modifiers: {KeyModifiers}";
	}
}
