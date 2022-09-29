#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Foldable
{
	/// <summary>
	/// Exposes Jetpack Window Manager fold information (Bounds, OcclusionType, State, Orientation)
	/// internal use only (for now)
	/// </summary>
	/// <remarks>
	/// Consider moving this class to Uno.UI core if it needs to be accessed by application code for
	/// more sophisticated dual-screen/foldable functionality. For now, it's only used internally
	/// in the Uno.UI.Foldable project
	/// 
	/// https://docs.microsoft.com/dual-screen/android/jetpack/window-manager/
	/// https://developer.android.com/jetpack/androidx/releases/window
	/// </remarks>
	internal class NativeFold
	{
		public Rect Bounds { get; set; }

		public bool IsOccluding { get; set; }

		public bool IsFlat { get; set; }

		public bool IsVertical { get; set; }
	}
}
