using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal static class InputReturnTypeExtensions
{
	public static UIReturnKeyType ToUIReturnKeyType(this InputReturnType inputReturnType)
	{
		return inputReturnType switch
		{
			InputReturnType.Default => UIReturnKeyType.Default,
			InputReturnType.Go => UIReturnKeyType.Go,
			InputReturnType.Send => UIReturnKeyType.Send,
			InputReturnType.Next => UIReturnKeyType.Next,
			InputReturnType.Search => UIReturnKeyType.Search,
			InputReturnType.Done => UIReturnKeyType.Done,
			InputReturnType.Previous => UIReturnKeyType.Previous,
			_ => UIReturnKeyType.Default
		};
	}
}
