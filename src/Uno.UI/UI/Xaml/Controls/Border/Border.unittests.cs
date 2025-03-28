using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls;

partial class Border
{
	public override IEnumerable<UIElement> GetChildren()
		=> Child is FrameworkElement fe ? new[] { fe } : Array.Empty<FrameworkElement>();
}
