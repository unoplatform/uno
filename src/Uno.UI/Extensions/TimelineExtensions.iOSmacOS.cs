using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public static partial class TimelineExtensions
	{
		internal static void SetValueBypassPropagation(this Timeline timeline, object value)
		{
			if (timeline.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				timeline.Log().DebugFormat("Setting [{0}] to [{1} / {2}] and bypassing native propagation", value, Storyboard.GetTargetName(timeline), Storyboard.GetTargetProperty(timeline));
			}

			timeline.PropertyInfo.Value = value;
		}
	}
}
