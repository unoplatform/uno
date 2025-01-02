/*
 * HorizontalListView.cs a derivation of Paul Soucy's HorizontalListView.java,
 * with additional features, including Snap mode and C# event handlers.
 *  
 * Copyright (c) 2012 Tomasz Cielecki (tomasz@ostebaronen.dk)
 * Copyright (c) 2011 Paul Soucy (paul@dev-smart.com)
 *  
 * The MIT License 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using Android.Database;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Content;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;

namespace Uno.UI.Controls
{
	[Preserve]
	public delegate void ScreenChangedEventHandler(object sender, ScreenChangedEventArgs e);

	[Preserve(AllMembers = true)]
	public class ScreenChangedEventArgs : EventArgs
	{
		public int CurrentScreen { get; set; }
		public int CurrentX { get; set; }
	}

	[Preserve(AllMembers = true)]
	public partial class NativeHorizontalListView : AdapterView<BaseAdapter>
	{
		#region Fields

		private const int SnapVelocityDipPerSecond = 600;
		private const int VelocityUnitPixelsPerSecond = 1000;
		private const int FractionOfScreenWidthForSwipe = 4;
		private const int AnimationScreenDurationMillis = 500;
		private VelocityTracker _velocityTracker;
		private int _maximumVelocity;
		private int _densityAdjustedSnapVelocity;
		private int _currentScreen;

		private int _leftViewIndex;
		private int _rightViewIndex;
		private int _displayOffset;
		private int _currentX;
		private int _nextX;
		private int _maxX;
		private GestureDetector _gestureDetector;
		private readonly Queue<View> _removedViewQueue = new Queue<View>();
		private bool _dataChanged;
		private DataSetObserver _dataSetObserver;
		private BaseAdapter _adapter;

		#endregion

		#region Ctor + Init

		public NativeHorizontalListView(IntPtr handle, JniHandleOwnership transfer)
			: base(handle, transfer)
		{
			InitView();
			_dataSetObserver = new DataObserver(this);
		}

		public NativeHorizontalListView(Android.Content.Context context) : this(context, null) { }

		public NativeHorizontalListView(Android.Content.Context context, IAttributeSet attrs) : this(context, attrs, 0) { }

		public NativeHorizontalListView(Android.Content.Context context, IAttributeSet attrs, int defStyle)
			: base(context, attrs, defStyle)
		{
			InitView();
			_dataSetObserver = new DataObserver(this);
		}

		private void InitView()
		{
			_leftViewIndex = -1;
			_rightViewIndex = 0;
			_displayOffset = 0;
			_currentX = 0;
			_nextX = 0;
			_maxX = int.MaxValue;
			Scroller = new Scroller(Context);
			var listener = new GestureListener(this);
			_gestureDetector = new GestureDetector(Context, listener);

			var configuration = ViewConfiguration.Get(Context);
			_maximumVelocity = configuration.ScaledMaximumFlingVelocity;

			var density = Context.Resources.DisplayMetrics.Density;
			_densityAdjustedSnapVelocity = (int)(density * SnapVelocityDipPerSecond);
		}

		#endregion

		#region Properties

		#region EventHandlers

		public event ScreenChangedEventHandler ScreenChanged;
		private int _heightMeasureSpec;
		private int _widthMeasureSpec;
		private int _paddingTop;
		private GravityFlags _alignMode = GravityFlags.Center;
		private int _height;

		protected virtual void OnScreenChanged(ScreenChangedEventArgs e)
		{
			if (ScreenChanged != null)
				ScreenChanged(this, e);
		}

		#endregion

		public override View SelectedView
		{
			get { return GetChildAt(CurrentScreen); }
		}

		public override BaseAdapter Adapter
		{
			get { return _adapter; }
			set
			{
				if (null != Adapter)
					Adapter.UnregisterDataSetObserver(_dataSetObserver);

				_adapter = value;
				_adapter.RegisterDataSetObserver(_dataSetObserver);
				Reset();
			}
		}

		public Scroller Scroller { get; private set; }

		/// <summary>
		/// Returns the CurrentScreen (Snap mode only).
		/// </summary>
		public int CurrentScreen
		{
			get { return _currentScreen; }
			private set
			{
				_currentScreen = value;
				OnScreenChanged(new ScreenChangedEventArgs
				{
					CurrentScreen = _currentScreen,
					CurrentX = _currentX
				});
			}
		}

		/// <summary>
		/// Returns the current leftmost X coordinate.
		/// </summary>
		public int CurrentX
		{
			get { return _currentX; }
			private set { _currentX = value; }
		}

		public bool Snap { get; set; }

		#endregion

		private void Reset()
		{
			lock (this)
			{
				InitView();
				RecycleAllItems();
				RequestLayout();
			}
		}

		private void AddAndMeasureChild(View child, int viewPos)
		{
			var childParams = child.LayoutParameters ??
							  new LayoutParams(LayoutParams.WrapContent, LayoutParams.MatchParent);

			AddViewInLayout(child, viewPos, childParams, false);

			ForceChildLayout(child, childParams);
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayout(changed, left, top, right, bottom);

			if (null == Adapter)
				return;

			_paddingTop = PaddingTop;
			_height = bottom - top;
			LayoutChildren();

			RefreshPosition();
		}

		private void RefreshPosition()
		{
			if (_dataChanged)
			{
				var oldCurrentX = CurrentX;
				InitView();
				RecycleAllItems();
				_nextX = oldCurrentX;
				_dataChanged = false;
			}

			if (Scroller.ComputeScrollOffset())
				_nextX = Scroller.CurrX;

			if (_nextX <= 0)
			{
				_nextX = 0;
				Scroller.ForceFinished(true);
			}

			if (_nextX >= _maxX)
			{
				_nextX = _maxX;
				Scroller.ForceFinished(true);
			}

			var dx = CurrentX - _nextX;
			RemoveNonVisibleItems(dx);
			FillList(dx);
			PositionItems(dx);

			CurrentX = _nextX;

			if (!Scroller.IsFinished)
				Post(RefreshPosition);
		}

		private void RecycleAllItems()
		{
			var child = GetChildAt(0);
			while (null != child)
			{
				_removedViewQueue.Enqueue(child);
				RemoveViewInLayout(child);
				child = GetChildAt(0);
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			_heightMeasureSpec = heightMeasureSpec;
			_widthMeasureSpec = widthMeasureSpec;
		}

		public void ForceChildLayout(View child, LayoutParams layoutParams)
		{
			int childHeightSpec = ViewGroup.GetChildMeasureSpec(_heightMeasureSpec, PaddingTop + PaddingBottom, layoutParams.Height);
			int childWidthSpec = ViewGroup.GetChildMeasureSpec(_widthMeasureSpec, PaddingLeft + PaddingRight, layoutParams.Width);

			child.Measure(childWidthSpec, childHeightSpec);
		}

		public void LayoutChildren()
		{
			int left, right;

			for (int i = 0; i < ChildCount; i++)
			{
				View child = GetChildAt(i);

				ForceChildLayout(child, child.LayoutParameters);

				left = child.Left;
				right = child.Right;

				int childHeight = child.Height;

				LayoutChild(child, left, right, childHeight);
			}
		}

		protected void LayoutChild(View child, int left, int right, int childHeight)
		{
			int top = _paddingTop;
			if (_alignMode == GravityFlags.Bottom)
			{
				top = top + (_height - childHeight);
			}
			else if (_alignMode == GravityFlags.Center)
			{
				top = top + (_height - childHeight) / 2;
			}

			if (child is Windows.UI.Xaml.UIElement elt)
			{
				elt.LayoutSlotWithMarginsAndAlignments = new Windows.Foundation.Rect(left, top, right, top + childHeight).PhysicalToLogicalPixels();
			}
			child.Layout(left, top, right, top + childHeight);
		}

		public override void SetSelection(int position)
		{
			var maxItems = Width / GetChildAt(0).MeasuredWidth;
			int itemWidth = GetChildAt(0).SelectOrDefault(v => v.Width);

			CurrentScreen = (position / maxItems) + 1;
			SnapToDestination();
		}

		public void SetSelection(int position, int duration)
		{
			CurrentScreen = position;
			SnapToDestination(duration);
		}

		protected void ScrollIntoViewInner(int position)
		{
			//Note: this assumes all children have the same width
			int itemWidth = GetChildAt(0).SelectOrDefault(v => v.Width);
			var rightmostValidScrollOffset = (position * itemWidth) + PaddingLeft;
			var leftmostValidScrollOffset = rightmostValidScrollOffset + itemWidth - Width;

			int targetX;
			if (CurrentX >= leftmostValidScrollOffset && CurrentX <= rightmostValidScrollOffset)
			{
				//Item is already in view, no need to scroll
				return;
			}

			if (CurrentX < leftmostValidScrollOffset)
			{
				//Item is offscreen to the left
				targetX = leftmostValidScrollOffset + 1;
			}
			else
			{
				//Item is offscreen to the right
				targetX = rightmostValidScrollOffset - 1;
			}

			_nextX = targetX;
			RefreshPosition();
		}

		private void FillList(int dx)
		{
			var edge = _displayOffset; //TODO: https://github.com/dinocore1/DevsmartLib-Android/issues/24
			var child = GetChildAt(ChildCount - 1);
			if (null != child)
				edge = child.Right;
			FillListRight(edge, dx);

			edge = 0;
			child = GetChildAt(0);
			if (null != child)
				edge = child.Left;
			FillListLeft(edge, dx);
		}

		private void FillListRight(int rightEdge, int dx)
		{
			while (rightEdge + dx < Width && _rightViewIndex < Adapter.Count)
			{
				View view = null;
				if (_removedViewQueue.Count > 0)
				{
					view = _removedViewQueue.Dequeue();
				}

				var child = Adapter.GetView(_rightViewIndex, view, this);
				AddAndMeasureChild(child, -1);
				rightEdge += child.MeasuredWidth;

				if (_rightViewIndex == Adapter.Count - 1)
				{
					var paddingLeft = PaddingLeft;
					_maxX = CurrentX + (rightEdge - paddingLeft) - (Width - paddingLeft - PaddingRight);
				}

				if (_maxX < 0)
					_maxX = 0;

				_rightViewIndex++;
			}
		}

		private void FillListLeft(int leftEdge, int dx)
		{
			while (leftEdge + dx > 0 && _leftViewIndex >= 0)
			{
				View view = null;
				if (_removedViewQueue.Count > 0)
				{
					view = _removedViewQueue.Dequeue();
				}

				var child = Adapter.GetView(_leftViewIndex, view, this);
				AddAndMeasureChild(child, 0);
				leftEdge -= child.MeasuredWidth;
				_leftViewIndex--;
				_displayOffset -= child.MeasuredWidth;
			}
		}

		private void RemoveNonVisibleItems(int dx)
		{
			var child = GetChildAt(0);
			while (null != child && child.Right + dx <= 0)
			{
				_displayOffset += child.MeasuredWidth;
				_removedViewQueue.Enqueue(child);
				RemoveViewInLayout(child);
				_leftViewIndex++;
				child = GetChildAt(0);
			}

			child = GetChildAt(ChildCount - 1);
			while (null != child && child.Left + dx >= Width)
			{
				_removedViewQueue.Enqueue(child);
				RemoveViewInLayout(child);
				_rightViewIndex--;
				child = GetChildAt(ChildCount - 1);
			}
		}

		private void PositionItems(int dx)
		{
			if (ChildCount < 1)
			{
				return;
			}

			_displayOffset += dx;
			var leftOffset = _displayOffset;

			for (var i = 0; i < ChildCount; i++)
			{
				var child = GetChildAt(i);

				var top = PaddingTop;
				var left = leftOffset + PaddingLeft;
				var right = left + child.MeasuredWidth;
				var bottom = top + child.MeasuredHeight;

				if (child is Windows.UI.Xaml.UIElement elt)
				{
					elt.LayoutSlotWithMarginsAndAlignments = new Windows.Foundation.Rect(left, top, right, bottom).PhysicalToLogicalPixels();
				}
				child.Layout(left, top, right, bottom);
				leftOffset += child.MeasuredWidth + child.PaddingRight;
			}
		}

		public override bool DispatchTouchEvent(MotionEvent e)
		{
			var handled = base.DispatchTouchEvent(e) | _gestureDetector.OnTouchEvent(e);
			return Snap ? base.DispatchTouchEvent(e) : handled;
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			if (Snap) // Oh snap!
			{
				if (_velocityTracker == null)
					_velocityTracker = VelocityTracker.Obtain();
				_velocityTracker.AddMovement(e);

				if (e.Action == MotionEventActions.Cancel || e.Action == MotionEventActions.Up)
				{
					var velocityTracker = _velocityTracker;
					velocityTracker.ComputeCurrentVelocity(VelocityUnitPixelsPerSecond, _maximumVelocity);
					var velocityX = (int)velocityTracker.XVelocity;

					if (velocityX > _densityAdjustedSnapVelocity && CurrentScreen > 0)
						SnapToScreen(CurrentScreen - 1);
					else if (velocityX < -_densityAdjustedSnapVelocity && CurrentScreen < Adapter.Count - 1)
						SnapToScreen(CurrentScreen + 1);
					else
						SnapToDestination();

					if (null != _velocityTracker)
					{
						_velocityTracker.Recycle();
						_velocityTracker = null;
					}

					return true;
				}
			}
			return base.OnTouchEvent(e);
		}

		public void ScrollTo(int x)
		{
			Scroller.StartScroll(_nextX, 0, x - _nextX, 0);
			RefreshPosition();
		}

		private void SnapToScreen(int whichScreen, int duration = -1)
		{
			CurrentScreen = Math.Max(0, Math.Min(whichScreen, Adapter.Count - 1));
			var nextX = CurrentScreen * Width;
			var delta = nextX - _nextX;

			if (duration < 0)
			{
				Scroller.StartScroll(_nextX, 0, delta, 0, (int)(Math.Abs(delta) / (float)Width * AnimationScreenDurationMillis));
			}
			else
			{
				Scroller.StartScroll(_nextX, 0, delta, 0, duration);
			}

			RefreshPosition();
		}

		private void SnapToDestination(int duration = -1)
		{
			var whichScreen = CurrentScreen;
			var deltaX = _nextX - (Width * CurrentScreen);

			if ((deltaX < 0) && CurrentScreen != 0 && (Width / FractionOfScreenWidthForSwipe < -deltaX))
				whichScreen--;
			else if ((deltaX > 0) && (CurrentScreen + 1 != Adapter.Count) && (Width / FractionOfScreenWidthForSwipe < deltaX))
				whichScreen++;

			SnapToScreen(whichScreen, duration);
		}

		protected bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
		{
			lock (this)
			{
				Scroller.Fling(_nextX, 0, (int)-velocityX, 0, 0, _maxX, 0, 0);
			}

			RefreshPosition();

			return true;
		}

		protected bool OnDown(MotionEvent e)
		{
			Scroller.ForceFinished(true);
			return true;
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage(
			"Microsoft.Usage",
			"CA2215:DisposeMethodsShouldCallBaseClassDispose",
			Justification = "Wanted behavior. base.Dispose must be called after the private fields otherwise we can't dispose them")]
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					if (null != _gestureDetector)
					{
						_gestureDetector.Dispose();
						_gestureDetector = null;
					}
					if (null != Scroller)
					{
						Scroller.Dispose();
						Scroller = null;
					}
					if (null != _dataSetObserver)
					{
						_dataSetObserver.Dispose();
						_dataSetObserver = null;
					}
					if (null != _velocityTracker)
					{
						_velocityTracker.Recycle();
						_velocityTracker = null;
					}
				}

				// Needs to be last, otherwise we can't access the other fields to dispose them.
				base.Dispose(disposing);
			}
			catch (Exception e)
			{
				this.Log().ErrorFormat("Failed to dispose view", e);
			}
		}

		#region Internal classes
		[Preserve(AllMembers = true)]
		private class DataObserver : DataSetObserver
		{
			private readonly NativeHorizontalListView _horizontalListView;

			public DataObserver(NativeHorizontalListView horizontalListView)
			{
				_horizontalListView = horizontalListView;
			}

			public override void OnChanged()
			{
				_horizontalListView.Reset();
			}

			public override void OnInvalidated()
			{
				_horizontalListView.Reset();
			}
		}

		[Preserve(AllMembers = true)]
		private class GestureListener : GestureDetector.SimpleOnGestureListener
		{
			private readonly NativeHorizontalListView _horizontalListView;

			public GestureListener(NativeHorizontalListView horizontalListView)
			{
				_horizontalListView = horizontalListView;
			}

			public override bool OnDown(MotionEvent e)
			{
				return _horizontalListView.OnDown(e);
			}

			public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
			{
				return _horizontalListView.OnFling(e1, e2, velocityX, velocityY);
			}

			public override bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
			{
				_horizontalListView.Parent?.RequestDisallowInterceptTouchEvent(true);
				_horizontalListView._nextX += (int)distanceX;
				_horizontalListView.RefreshPosition();

				return true;
			}

			public override bool OnSingleTapConfirmed(MotionEvent e)
			{
				for (var i = 0; i < _horizontalListView.ChildCount; i++)
				{
					var child = _horizontalListView.GetChildAt(i);

					if (!IsEventWithinView(e, child)) continue;
					if (null != _horizontalListView.OnItemClickListener)
						_horizontalListView.OnItemClickListener.OnItemClick(_horizontalListView, child, _horizontalListView._leftViewIndex + 1 + i,
																			_horizontalListView.Adapter.GetItemId(_horizontalListView._leftViewIndex + 1 + i));
					if (null != _horizontalListView.OnItemSelectedListener)
						_horizontalListView.OnItemSelectedListener.OnItemSelected(_horizontalListView, child, _horizontalListView._leftViewIndex + 1 + i,
																				  _horizontalListView.Adapter.GetItemId(_horizontalListView._leftViewIndex + 1 + i));
					break;
				}

				return true;
			}

			public override void OnLongPress(MotionEvent e)
			{
				for (var i = 0; i < _horizontalListView.ChildCount; i++)
				{
					var child = _horizontalListView.GetChildAt(i);

					if (!IsEventWithinView(e, child)) continue;
					if (null != _horizontalListView.OnItemLongClickListener)
						_horizontalListView.OnItemLongClickListener.OnItemLongClick(_horizontalListView, child, _horizontalListView._leftViewIndex + 1 + i,
																					_horizontalListView.Adapter.GetItemId(_horizontalListView._leftViewIndex + 1 + i));
					break;
				}
			}

			private static bool IsEventWithinView(MotionEvent e, View child)
			{
				var viewRect = new Rect();
				var childPosition = new int[2];
				child.GetLocationOnScreen(childPosition);
				var left = childPosition[0];
				var right = left + child.Width;
				var top = childPosition[1];
				var bottom = top + child.Height;
				viewRect.Set(left, top, right, bottom);
				return viewRect.Contains((int)e.RawX, (int)e.RawY);
			}
		}
		#endregion
	}
}
