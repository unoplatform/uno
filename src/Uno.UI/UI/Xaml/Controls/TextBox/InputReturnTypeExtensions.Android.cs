using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal static class InputReturnTypeExtensions
{
	public static ImeAction ToImeAction(this InputReturnType inputReturnType)
	{
		return inputReturnType switch
		{
			InputReturnType.Default => ImeAction.None,
			InputReturnType.Go => ImeAction.Go,
			InputReturnType.Send => ImeAction.Send,
			InputReturnType.Next => ImeAction.Next,
			InputReturnType.Search => ImeAction.Search,
			InputReturnType.Done => ImeAction.Done,
			InputReturnType.Previous => ImeAction.Previous,
			_ => ImeAction.None
		};
	}
}
