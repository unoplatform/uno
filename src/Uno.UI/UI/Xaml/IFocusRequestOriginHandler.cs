#nullable enable

namespace Microsoft.UI.Xaml;

internal interface IFocusRequestOriginHandler
{
	void OnFocusRequesting(FocusState focusState);

	void OnFocusRequested(FocusState focusState, bool succeeded);
}
