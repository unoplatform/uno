using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

partial class Control
{
	// MUX Reference Control_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d
	internal bool DoesElementOverlapViewport(UIElement pCandidate)
	{
		double horizontalOffset = 0;
		double verticalOffset = 0;
		double width;
		double height;

		if (this is ScrollViewer spThisAsScrollViewer)
		{
			width = spThisAsScrollViewer.ViewportWidth;
			height = spThisAsScrollViewer.ViewportHeight;
			horizontalOffset = spThisAsScrollViewer.HorizontalOffset;
			verticalOffset = spThisAsScrollViewer.VerticalOffset;
		}
		else
		{
			height = ActualHeight;
			width = ActualWidth;
		}

		// check if the candidate is within the viewport
		var offsetRelativeToElement = pCandidate.TransformToVisual(this).TransformPoint(default);

		var spCandidateAsFE = (FrameworkElement)pCandidate;
		var candidateWidth = spCandidateAsFE.ActualWidth;
		var candidateHeight = spCandidateAsFE.ActualHeight;

		// candidate rectangle top left and bottom right points
		var candidateLeft = offsetRelativeToElement.X + horizontalOffset;
		var candidateTop = offsetRelativeToElement.Y + verticalOffset;
		var candidateRight = candidateLeft + candidateWidth;
		var candidateBottom = candidateTop + candidateHeight;

		// viewport top left and bottom right points
		var viewportRight = horizontalOffset + width;
		var viewportBottom = verticalOffset + height;

		// does the candidate overlaps the viewport
		return !(candidateLeft > viewportRight ||
			horizontalOffset > candidateRight ||
			candidateTop > viewportBottom ||
			verticalOffset > candidateBottom);
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == VisibilityProperty)
		{
			OnVisibilityChanged();
		}
		else if (args.Property == IsFocusEngagementEnabledProperty)
		{
			//Will disengage only if the Control is engaged
			RemoveFocusEngagement();
		}
		else if (args.Property == IsFocusEngagedProperty)
		{
			SetFocusEngagement();
		}
	}
}
