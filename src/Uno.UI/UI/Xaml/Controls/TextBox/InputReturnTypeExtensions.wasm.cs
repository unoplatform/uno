namespace Uno.UI.Xaml.Controls;

internal static class InputReturnTypeExtensions
{
	public static string ToEnterKeyHintValue(this InputReturnType inputReturnType) =>
		inputReturnType switch
		{
			InputReturnType.Enter => "enter",
			InputReturnType.Done => "done",
			InputReturnType.Go => "go",
			InputReturnType.Next => "next",
			InputReturnType.Previous => "previous",
			InputReturnType.Search => "search",
			InputReturnType.Send => "send",
			// Default + Apple-only values (Continue/Join/Route/EmergencyCall) have no
			// HTML enterkeyhint equivalent. Empty string yields the browser default.
			_ => "",
		};
}
