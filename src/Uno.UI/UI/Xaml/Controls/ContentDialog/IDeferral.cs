using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	internal interface IDeferral
	{
		Action DeferralAction { get; set; }
	}
}
