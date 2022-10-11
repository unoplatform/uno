#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	public partial class VisualStateManager
	{
		/// <summary>
		/// Set visual states as DOM attribute if AssignDOMXamlProperties is enabled - useful for debugging.
		/// </summary>
		/// <param name="groups"></param>
		/// <param name="templateRoot"></param>
		private static void TryAssignDOMVisualStates(IList<VisualStateGroup> groups, IFrameworkElement templateRoot)
		{
			if (Uno.UI.FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				var sb = new StringBuilder();
				sb.Append('[');
				foreach (var group in groups)
				{
					sb.Append($"{group}: {group.CurrentState}, ");
				}
				sb.Append(" ]");
				(templateRoot as UIElement)?.UpdateDOMXamlProperty("visualstates", sb.ToString());
			}
		}
	}
}
