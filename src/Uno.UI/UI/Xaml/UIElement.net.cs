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

		private Rect _arranged;

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

		public int MeasureCallCount { get; protected set; }
		public int ArrangeCallCount { get; protected set; }

		public Size? RequestedDesiredSize { get; set; }
		public Size AvailableMeasureSize { get; protected set; }

		public Rect Arranged
		{
			get => _arranged;
			set
			{
				ArrangeCallCount++;
				_arranged = value;
			}
		}

		public Func<Size, Size> DesiredSizeSelector { get; set; }

		public IntPtr Handle { get; set; }

		internal Windows.Foundation.Point GetPosition(Point position, global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new NotSupportedException();
		}

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newVisibility)
		{
		}

		public string ShowLocalVisualTree(int fromHeight = 1000) => Uno.UI.ViewExtensions.ShowLocalVisualTree(this, fromHeight);
	}
}
