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

			// Get the property name from the last part of the
			// specified name (for dotted paths, if they exist)
			var propertyName = animatedItem.PropertyName.Split(new[] { '.' }).Last().Replace("(", "").Replace(")", "");

			var dc = animatedItem.DataContext;
			using (dc != null ?
				DependencyObjectStore.BypassPropagation(
					(DependencyObject)dc,
					DependencyProperty.GetProperty(dc.GetType(), propertyName)
				) :
				// DC may have been collected since it's weakly held
				null
			)
			{
				timeline.PropertyInfo.Value = value;
			}
		}
	}
}
