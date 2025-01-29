using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

partial class Control
{
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
