using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Controls
{
	partial class SplitView
	{
		/*

			About the PatchInvalidFinalState:
				On iOS for some strange reasons, the burger menu may not complete its open/close animation,
				but instead either does not appear at all (when opened), either stays partially visible (when closed).
				This seems to be related to the fact that  animations of SplitView are altering the Grid.Column and the Visibility 
				of some controls.
				Usually it will become fully visible/hidden when a rendering cycle occurs, but this causes
				a delay for the users which seems awkward.
				The patch here only ensure to invalidate the view to force a layouting cycle.
			
		*/

		private VisualStateGroup _patchDisplayModeStatesGroup;

		private void PatchInvalidFinalState(string targetStateName)
		{
			// Get the DisplayModeStates visual state group from the root element of the template
			if (_patchDisplayModeStatesGroup == null)
			{
				var rootElement = TemplatedRoot as FrameworkElement;
				if (rootElement == null && VisualTreeHelper.GetChildrenCount(this) > 0)
				{
					rootElement = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
				}

				if (rootElement != null)
				{
					_patchDisplayModeStatesGroup = VisualStateManager
						.GetVisualStateGroups(rootElement)
						.FirstOrDefault(g => g.Name == "DisplayModeStates");
				}
			}

			var current = _patchDisplayModeStatesGroup?.CurrentState;
			if (current == null)
			{
				return;
			}

			// As completion are not properly handled by visual states/Storyboard, we have to dig to find the transition
			// and subscribe to its longest Storyboard's timeline
			var timeline = _patchDisplayModeStatesGroup
				.Transitions
				.FirstOrDefault(t => t.From == current.Name && t.To == targetStateName)
				?.Storyboard?.Children
				.Where(t => t.Duration.HasTimeSpan)
				.OrderBy(t => (t.BeginTime ?? TimeSpan.Zero) + t.Duration.TimeSpan)
				.LastOrDefault();

			if (timeline != null)
			{
				timeline.Completed += PatchInvalidFinalStateHandler;
			}
		}

		private void PatchInvalidFinalStateHandler(object sender, object o)
		{
			if (sender is Timeline timeline)
			{
				timeline.Completed -= PatchInvalidFinalStateHandler;
			}

			_ = Dispatcher.RunIdleAsync(_ => InvalidateMeasure());
		}
	}
}
