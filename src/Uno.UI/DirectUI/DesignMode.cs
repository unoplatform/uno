using Windows.ApplicationModel;

namespace DirectUI;

// There are certain features/behaviors that we want to turn off under the designer because they make for a bad
// designer experience.
internal enum DesignerMode
{
	None = 0x0,         // Not in design mode
	V2Only = 0x2,         // Should be used by DCompTreeHost for hooking up visual to CoreApplication.
}

internal static class DesignerInterop
{
	public static bool GetDesignerMode(DesignerMode mode) => false; // TODO: Uno specific: We currently don't have any designer mode.
}
