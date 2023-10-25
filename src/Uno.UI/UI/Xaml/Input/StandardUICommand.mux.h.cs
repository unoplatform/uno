using Windows.System;

namespace Windows.UI.Xaml.Input;

partial class StandardUICommand
{
	private bool m_ownsLabel = true;
	private bool m_ownsIconSource = true;
	private bool m_ownsKeyboardAccelerator = true;
	private bool m_ownsDescription = true;

	private VirtualKey m_previousAcceleratorKey;
	private VirtualKeyModifiers m_previousAcceleratorModifiers;
	private bool m_settingPropertyInternally;
}
