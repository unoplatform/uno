#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by providers with a native message channel or a compatibility
/// bridge installed before page scripts can subscribe to message events.
/// </summary>
internal interface ISupportsPostWebMessage
{
	void PostWebMessageAsJson(string json);

	void PostWebMessageAsString(string message);
}
