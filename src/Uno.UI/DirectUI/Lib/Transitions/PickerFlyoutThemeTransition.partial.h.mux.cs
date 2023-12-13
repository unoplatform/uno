using System;
using Windows.UI.Xaml.Media.Animation;

namespace DirectUI;

internal class PickerFlyoutThemeTransition : Transition
{
	private const int s_OpenDuration = 250;
	private const int s_CloseDuration = 167;
	private const int s_OpacityChangeDuration = 83;
	private const int s_OpacityChangeBeginTime = s_CloseDuration - s_OpacityChangeDuration;
}
