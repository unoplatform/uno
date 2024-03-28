using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Samples.Controls
{
	/// <summary>
	/// A control which wraps a Path, facilitating animation and allowing binding to the Foreground property (mapped to Path.Fill through styles).
	/// </summary>
	public partial class PathControl : Control
	{
		private const string LoadingStoryboard = "LoadingStoryboard";
		private const string StoppedStoryboard = "StoppedStoryboard";


		public PathControl()
		{
			DefaultStyleKey = typeof(PathControl);

			this.Loaded += OnLoaded;
			this.Unloaded += OnUnloaded;

			HorizontalContentAlignment = HorizontalAlignment.Center;
			VerticalContentAlignment = VerticalAlignment.Center;
		}

		public Stretch Stretch
		{
			get { return (Stretch)GetValue(StretchProperty); }
			set { SetValue(StretchProperty, value); }
		}
		public static DependencyProperty StretchProperty { get; } =
			DependencyProperty.Register("Stretch", typeof(Stretch), typeof(PathControl), new PropertyMetadata(default(Stretch)));

		public Geometry Data
		{
			get { return (Geometry)GetValue(DataProperty); }
			set { SetValue(DataProperty, value); }
		}

		public static DependencyProperty DataProperty { get; } =
			DependencyProperty.Register("Data", typeof(Geometry), typeof(PathControl), new PropertyMetadata(default(Geometry)));

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var control = this as PathControl;

			if (!AnimationStopped)
			{
				VisualStateManager.GoToState(control, LoadingStoryboard, true);
			}
		}

		// Reset animation as a workaround for bug #21369 on iOS
		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, StoppedStoryboard, true);
		}

		public bool AnimationStopped
		{
			get { return (bool)GetValue(AnimationStoppedProperty); }
			set { SetValue(AnimationStoppedProperty, value); }
		}

		public static DependencyProperty AnimationStoppedProperty { get; } =
			DependencyProperty.Register("AnimationStopped", typeof(bool), typeof(PathControl), new PropertyMetadata(false, OnAnimationStoppedChanged));

		private static void OnAnimationStoppedChanged(object d, DependencyPropertyChangedEventArgs e)
		{
			var control = d as PathControl;
			var stopAnimation = (bool)e.NewValue;
			if (control != null)
			{
				if (stopAnimation)
				{
					VisualStateManager.GoToState(control, StoppedStoryboard, true);
				}
				else
				{
					VisualStateManager.GoToState(control, LoadingStoryboard, true);
				}
			}
		}
	}
}
