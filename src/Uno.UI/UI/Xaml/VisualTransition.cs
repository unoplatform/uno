using Uno.Extensions;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Data;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml
{
	[ContentProperty(Name = "Storyboard")]
	public partial class VisualTransition : DependencyObject
	{
		/// <summary>
		/// Lazy builder provided by the source generator. Invoking this will
		/// optionally fill <see cref="Storyboard"/>.
		/// </summary>
		internal Action LazyBuilder { get; set; }

		public VisualTransition()
		{
			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();
		}

		public string From { get; set; }

		public string To { get; set; }


		#region Storyboard DependencyProperty

		public Storyboard Storyboard
		{
			get
			{
				EnsureMaterialized();
				return (Storyboard)this.GetValue(StoryboardProperty);
			}
			set { this.SetValue(StoryboardProperty, value); }
		}

		private void EnsureMaterialized()
		{
			if (LazyBuilder != null)
			{
				var builder = LazyBuilder;
				LazyBuilder = null;
				builder.Invoke();

				if (Storyboard is DependencyObject storyboardProvider)
				{
					// Set the theme changed flag on so the update processes
					// the children.
					storyboardProvider.UpdateResourceBindings(ResourceUpdateReason.ThemeResource);
				}
			}
		}

		public static DependencyProperty StoryboardProperty { get; } =
			DependencyProperty.Register(
				"Storyboard",
				typeof(Storyboard),
				typeof(VisualTransition),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.LogicalChild
				)
			);

		#endregion

		/// <summary>
		/// Gets or sets the time that it takes to move from one state to another, for the animations VisualStateManager
		/// generates between the states.
		/// </summary>
		public Duration GeneratedDuration { get; set; } = new Duration(TimeSpan.Zero);

		/// <summary>
		/// Gets or sets the easing function applied to the generated animations.
		/// </summary>
		public EasingFunctionBase GeneratedEasingFunction { get; set; }

		// CVisualTransition::IsZeroDuration
		internal bool IsZeroDuration()
		{
			var visualTransitionZeroDuration = !GeneratedDuration.HasTimeSpan || GeneratedDuration.TimeSpan == TimeSpan.Zero;
			var storyboardNullOrZeroDuration = true;
			if (Storyboard is { } storyboard)
			{
				// We use the GetDuration method to ensure that we take into account offshifted
				// start times.
				storyboard.GetDuration(out _, out var rDuration);
				storyboardNullOrZeroDuration = rDuration == 0.0f;
			}
			return visualTransitionZeroDuration && storyboardNullOrZeroDuration;
		}
	}
}
