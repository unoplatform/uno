using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	internal interface ICommandBarLabeledElement
	{
		void SetDefaultLabelPosition(CommandBarDefaultLabelPosition defaultLabelPosition);
		bool GetHasBottomLabel();
	}
}
