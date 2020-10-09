using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	[TestFixture]
	public class ScrollViewer_Pointer_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Parent_PointerMoved()
		{
			Run("UITests.Windows_UI_Input.PointersTests.ScrollViewer_PointerMoved");

			_app.WaitForElement("HostGrid");

			var gridRect = _app.GetRect("HostGrid");

			//Directly above host grid
			var leftLow = gridRect.GetPointInRect(new PointF(0.15f, 0.8f));
			var leftHigh = gridRect.GetPointInRect(new PointF(0.15f, 0.2f));

			//Above vertically-scrollable ScrollViewer
			var centerLow = gridRect.GetPointInRect(new PointF(0.5f, 0.8f));
			var centerHigh = gridRect.GetPointInRect(new PointF(0.5f, 0.2f));

			//Above unscrollable ScrollViewer
			var rightLow = gridRect.GetPointInRect(new PointF(0.85f, 0.8f));
			var rightHigh = gridRect.GetPointInRect(new PointF(0.85f, 0.2f));

			WaitForUndragged();

			_app.DragCoordinates(leftLow, leftHigh);
			WaitForDragged();
			Reset();

			_app.DragCoordinates(centerLow, centerHigh);
			if (IsTouchInteraction)
			{
				WaitForUndragged(); // Interaction is swallowed by ScrollViewer in touch
			}
			else
			{
				WaitForDragged(); // When using mouse the ScrollViewer will not swallow the drag
			}
			Reset();

			_app.DragCoordinates(rightLow, rightHigh);
			WaitForDragged(); // ScrollViewer is unscrollable, PointerMoved will be surfaced to parent (in both touch and mouse mode)


			void WaitForUndragged() => _app.WaitForText("StatusTextBlock", "Not dragged");

			void WaitForDragged() => _app.WaitForText("StatusTextBlock", "DragFull");

			void Reset()
			{
				_app.FastTap("ResetColorButton");
				WaitForUndragged();
			}
		}
	}
}
