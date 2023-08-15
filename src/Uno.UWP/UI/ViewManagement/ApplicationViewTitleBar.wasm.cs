using System;
using System.Collections.Generic;
using System.Text;
using Uno.Foundation;

namespace Windows.UI.ViewManagement;

public partial class ApplicationViewTitleBar
{
	public global::Windows.UI.Color? BackgroundColor
	{
		get => TitleBarHelper.BackgroundColor;
		set => TitleBarHelper.BackgroundColor = value;
	}
}
