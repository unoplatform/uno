using Android.Support.V4.Widget;
using System;
using System.Collections.Generic;
using System.Text;
using Android.Runtime;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Graphics;
using Android.Widget;
using UIElement = Android.Views.View;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Uno.UI;
using Uno.Extensions;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Controls
{
	public partial class BindableDrawerLayout : DrawerLayout, DrawerLayout.IDrawerListener, DependencyObject
	{
		private Border _rightPane;
		private Border _leftPane;

		#region Constructors

		public BindableDrawerLayout() : base(ContextHelper.Current)
		{
			Initialize();
		}

		public BindableDrawerLayout(Android.Content.Context context) : base(context)
		{
			Initialize();
		}

		public BindableDrawerLayout(Android.Content.Context context, IAttributeSet attrs) : base(context, attrs)
		{
			Initialize();
		}

		public BindableDrawerLayout(Android.Content.Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			Initialize();
		}

		protected BindableDrawerLayout(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			Initialize();
		}

		#endregion

		private void Initialize()
		{
			InitializeBinder();

#pragma warning disable 618
			SetDrawerListener(this);
#pragma warning restore 618

			LayoutParameters = new LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent);

			_leftPane = new Pane(SplitViewPanePlacement.Left);
			_rightPane = new Pane(SplitViewPanePlacement.Right);
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			// Ensure the stretching of Content is taken into account
			var element = Content as FrameworkElement;
			if (element != null)
			{
				element.StretchAffectsMeasure = true;
			}

			// Drawer layout must be measured with MeasureSpecMode.Exactly otherwise an exception is thrown
			base.OnMeasure(
				ViewHelper.MakeMeasureSpec(
					ViewHelper.MeasureSpecGetSize(widthMeasureSpec),
					MeasureSpecMode.Exactly
				),
				ViewHelper.MakeMeasureSpec(
					ViewHelper.MeasureSpecGetSize(heightMeasureSpec),
					MeasureSpecMode.Exactly
				)
			);
		}

		#region IsEnabled DependencyProperty

		public bool IsEnabled
		{
			get { return (bool)this.GetValue(IsEnabledProperty); }
			set { this.SetValue(IsEnabledProperty, value); }
		}

		public static DependencyProperty IsEnabledProperty =
			DependencyProperty.Register(
				"IsEnabled",
				typeof(bool),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(bool)true,
					(s, e) => ((BindableDrawerLayout)s)?.OnIsEnabledChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);

		private void OnIsEnabledChanged(bool oldIsEnabled, bool newIsEnabled)
		{
			var lockMode = newIsEnabled
				? LockModeUnlocked
				: LockModeLockedClosed;
			if (!newIsEnabled)
			{
				SetDrawerLockMode(lockMode);
			}
			else //When entire pane is enabled restore based on left and right enabled properties
			{
				OnLeftPaneIsEnabledChanged(false, IsLeftPaneEnabled);
				OnRightPaneIsEnabledChanged(false, IsRightPaneEnabled);
			}
		}
		#endregion

		#region IsLeftPaneEnabled DependencyProperty
		public bool IsLeftPaneEnabled
		{
			get { return (bool)this.GetValue(IsLeftPaneEnabledProperty); }
			set { this.SetValue(IsLeftPaneEnabledProperty, value); }
		}

		public static readonly DependencyProperty IsLeftPaneEnabledProperty =
			DependencyProperty.Register(
				"IsLeftPaneEnabled",
				typeof(bool),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(bool)true,
					(s, e) => ((BindableDrawerLayout)s)?.OnLeftPaneIsEnabledChanged((bool)e.OldValue, (bool)e.NewValue)
				));

		private void OnLeftPaneIsEnabledChanged(bool oldIsEnabled, bool newIsEnabled)
		{
			var lockMode = newIsEnabled
				? LockModeUnlocked
				: LockModeLockedClosed;

			SetDrawerLockMode(lockMode, _leftPane);
		}

		#endregion

		#region IsRightPaneEnabled DependencyProperty
		public bool IsRightPaneEnabled
		{
			get { return (bool)this.GetValue(IsRightPaneEnabledProperty); }
			set { this.SetValue(IsRightPaneEnabledProperty, value); }
		}

		public static readonly DependencyProperty IsRightPaneEnabledProperty =
			DependencyProperty.Register(
				"IsRightPaneEnabled",
				typeof(bool),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(bool)true,
					(s, e) => ((BindableDrawerLayout)s)?.OnRightPaneIsEnabledChanged((bool)e.OldValue, (bool)e.NewValue)
				));

		private void OnRightPaneIsEnabledChanged(bool oldIsEnabled, bool newIsEnabled)
		{
			var lockMode = newIsEnabled
				? LockModeUnlocked
				: LockModeLockedClosed;

			SetDrawerLockMode(lockMode, _rightPane);
		}

		#endregion

		#region Content DependencyProperty

		public UIElement Content
		{
			get { return (UIElement)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		public static DependencyProperty ContentProperty =
			DependencyProperty.Register(
				"Content",
				typeof(UIElement),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(UIElement)null,
					(s, e) => ((BindableDrawerLayout)s)?.OnContentChanged((UIElement)e.OldValue, (UIElement)e.NewValue)
				)
			);

		protected void OnContentChanged(UIElement oldValue, UIElement newValue)
		{
			if (oldValue != null)
			{
				this.RemoveView(oldValue);
			}

			if (newValue != null)
			{
				var layoutParameters = new LayoutParams(
					ViewGroup.LayoutParams.MatchParent,
					ViewGroup.LayoutParams.MatchParent);

				AddView(newValue, 0, layoutParameters);
			}
		}

		#endregion

		#region RightPane DependencyProperty

		public UIElement RightPane
		{
			get { return (UIElement)this.GetValue(RightPaneProperty); }
			set { this.SetValue(RightPaneProperty, value); }
		}

		public static DependencyProperty RightPaneProperty =
			DependencyProperty.Register(
				"RightPane",
				typeof(UIElement),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(UIElement)null,
					(s, e) => ((BindableDrawerLayout)s)?.OnRightPaneChanged((UIElement)e.OldValue, (UIElement)e.NewValue)
				)
			);

		protected void OnRightPaneChanged(UIElement oldValue, UIElement newValue)
		{
			if (newValue != null && _rightPane.Parent == null)
			{
				AddView(_rightPane);
			}
			else if (newValue == null && _rightPane.Parent == this)
			{
				RemoveView(_rightPane);
			}

			_rightPane.Child = newValue;
		}

		#endregion

		#region RightPaneOpenLength DependencyProperty

		public double RightPaneOpenLength
		{
			get { return (double)this.GetValue(RightPaneOpenLengthProperty); }
			set { this.SetValue(RightPaneOpenLengthProperty, value); }
		}

		public static DependencyProperty RightPaneOpenLengthProperty =
			DependencyProperty.Register(
				"RightPaneOpenLength",
				typeof(double),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(double)0,
					(s, e) => ((BindableDrawerLayout)s)?.OnRightPaneOpenLengthChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected void OnRightPaneOpenLengthChanged(double oldValue, double newValue)
		{
			_rightPane.Width = (float)newValue;
		}

		#endregion

		#region IsRightPaneOpen DependencyProperty

		public bool IsRightPaneOpen
		{
			get { return (bool)this.GetValue(IsRightPaneOpenProperty); }
			set { this.SetValue(IsRightPaneOpenProperty, value); }
		}

		public static DependencyProperty IsRightPaneOpenProperty =
			DependencyProperty.Register(
				"IsRightPaneOpen",
				typeof(bool),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(bool)false,
					(s, e) => ((BindableDrawerLayout)s)?.OnIsRightPaneOpenChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);

		protected void OnIsRightPaneOpenChanged(bool oldValue, bool newValue)
		{
			if (newValue)
			{
				OpenDrawer(_rightPane);
			}
			else
			{
				CloseDrawer(_rightPane);
			}
		}

		#endregion

		#region RightPaneBackground DependencyProperty

		public Brush RightPaneBackground
		{
			get { return (Brush)this.GetValue(RightPaneBackgroundProperty); }
			set { this.SetValue(RightPaneBackgroundProperty, value); }
		}

		public static DependencyProperty RightPaneBackgroundProperty =
			DependencyProperty.Register(
				"RightPaneBackground",
				typeof(Brush),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(Brush)null,
					(s, e) => ((BindableDrawerLayout)s)?.OnRightPaneBackgroundChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		protected void OnRightPaneBackgroundChanged(Brush oldValue, Brush newValue)
		{
			if (_rightPane != null)
			{
				_rightPane.Background = newValue;
			}
		}

		#endregion

		#region LeftPane DependencyProperty

		public UIElement LeftPane
		{
			get { return (UIElement)this.GetValue(LeftPaneProperty); }
			set { this.SetValue(LeftPaneProperty, value); }
		}

		public static DependencyProperty LeftPaneProperty =
			DependencyProperty.Register(
				"LeftPane",
				typeof(UIElement),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(UIElement)null,
					(s, e) => ((BindableDrawerLayout)s)?.OnLeftPaneChanged((UIElement)e.OldValue, (UIElement)e.NewValue)
				)
			);

		protected void OnLeftPaneChanged(UIElement oldValue, UIElement newValue)
		{
			if (newValue != null && _leftPane.Parent == null)
			{
				AddView(_leftPane);
			}
			else if (newValue == null && _leftPane.Parent == this)
			{
				RemoveView(_leftPane);
			}

			_leftPane.Child = newValue;
		}

		#endregion

		#region LeftPaneOpenLength DependencyProperty

		public double LeftPaneOpenLength
		{
			get { return (double)this.GetValue(LeftPaneOpenLengthProperty); }
			set { this.SetValue(LeftPaneOpenLengthProperty, value); }
		}

		public static DependencyProperty LeftPaneOpenLengthProperty =
			DependencyProperty.Register(
				"LeftPaneOpenLength",
				typeof(double),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(double)0,
					(s, e) => ((BindableDrawerLayout)s)?.OnLeftPaneOpenLengthChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected void OnLeftPaneOpenLengthChanged(double oldValue, double newValue)
		{
			_leftPane.Width = (float)newValue;
		}

		#endregion

		#region IsLeftPaneOpen DependencyProperty

		public bool IsLeftPaneOpen
		{
			get { return (bool)this.GetValue(IsLeftPaneOpenProperty); }
			set { this.SetValue(IsLeftPaneOpenProperty, value); }
		}

		public static DependencyProperty IsLeftPaneOpenProperty =
			DependencyProperty.Register(
				"IsLeftPaneOpen",
				typeof(bool),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(bool)false,
					(s, e) => ((BindableDrawerLayout)s)?.OnIsLeftPaneOpenChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);

		protected void OnIsLeftPaneOpenChanged(bool oldValue, bool newValue)
		{
			if (newValue)
			{
				OpenDrawer(_leftPane);
			}
			else
			{
				CloseDrawer(_leftPane);
			}
		}

		#endregion

		#region LeftPaneBackground DependencyProperty

		public Brush LeftPaneBackground
		{
			get { return (Brush)this.GetValue(LeftPaneBackgroundProperty); }
			set { this.SetValue(LeftPaneBackgroundProperty, value); }
		}

		public static DependencyProperty LeftPaneBackgroundProperty =
			DependencyProperty.Register(
				"LeftPaneBackground",
				typeof(Brush),
				typeof(BindableDrawerLayout),
				new PropertyMetadata(
					(Brush)null,
					(s, e) => ((BindableDrawerLayout)s)?.OnLeftPaneBackgroundChanged((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		protected void OnLeftPaneBackgroundChanged(Brush oldValue, Brush newValue)
		{
			if (_leftPane != null)
			{
				_leftPane.Background = newValue;
			}
		}

		#endregion

		#region IDrawerListener

		public void OnDrawerClosed(View drawerView)
		{
		}

		public void OnDrawerOpened(View drawerView)
		{
		}

		public void OnDrawerSlide(View drawerView, float slideOffset)
		{
		}

		public void OnDrawerStateChanged(int newState)
		{
			if (newState == StateIdle)
			{
				IsLeftPaneOpen = IsDrawerOpen(_leftPane);
				IsRightPaneOpen = IsDrawerOpen(_rightPane);
			}
		}

		#endregion
	}

	internal partial class Pane : Border
	{
		public Pane(SplitViewPanePlacement placement)
		{
			StretchAffectsMeasure = true;
			VerticalAlignment = VerticalAlignment.Stretch;

			switch (placement)
			{
				case SplitViewPanePlacement.Left:
					HorizontalAlignment = HorizontalAlignment.Left;
					LayoutParameters = new DrawerLayout.LayoutParams(100, LayoutParams.MatchParent)
					{
						Gravity = (int)GravityFlags.Start
					};
					break;
				case SplitViewPanePlacement.Right:
					HorizontalAlignment = HorizontalAlignment.Right;
					LayoutParameters = new DrawerLayout.LayoutParams(100, LayoutParams.MatchParent)
					{
						Gravity = (int)GravityFlags.End
					};
					break;
			}
		}

		/// <inheritdoc />
		protected override bool NativeHitCheck()
			=> true; // Ensure clicks don't go through panes
	}


}
