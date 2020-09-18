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
			_isFrameworkElement = this is FrameworkElement; // Avoids unused field error
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

		internal void AddChild(UIElement child, int? index = null) => throw new NotSupportedException("Reference assembly");
		internal void MoveChildTo(int oldIndex, int newIndex) => throw new NotSupportedException("Reference assembly");
		internal bool RemoveChild(UIElement child) => throw new NotSupportedException("Reference assembly");
		internal void ClearChildren() => throw new NotSupportedException("Reference assembly");

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newVisibility)
		{
		}
	}
}
