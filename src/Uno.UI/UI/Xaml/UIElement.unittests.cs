using System;
using System.Collections.Generic;
using System.Text;
using Uno;
using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml
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

		internal bool IsMeasureDirtyPath => false;

		internal bool IsArrangeDirtyPath => false;

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

		internal Func<Size, Size> DesiredSizeSelector { get; set; }

		public IntPtr Handle { get; set; }

		internal Windows.Foundation.Point GetPosition(Point position, global::Microsoft.UI.Xaml.UIElement relativeTo)
		{
			throw new NotSupportedException();
		}

		public string ShowLocalVisualTree(int fromHeight = 1000) => Uno.UI.ViewExtensions.ShowLocalVisualTree(this, fromHeight);

		//TODO Uno: This is currently just a stub, should be implemented properly for tests
		[NotImplemented]
		internal void AddChild(UIElement child, int? index = null)
		{
			child.SetParent(this);
		}

		partial void OnMeasurePartial(Size slotSize)
		{
			MeasureCallCount++;
			AvailableMeasureSize = slotSize;

			if (DesiredSizeSelector != null)
			{
				var desiredSize = DesiredSizeSelector(slotSize);

				LayoutInformation.SetDesiredSize(this, desiredSize);
				RequestedDesiredSize = desiredSize;
			}
			else if (RequestedDesiredSize != null)
			{
				var desiredSize = RequestedDesiredSize.Value;

				LayoutInformation.SetDesiredSize(this, desiredSize);
			}
		}
	}
}
