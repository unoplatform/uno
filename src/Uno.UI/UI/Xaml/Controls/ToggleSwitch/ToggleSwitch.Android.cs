using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Disposables;
using Uno.Logging;
using Uno.UI;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class ToggleSwitch
	{
		protected override void OnLoaded()
		{
			base.OnLoaded();

			Clickable = true;
			Focusable = true;
			FocusableInTouchMode = true;
		}
	}
}
