using Android.Animation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
    public partial class EasingFunctionBase
    {
		internal virtual ITimeInterpolator CreateTimeInterpolator() { throw new InvalidOperationException("Don't call base.CreateTimeInterpolator()"); }
    }
}
