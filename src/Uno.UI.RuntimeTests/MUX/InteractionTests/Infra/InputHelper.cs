// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/test/testinfra/MUXTestInfra/Common/InputHelper.cs, commit fee1e8acc

using Common;
using System;

using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Controls;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;
using Point = System.Drawing.Point;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common
{
	public enum Direction
	{
		North,
		NorthEast,
		East,
		SouthEast,
		South,
		SouthWest,
		West,
		NorthWest
	}

	// Helper class to abstract some of Microsoft.Windows.Apps.Test's input methods.
	public static class InputHelper
	{
		public const uint DefaultPanHoldDuration = 100;
		public const float DefaultPanAcceleration = 0.1f;
		public const uint DefaultDragDuration = 100;

		private enum InputEvent
		{
			LeftClick,
			RightClick,
			Tap,
			Pan,
			Flick,
			Pinch,
			Stretch,
			Drag,
			MouseDown,
			MouseUp,
			MouseMove,
		}

		public static async Task LeftClick(UIObject obj, bool skipWait = false)
		{
			Log.Comment("Left-click on {0}.", obj.GetIdentifier());
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.LeftClick))
			{
				obj.Click(PointerButtons.Primary);
			}
			if (!skipWait)
			{
				await Wait.ForIdle();
			}
		}

		public static async Task Click(UIObject obj, PointerButtons pointerButton, bool skipWait = false)
		{
			Log.Comment("Click on {0} with pointer button '{1}'.", obj.GetIdentifier(), pointerButton);
			obj.Click(pointerButton);
			if (!skipWait)
			{
				await Wait.ForIdle();
			}
		}

		public static async Task LeftClick(UIObject obj, double offsetX, double offsetY, bool skipWait = false)
		{
			Log.Comment("Left-click on {0}, at ({1}, {2}).", obj.GetIdentifier(), offsetX, offsetY);
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.LeftClick))
			{
				obj.Click(PointerButtons.Primary, offsetX, offsetY);
			}
			if (!skipWait)
			{
				await Wait.ForIdle();
			}
		}

		public static async Task LeftDoubleClick(UIObject obj)
		{
			Log.Comment("Left double-click on {0}.", obj.GetIdentifier());
			obj.DoubleClick(PointerButtons.Primary);
			await Wait.ForIdle();
		}

		public static async Task LeftDoubleClick(UIObject obj, double offsetX, double offsetY)
		{
			Log.Comment("Left double-click on {0}, at ({1}, {2}).", obj.GetIdentifier(), offsetX, offsetY);
			obj.DoubleClick(PointerButtons.Primary, offsetX, offsetY);
			await Wait.ForIdle();
		}

		public static async Task RightClick(UIObject obj, int offsetX = 0, int offsetY = 0)
		{
			if (offsetX == 0 && offsetY == 0)
			{
				Log.Comment("Right-click on {0}.", obj.GetIdentifier());
			}
			else
			{
				Log.Comment("Right-click on {0} at offset ({1}, {2}).", obj.GetIdentifier(), offsetX, offsetY);
			}

			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.RightClick))
			{
				obj.Click(PointerButtons.Secondary, offsetX, offsetY);
			}

			await Wait.ForIdle();
		}

		public static async Task RotateWheel(UIObject obj, int mouseWheelDelta)
		{
			Log.Comment("RotateWheel on {0} with mouseWheelDelta: {1}.", obj.GetIdentifier(), mouseWheelDelta);
			MouseWheelInput.RotateWheel(obj, mouseWheelDelta);
			await Wait.ForIdle();
		}

		public static async Task LeftMouseButtonDown(UIObject obj, int offsetX = 0, int offsetY = 0)
		{
			if (offsetX == 0 && offsetY == 0)
			{
				Log.Comment("Left mouse button down on {0}.", obj.GetIdentifier());
			}
			else
			{
				Log.Comment("Left mouse button down on {0} at offset ({1}, {2}).",
					obj.GetIdentifier(), offsetX, offsetY);
			}

			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.MouseDown))
			{
				var point = obj.GetClickablePoint();

				point.X += offsetX;
				point.Y += offsetY;

				PointerInput.Move(point);
				PointerInput.Press(PointerButtons.Primary);
			}
			await Wait.ForIdle();
		}

		public static async Task LeftMouseButtonUp(UIObject obj = null, int offsetX = 0, int offsetY = 0)
		{
			if (obj == null)
			{
				Log.Comment("Left mouse button up.");
			}
			else if (offsetX == 0 && offsetY == 0)
			{
				Log.Comment("Left mouse button up on {0}.", obj.GetIdentifier());
			}
			else
			{
				Log.Comment("Left mouse button up on {0} at offset ({1}, {2}).",
					obj.GetIdentifier(), offsetX, offsetY);
			}

			if (obj != null)
			{
				using (var waiter = GetWaiterForInputEvent(obj, InputEvent.MouseUp))
				{
					var point = obj.GetClickablePoint();

					point.X += offsetX;
					point.Y += offsetY;

					PointerInput.Move(point);
					PointerInput.Release(PointerButtons.Primary);
				}
			}
			else
			{
				PointerInput.Release(PointerButtons.Primary);
			}

			await Wait.ForIdle();
		}

		public static async Task MoveMouse(UIObject obj, int offsetX, int offsetY)
		{
			Log.Comment("Move mouse on {0} to offset ({1}, {2}).", obj.GetIdentifier(), offsetX, offsetY);
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.MouseMove))
			{
				var point = obj.GetClickablePoint();

				point.X += offsetX;
				point.Y += offsetY;

				PointerInput.Move(point);
			}
			await Wait.ForIdle();
		}

		public static async Task MoveMouse(Point point)
		{
			Log.Comment("Move mouse to point ({0}, {1}).", point.X, point.Y);
			PointerInput.Move(point);
			await Wait.ForIdle();
		}

		public static async Task TapAndHold(UIObject obj, double offsetX, double offsetY, uint durationMs)
		{
			Log.Comment("Tap and hold on {0} at ({1}, {2}) for {3} ms.", obj.GetIdentifier(), offsetX, offsetY, durationMs);
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Tap))
			{
				obj.TapAndHold(offsetX, offsetY, durationMs);
			}
			await Wait.ForIdle();
		}

		public static async Task Tap(UIObject obj)
		{
			// Allowing three attempts to work around occasional failure on Phone builds.
			int retries = 3;
			while (retries > 0)
			{
				try
				{
					Log.Comment("Tap on {0}.", obj.GetIdentifier());
					using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Tap))
					{
						obj.Tap();
					}

					retries = 0;
				}
				catch (Exception e)
				{
					Log.Warning("Exception while tapping: " + e.Message);
					retries--;
				}
			}

			await Wait.ForIdle();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="offsetX">value from top left</param>
		/// <param name="offsetY">value from top left</param>
		public static async Task Tap(UIObject obj, double offsetX, double offsetY)
		{
			// Allowing three attempts to work around occasional failure on Phone builds.
			int retries = 3;
			while (retries > 0)
			{
				try
				{
					Log.Comment("Tap on {0} at ({1}, {2}).", obj.GetIdentifier(), offsetX, offsetY);
					Log.Comment("Bounding rectangle is: " + obj.BoundingRectangle.ToString());

					if (offsetX > obj.BoundingRectangle.Width)
					{
						Log.Warning("Warning, the tap offsetX is higher than the bounding rectangle width");
					}

					if (offsetY > obj.BoundingRectangle.Height)
					{
						Log.Warning("Warning, the tap offsetY is higher than the bounding rectangle height");
					}

					using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Tap))
					{
						obj.Tap(offsetX, offsetY);

						await (waiter?.Wait() ?? Task.CompletedTask);
					}

					retries = 0;
				}
				catch (Exception e)
				{
					Log.Warning("Exception while tapping: " + e.Message);
					retries--;
				}
			}

			await Wait.ForIdle();
		}

		public static async Task TapAndHold(UIObject obj, uint durationMs)
		{
			Log.Comment("Tap and hold on {0} for {1} ms.", obj.GetIdentifier(), durationMs);
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Tap))
			{
				obj.TapAndHold(durationMs);
			}
			await Wait.ForIdle();
		}

		public static async Task DoubleTap(UIObject obj)
		{
			try
			{
				Log.Comment("Double-tap on {0}.", obj.GetIdentifier());
				using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Tap))
				{
					obj.DoubleTap();
				}
			}
			catch (Exception e)
			{
				Log.Warning("Exception while double-tapping: " + e.Message);
			}

			await Wait.ForIdle();
		}

		public static async Task DoubleTap(UIObject obj, double offsetX, double offsetY)
		{
			try
			{
				Log.Comment("Double-tap on {0} at ({1}, {2}).", obj.GetIdentifier(), offsetX, offsetY);
				using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Tap))
				{
					obj.DoubleTap(offsetX, offsetY);
				}
			}
			catch (Exception e)
			{
				Log.Warning("Exception while double-tapping: " + e.Message);
			}

			await Wait.ForIdle();
		}

		public static async Task Pan(UIObject obj, int distance, Direction direction, uint holdDuration = DefaultPanHoldDuration, float panAcceleration = DefaultPanAcceleration, uint dragDuration = DefaultDragDuration, bool waitForIdle = true)
		{
			Log.Comment("Pan on {0} for {1} pixels in the {2} direction, after holding {3} ms.",
				obj.GetIdentifier(), distance, direction, holdDuration);
			Log.Comment("Clickable point of pan object is {0}", obj.GetClickablePoint());
			Log.Comment("Bounding rectangle of object is {0}", obj.BoundingRectangle);

			await Pan(obj, obj.GetClickablePoint(), distance, direction, holdDuration, panAcceleration, dragDuration, waitForIdle);
		}

		public static async Task Pan(UIObject obj, Point start, int distance, Direction direction, uint holdDuration = DefaultPanHoldDuration, float panAcceleration = DefaultPanAcceleration, uint dragDuration = DefaultDragDuration, bool waitForIdle = true)
		{
			Log.Comment("Pan on {0}, starting at ({1}, {2}), for {3} pixels in the {4} direction, after holding {5} ms.",
				obj.GetIdentifier(), start.X, start.Y, distance, direction, holdDuration);
			double directionRadians = DirectionToAngle(direction) * Math.PI / 180d;
			Point end = new Point()
			{
				X = start.X + (int)Math.Round(distance * Math.Cos(directionRadians)),
				Y = start.Y - (int)Math.Round(distance * Math.Sin(directionRadians))
			};

			await Pan(obj, start, end, holdDuration, panAcceleration, dragDuration, waitForIdle);
		}

		public static async Task Pan(UIObject obj, Point start, Point end, uint holdDuration = DefaultPanHoldDuration, float panAcceleration = DefaultPanAcceleration, uint dragDuration = DefaultDragDuration, bool waitForIdle = true)
		{
			Log.Comment("Pan on {0}, starting at ({1}, {2}), ending at ({3}, {4}), after holding {5} ms, with acceleration {6}.",
				obj.GetIdentifier(), start.X, start.Y, end.X, end.Y, holdDuration, panAcceleration);

			if (panAcceleration == 0.0f)
			{
				using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Drag))
				{
					SinglePointGesture.Current.Move(start);
					SinglePointGesture.Current.PressAndDrag(end, dragDuration, holdDuration);
				}
			}
			else
			{
				using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Pan))
				{
					SinglePointGesture.Current.Move(start);
					SinglePointGesture.Current.Pan(end, holdDuration, panAcceleration);
				}
			}

			if (waitForIdle)
			{
				await Wait.ForIdle();
			}
		}

		public static async Task DragToTarget(UIObject obj, UIObject obj2, int xOffset = 0, int yOffset = 0)
		{
			await DragToTargetHelper(obj, obj2, xOffset, yOffset, useMouse: false);
		}

		public static async Task MouseDragToTarget(UIObject obj, UIObject obj2, int xOffset = 0, int yOffset = 0)
		{
			await DragToTargetHelper(obj, obj2, xOffset, yOffset, useMouse: true);
		}

		public static async Task DragToTargetHelper(UIObject obj, UIObject obj2, int xOffset, int yOffset, bool useMouse)
		{
			Point startPoint = obj.GetClickablePoint();
			Log.Comment("Start Point X:{0} Y:{1}", startPoint.X, startPoint.Y);

			Point end = obj2.GetClickablePoint();
			end.X += xOffset;
			end.Y += yOffset;
			Log.Comment("End Point X:{0} Y:{1}", end.X, end.Y);

			uint dragDuration = 2000;

			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Drag))
			{
				Log.Comment("Move input to Start Point.");

				if (useMouse)
				{
					PointerInput.Move(startPoint);
				}
				else
				{
					SinglePointGesture.Current.Move(startPoint);
				}

				Log.Comment("Begin Drag.");

				if (useMouse)
				{
					PointerInput.ClickDrag(end, PointerButtons.Primary, dragDuration);
				}
				else
				{
					SinglePointGesture.Current.PressAndDrag(end, dragDuration, 500);
				}

				Log.Comment("Drag Complete.");
			}
			await Wait.ForIdle();
		}

		public static async Task DragDistance(UIObject obj, int distance, Direction direction, uint duration = 4000)
		{
			await DragDistanceHelper(obj, distance, direction, duration, useMouse: false);
		}

		public static async Task MouseDragDistance(UIObject obj, int distance, Direction direction, uint duration = 4000)
		{
			await DragDistanceHelper(obj, distance, direction, duration, useMouse: true);
		}

		private static async Task DragDistanceHelper(UIObject obj, int distance, Direction direction, uint duration, bool useMouse)
		{
			Log.Comment("Drag {0} {1} pixels in the {2} direction.", obj.GetIdentifier(), distance, direction);

			Point startPoint = obj.GetClickablePoint();
			double directionRadians = DirectionToAngle(direction) * Math.PI / 180d;

			Log.Comment("Start Point X:{0} Y:{1}", startPoint.X, startPoint.Y);

			Point end = new Point()
			{
				X = startPoint.X + (int)Math.Round(distance * Math.Cos(directionRadians)),
				Y = startPoint.Y - (int)Math.Round(distance * Math.Sin(directionRadians))
			};
			Log.Comment("End Point X:{0} Y:{1}", end.X, end.Y);

			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Drag))
			{
				Log.Comment("Move input to Start Point.");

				if (useMouse)
				{
					PointerInput.Move(startPoint);
				}
				else
				{
					SinglePointGesture.Current.Move(startPoint);
				}

				Log.Comment("Begin Drag.");

				if (useMouse)
				{
					PointerInput.ClickDrag(end, PointerButtons.Primary, duration);
				}
				else
				{
					SinglePointGesture.Current.ClickDrag(end, PointerButtons.Primary, duration);
				}

				Log.Comment("Drag Complete.");
			}
			await Wait.ForIdle();
		}

		public static async Task Flick(UIObject obj, int distance, Direction direction)
		{
			Log.Comment("Flick on {0} over {1} pixels in the {2} direction.", obj.GetIdentifier(), distance, direction);
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Flick))
			{
				obj.Flick(distance, DirectionToAngle(direction));
			}
			await Wait.ForIdle();
		}

		// TODO_WebView2Input - Task 30555367 - Stack Overflow in WebView2 pointer tests when (MUXC) test infra calls WV2.GetClickablePoint()
		//                      Workaround to avoid any calls to WebView2.GetClickablePoint
		// This helper can be removed when the other Flick works again on WebView2
		public static async Task Flick(UIObject obj, Point startPoint, int distance, Direction direction)
		{
			Log.Comment("Flick on {0} over {1} pixels in the {2} direction.", obj.GetIdentifier(), distance, direction);
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Flick))
			{
				obj.Flick(startPoint, distance, DirectionToAngle(direction));
			}
			await Wait.ForIdle();
		}

		public static async Task Pinch(UIObject obj)
		{
			Log.Comment("Pinch on {0}.", obj.GetIdentifier());
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Pinch))
			{
				MultiPointGesture.Pinch(obj);
			}
			await Wait.ForIdle();
		}

		public static async Task Pinch(UIObject obj, uint distance, Direction direction, bool pivot)
		{
			string id = obj.GetIdentifier();
			string pivotMode;
			if (pivot)
			{
				pivotMode = "one finger stationary";
			}
			else
			{
				pivotMode = "both fingers moving";
			}
			Log.Comment("Pinch with {0} on {1}, in the {2} direction, over a distance of {3} pixels.",
				pivotMode, id, direction, distance);

			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Pinch))
			{
				MultiPointGesture.Pinch(obj, distance, DirectionToAngle(direction), pivot);
			}
			await Wait.ForIdle();
		}

		public static async Task Stretch(UIObject obj)
		{
			Log.Comment("Stretch on {0}.", obj.GetIdentifier());
			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Pinch))
			{
				try
				{
					MultiPointGesture.Stretch(obj);
				}
				catch (Exception e)
				{
					Log.Error("Exception: {0} \n{1}", e.Message, e.StackTrace);
				}
			}
			await Wait.ForIdle();
		}

		public static async Task Stretch(UIObject obj, uint distance, Direction direction, bool pivot)
		{
			string id = obj.GetIdentifier();
			string pivotMode;
			if (pivot)
			{
				pivotMode = "one finger stationary";
			}
			else
			{
				pivotMode = "both fingers moving";
			}
			Log.Comment("Stretch with {0} on {1}, in the {2} direction, over a distance of {3} pixels.",
				pivotMode, id, direction, distance);

			using (var waiter = GetWaiterForInputEvent(obj, InputEvent.Pinch))
			{
				try
				{
					MultiPointGesture.Stretch(obj, distance, DirectionToAngle(direction), pivot);
				}
				catch (Exception e)
				{
					Log.Error("Exception: {0} \n{1}", e.Message, e.StackTrace);
				}
			}
			await Wait.ForIdle();
		}

		// Microsoft.Windows.Apps.Test's Pan input methods use angles to determine direction
		// with 0 corresponding to the positive x-axis and proceeding
		// counter-clockwise (up is 90, left is 180, and down is 270).
		private static float DirectionToAngle(Direction direction)
		{
			float angle = 0f;

			switch (direction)
			{
				default:
				case Direction.North:
					angle = 90f;
					break;
				case Direction.NorthEast:
					angle = 45f;
					break;
				case Direction.East:
					angle = 0f;
					break;
				case Direction.SouthEast:
					angle = 315f;
					break;
				case Direction.South:
					angle = 270f;
					break;
				case Direction.SouthWest:
					angle = 225f;
					break;
				case Direction.West:
					angle = 180f;
					break;
				case Direction.NorthWest:
					angle = 135f;
					break;
			}

			return angle;
		}

		private static Waiter GetWaiterForInputEvent(UIObject obj, InputEvent inputEvent)
		{
			Waiter waiter = null;

			if (obj is Button)
			{
				var button = obj as Button;
				if (inputEvent == InputEvent.LeftClick || inputEvent == InputEvent.Tap)
				{
					waiter = button.GetInvokedWaiter();
				}
			}
			else if (obj is ToggleButton)
			{
				var toggleButton = obj as ToggleButton;
				if (inputEvent == InputEvent.LeftClick || inputEvent == InputEvent.Tap)
				{
					waiter = toggleButton.GetToggledWaiter();
				}
			}
			else if (obj is ComboBox)
			{
				var comboBox = obj as ComboBox;
				if (comboBox.ExpandCollapseState == ExpandCollapseState.Collapsed &&
					(inputEvent == InputEvent.LeftClick || inputEvent == InputEvent.Tap))
				{
					waiter = comboBox.GetExpandedWaiter();
				}
			}
#if COLORPICKER_INCLUDED
			else if (obj is ColorSpectrum)
			{
				var colorSpectrum = obj as ColorSpectrum;
				waiter = colorSpectrum.GetColorChangedWaiter();
			}
			else if (obj is ColorPickerSlider)
			{
				var colorPickerSlider = obj as ColorPickerSlider;
				waiter = colorPickerSlider.GetColorChangedWaiter();
			}
#endif

			return waiter;
		}

		// Tests should scroll to their bottom most element to ensure the entire
		// test UI is onscreen otherwise the fake input injection will fail.
		//
		// Potentially has side effects depending on if a scroll item implementation can be retrieved
		public static async Task ScrollToElement(UIObject uiObject)
		{
			Log.Comment("ScrollToElement: " + uiObject.ToString());
			bool successfullyScrolled = false;
			ScrollItemImplementation scrollItem = new ScrollItemImplementation(uiObject);
			if (scrollItem.IsAvailable)
			{
				Log.Comment("Using ScrollItemImplementation to scroll");
				scrollItem.ScrollIntoView();
				successfullyScrolled = true;
			}

			if (!successfullyScrolled)
			{
				Log.Comment("Using ScrollItem UIA pattern to scroll");
				// Uno: in-process equivalent of the UIA client calls on AutomationElement.FocusedElement.
				var previouslyFocusedElement = UIObject.Focused;
				uiObject.SetFocus();
				await Wait.ForIdle();
				var newFocusedElement = UIObject.Focused;
				if (newFocusedElement != null)
				{
					var scrollItemPattern = new ScrollItemImplementation(newFocusedElement);
					if (scrollItemPattern.IsAvailable)
					{
						scrollItemPattern.ScrollIntoView();
					}
				}

				if (previouslyFocusedElement != null)
				{
					previouslyFocusedElement.SetFocus();
				}
			}
			await Wait.ForIdle();
		}
	}
}
