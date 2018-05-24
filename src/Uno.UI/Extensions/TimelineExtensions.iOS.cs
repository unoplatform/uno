using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Media.Animation
{
    public static partial class TimelineExtensions
	{
		internal static void SetValueBypassPropagation(this Timeline timeline, object value)
		{
			if (timeline.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				timeline.Log().DebugFormat("Setting [{0}] to [{1} / {2}] and bypassing native propagation", value, Storyboard.GetTargetName(timeline), Storyboard.GetTargetProperty(timeline));
			}

			var animatedItem = timeline.PropertyInfo.GetPathItems().Last();

			using (
				DependencyObjectStore.BypassPropagation(
					(DependencyObject)animatedItem.DataContext,
					DependencyProperty.GetProperty(animatedItem.DataContext.GetType(), animatedItem.PropertyName)
				)
			)
			{
				timeline.PropertyInfo.Value = value;
			}
		}
	}
}
