namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public enum InputSystemCursorShape
#else
internal enum InputSystemCursorShape
#endif
{
	Arrow = 0,
	Cross = 1,
	Hand = 3,
	Help = 4,
	IBeam = 5,
	SizeAll = 6,
	SizeNortheastSouthwest = 7,
	SizeNorthSouth = 8,
	SizeNorthwestSoutheast = 9,
	SizeWestEast = 10,
	UniversalNo = 11,
	UpArrow = 12,
	Wait = 13,
	Pin = 14,
	Person = 0xF,
	AppStarting = 0x10
}
