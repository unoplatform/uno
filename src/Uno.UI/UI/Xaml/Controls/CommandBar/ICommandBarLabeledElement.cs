using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	internal interface ICommandBarLabeledElement
	{
		void SetDefaultLabelPosition(CommandBarDefaultLabelPosition defaultLabelPosition);
		bool GetHasBottomLabel();
	}
}
