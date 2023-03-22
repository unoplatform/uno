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

#if __ANDROID__
		internal PointerEventArgs(PointerPoint currentPoint, VirtualKeyModifiers keyModifiers, Android.Views.MotionEvent motionEvent)
			: this(currentPoint, keyModifiers)
		{
			MotionEvent = motionEvent;
		}

		internal Android.Views.MotionEvent MotionEvent { get; }
#endif

		public bool Handled { get; set; }

		public PointerPoint CurrentPoint { get; }

		public VirtualKeyModifiers KeyModifiers { get; }

		public IList<PointerPoint> GetIntermediatePoints()
			=> new List<PointerPoint> { CurrentPoint };

		/// <inheritdoc />
		public override string ToString()
			=> $"{CurrentPoint} | modifiers: {KeyModifiers}";
	}
}
