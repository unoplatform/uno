// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference controls/dev/AnimatedIcon/TestUI/AnimatedIconPage.xaml.cs

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Windows.UI;
using System.Windows.Input;

using AnimatedIcon = Microsoft.UI.Xaml.Controls.AnimatedIcon;
#if HAS_UNO
using Microsoft.UI.Private.Controls;
#endif
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.AnimatedIconTests
{
	[Sample("Icons", "MUX")]
	public sealed partial class AnimatedIconPage : Page
	{
		public AnimatedIconPage()
		{
			this.InitializeComponent();
			this.Loaded += AnimatedIconPage_Loaded;
#if HAS_UNO
			AnimatedIconTestHooks.LastAnimationSegmentChanged += AnimatedIconTestHooks_LastAnimationSegmentChanged;
			// TODO Uno specific: upstream never unsubscribes from this static event. SamplesApp keeps running after
			// the sample is closed, so the handler is dropped while unloaded.
			this.Unloaded += (_, _) => AnimatedIconTestHooks.LastAnimationSegmentChanged -= AnimatedIconTestHooks_LastAnimationSegmentChanged;
			this.Loaded += (_, _) =>
			{
				AnimatedIconTestHooks.LastAnimationSegmentChanged -= AnimatedIconTestHooks_LastAnimationSegmentChanged;
				AnimatedIconTestHooks.LastAnimationSegmentChanged += AnimatedIconTestHooks_LastAnimationSegmentChanged;
			};
#endif
		}

		private void AnimatedIconTestHooks_LastAnimationSegmentChanged(AnimatedIcon sender, object args)
		{
#if HAS_UNO
			var rootGrid = VisualTreeHelper.GetChild(LargeAnimatedCheckbox, 0);
			var grid = VisualTreeHelper.GetChild(rootGrid, 0);
			var CheckIcon = VisualTreeHelper.GetChild(grid, 1) as AnimatedIcon;

			if (sender == CheckIcon)
			{
				LastTransitionTextBlock.Text = AnimatedIconTestHooks.GetLastAnimationSegment(CheckIcon);
			}
#endif
		}

		private void AnimatedIconPage_Loaded(object sender, RoutedEventArgs e)
		{
#if HAS_UNO
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.DropDownIcon_Cut.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.Cut);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.DropDownIcon_Queue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.QueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.DropDownIcon_SpeedUpQueue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.DropDownIconNavView_Cut.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.Cut);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.DropDownIconNavView_Queue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.QueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.DropDownIconNavView_SpeedUpQueue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SideChevron_Cut.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.Cut);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SideChevron_Queue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.QueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SideChevron_SpeedUpQueue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.HamburgerIcon_Cut.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.Cut);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.HamburgerIcon_Queue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.QueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.HamburgerIcon_SpeedUpQueue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SettingsIcon_Cut.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.Cut);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SettingsIcon_Queue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.QueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SettingsIcon_SpeedUpQueue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.BackIcon_Cut.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.Cut);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.BackIcon_Queue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.QueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.BackIcon_SpeedUpQueue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SearchIcon_Cut.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.Cut);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SearchIcon_Queue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.QueueOne);
			AnimatedIconTestHooks.SetAnimationQueueBehavior(this.SearchIcon_SpeedUpQueue.GetAnimatedIcon(), AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne);
#endif
		}

		private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
#if HAS_UNO
			if (this.DropDownIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.DropDownIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.DropDownIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.DropDownIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIconNavView_Cut != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.DropDownIconNavView_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIconNavView_Queue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.DropDownIconNavView_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIconNavView_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.DropDownIconNavView_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SideChevron_Cut != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SideChevron_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SideChevron_Queue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SideChevron_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SideChevron_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SideChevron_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.HamburgerIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.HamburgerIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.HamburgerIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.HamburgerIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.HamburgerIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.HamburgerIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SettingsIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SettingsIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SettingsIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SettingsIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SettingsIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SettingsIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.BackIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.BackIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.BackIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.BackIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.BackIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.BackIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SearchIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SearchIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SearchIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SearchIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SearchIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetDurationMultiplier(this.SearchIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.AnimatedCheckbox != null)
			{
				var rootGrid = VisualTreeHelper.GetChild(AnimatedCheckbox, 0);
				var grid = VisualTreeHelper.GetChild(rootGrid, 0);
				var CheckIcon = VisualTreeHelper.GetChild(grid, 1) as AnimatedIcon;

				AnimatedIconTestHooks.SetDurationMultiplier(CheckIcon, (float)e.NewValue);
			}
			if (this.LargeAnimatedCheckbox != null)
			{
				var rootGrid = VisualTreeHelper.GetChild(LargeAnimatedCheckbox, 0);
				var grid = VisualTreeHelper.GetChild(rootGrid, 0);
				var CheckIcon = VisualTreeHelper.GetChild(grid, 1) as AnimatedIcon;

				AnimatedIconTestHooks.SetDurationMultiplier(CheckIcon, (float)e.NewValue);
			}
#endif
		}

		private void SpeedUpSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
