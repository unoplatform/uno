using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		public UIElement()
		{
			Initialize();
			InitializePointers();
		}

		internal bool ShouldInterceptInvalidate { get; set; }

		public IntPtr Handle { get; }

		public string Name { get; set; }

		/// <summary>
		/// Determines if InvalidateMeasure has been called
		/// </summary>
		internal bool IsMeasureDirty => false;

		/// <summary>
		/// Determines if InvalidateArrange has been called
		/// </summary>
		internal bool IsArrangeDirty => false;

		internal bool IsPointerCaptured { get; set; }

		internal Size LastAvailableSize => Size.Empty;

		internal Windows.Foundation.Point GetPosition(Point position, global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new NotSupportedException();
		}

		internal void ClearChildren() { }

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newVisibility)
		{
		}
	}
}
