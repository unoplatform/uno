#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.UI.Input;

namespace Windows.UI.Core
{
	public partial class PointerEventArgs : ICoreWindowEventArgs
	{
		internal PointerEventArgs(PointerPoint currentPoint)
		{
			CurrentPoint = currentPoint;
		}

		public bool Handled
		{
			get;
			set;
		}

		public PointerPoint CurrentPoint { get; }

		[global::Uno.NotImplemented]
		public global::Windows.System.VirtualKeyModifiers KeyModifiers
			=> throw new global::System.NotImplementedException("The member VirtualKeyModifiers PointerEventArgs.KeyModifiers is not implemented in Uno.");

		[global::Uno.NotImplemented]
		public global::System.Collections.Generic.IList<global::Windows.UI.Input.PointerPoint> GetIntermediatePoints()
			=> throw new global::System.NotImplementedException("The member IList<PointerPoint> PointerEventArgs.GetIntermediatePoints() is not implemented in Uno.");
	}
}
