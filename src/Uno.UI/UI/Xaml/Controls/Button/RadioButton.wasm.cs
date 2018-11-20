using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class RadioButton : ToggleButton
	{
		private IEnumerable<RadioButton> GetOtherHierarchicalGroupMembers()
		{
			return (Parent as FrameworkElement)?
				.GetChildren()
				.OfType<RadioButton>()
				.Where(rb => rb != this)
				?? Enumerable.Empty<RadioButton>();
		}
	}
}
