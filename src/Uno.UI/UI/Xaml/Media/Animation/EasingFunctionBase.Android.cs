using Android.Animation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
    public abstract partial class EasingFunctionBase
    {
	    internal abstract ITimeInterpolator CreateTimeInterpolator();
    }
}