#if HAS_UNO
			if (this.DropDownIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.DropDownIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.DropDownIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.DropDownIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIconNavView_Cut != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.DropDownIconNavView_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIconNavView_Queue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.DropDownIconNavView_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.DropDownIconNavView_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.DropDownIconNavView_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SideChevron_Cut != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SideChevron_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SideChevron_Queue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SideChevron_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SideChevron_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SideChevron_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.HamburgerIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.HamburgerIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.HamburgerIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.HamburgerIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.HamburgerIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.HamburgerIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SettingsIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SettingsIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SettingsIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SettingsIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SettingsIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SettingsIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.BackIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.BackIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.BackIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.BackIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.BackIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.BackIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SearchIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SearchIcon_Cut.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SearchIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SearchIcon_Queue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.SearchIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetSpeedUpMultiplier(this.SearchIcon_SpeedUpQueue.GetAnimatedIcon(), (float)e.NewValue);
			}
			if (this.AnimatedCheckbox != null)
			{
				var rootGrid = VisualTreeHelper.GetChild(AnimatedCheckbox, 0);
				var grid = VisualTreeHelper.GetChild(rootGrid, 0);
				var CheckIcon = VisualTreeHelper.GetChild(grid, 1) as AnimatedIcon;

				AnimatedIconTestHooks.SetSpeedUpMultiplier(CheckIcon, (float)e.NewValue);
			}
			if (this.LargeAnimatedCheckbox != null)
			{
				var rootGrid = VisualTreeHelper.GetChild(LargeAnimatedCheckbox, 0);
				var grid = VisualTreeHelper.GetChild(rootGrid, 0);
				var CheckIcon = VisualTreeHelper.GetChild(grid, 1) as AnimatedIcon;

				AnimatedIconTestHooks.SetSpeedUpMultiplier(CheckIcon, (float)e.NewValue);
			}
#endif
		}

		private void QueueLengthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
#if HAS_UNO
			if (this.DropDownIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.DropDownIcon_Cut.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.DropDownIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.DropDownIcon_Queue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.DropDownIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.DropDownIcon_SpeedUpQueue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.DropDownIconNavView_Cut != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.DropDownIconNavView_Cut.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.DropDownIconNavView_Queue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.DropDownIconNavView_Queue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.DropDownIconNavView_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.DropDownIconNavView_SpeedUpQueue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SideChevron_Cut != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SideChevron_Cut.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SideChevron_Queue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SideChevron_Queue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SideChevron_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SideChevron_SpeedUpQueue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.HamburgerIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.HamburgerIcon_Cut.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.HamburgerIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.HamburgerIcon_Queue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.HamburgerIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.HamburgerIcon_SpeedUpQueue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SettingsIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SettingsIcon_Cut.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SettingsIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SettingsIcon_Queue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SettingsIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SettingsIcon_SpeedUpQueue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.BackIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.BackIcon_Cut.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.BackIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.BackIcon_Queue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.BackIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.BackIcon_SpeedUpQueue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SearchIcon_Cut != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SearchIcon_Cut.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SearchIcon_Queue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SearchIcon_Queue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.SearchIcon_SpeedUpQueue != null)
			{
				AnimatedIconTestHooks.SetQueueLength(this.SearchIcon_SpeedUpQueue.GetAnimatedIcon(), (int)e.NewValue);
			}
			if (this.AnimatedCheckbox != null)
			{
				var rootGrid = VisualTreeHelper.GetChild(AnimatedCheckbox, 0);
				var grid = VisualTreeHelper.GetChild(rootGrid, 0);
				var CheckIcon = VisualTreeHelper.GetChild(grid, 1) as AnimatedIcon;

				AnimatedIconTestHooks.SetQueueLength(CheckIcon, (int)e.NewValue);
			}
			if (this.LargeAnimatedCheckbox != null)
			{
				var rootGrid = VisualTreeHelper.GetChild(LargeAnimatedCheckbox, 0);
				var grid = VisualTreeHelper.GetChild(rootGrid, 0);
				var CheckIcon = VisualTreeHelper.GetChild(grid, 1) as AnimatedIcon;

				AnimatedIconTestHooks.SetQueueLength(CheckIcon, (int)e.NewValue);
			}
#endif
		}
		public void IsLeftToRightChecked(object sender, RoutedEventArgs args)
		{
			this.FlowDirection = FlowDirection.LeftToRight;
		}

		public void IsLeftToRightUnchecked(object sender, RoutedEventArgs args)
		{
			this.FlowDirection = FlowDirection.RightToLeft;
		}

		private void ChangeFallbackGlyphButton_Click(object sender, RoutedEventArgs e)
		{
			boundFallback.FallbackGlyph = "\uE9AE";
		}
	}
}
