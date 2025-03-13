using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal static class RuntimeFeatureBehavior
{
	internal static RuntimeEnabledFeatureDetector GetRuntimeEnabledFeatureDetector() => new();
}
