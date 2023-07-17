namespace Windows.UI.Xaml;

public sealed partial class Window
{
	internal static void CleanupCurrentForTestsOnly() => _current = null;
}
