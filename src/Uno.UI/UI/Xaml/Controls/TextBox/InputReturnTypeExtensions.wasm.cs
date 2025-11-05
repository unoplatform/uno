using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

internal static class InputReturnTypeExtensions
{
	public static string ToEnterKeyHintValue(this InputReturnType inputReturnType) =>
		inputReturnType switch
		{
			_ when Enum.IsDefined(inputReturnType) => inputReturnType.ToString().ToLowerInvariant(),
			_ => "",
		};
}
