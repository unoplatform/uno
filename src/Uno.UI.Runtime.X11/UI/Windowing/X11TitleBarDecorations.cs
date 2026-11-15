namespace Uno.UI.Runtime.X11;

/// <summary>
/// Combines the two states that drive a window's Motif decorations word: content extended into the title bar,
/// and the overlapped presenter's title bar. Extended content hides the title bar whatever the presenter asks
/// for, as on Win32, so the order the two are set in doesn't matter.
/// </summary>
internal sealed class X11TitleBarDecorations
{
	// MWM_DECOR_* bits from MwmUtil.h.
	internal const int All = 0x01;
	internal const int Border = 0x02;
	internal const int Title = 0x08;

	// Every decoration but All, which would turn the listed ones into exclusions.
	private const int EachDecoration = 0x7E;

	private bool _extendsContentIntoTitleBar;
	private bool _presenterHasTitleBar = true;

	internal int ExtendContentIntoTitleBar(bool extend)
	{
		_extendsContentIntoTitleBar = extend;
		return Decorations;
	}

	internal int SetPresenterHasTitleBar(bool hasTitleBar)
	{
		_presenterHasTitleBar = hasTitleBar;
		return Decorations;
	}

	internal int Decorations
		=> _extendsContentIntoTitleBar
			? 0
			: _presenterHasTitleBar
				? 0xFF
				// Title and Border go together: some window managers draw a title bar for Border alone.
				: EachDecoration & ~(Title | Border);
}
