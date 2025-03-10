#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarToggleButton : IAppBarButtonHelpersProvider
{
	DependencyProperty IAppBarButtonHelpersProvider.GetIsCompactDependencyProperty() => IsCompactProperty;

	DependencyProperty IAppBarButtonHelpersProvider.GetUseOverflowStyleDependencyProperty() => UseOverflowStyleProperty;

	DependencyProperty IAppBarButtonHelpersProvider.GetLabelPositionDependencyProperty() => LabelPositionProperty;

	DependencyProperty IAppBarButtonHelpersProvider.GetLabelDependencyProperty() => LabelProperty;

	DependencyProperty IAppBarButtonHelpersProvider.GetIconDependencyProperty() => IconProperty;

	DependencyProperty IAppBarButtonHelpersProvider.GetKeyboardAcceleratorTextDependencyProperty() => KeyboardAcceleratorTextOverrideProperty;

	bool IAppBarButtonHelpersProvider.m_ownsToolTip { get => m_ownsToolTip; set => m_ownsToolTip = value; }

	TextBlock? IAppBarButtonHelpersProvider.m_tpKeyboardAcceleratorTextLabel { get => m_tpKeyboardAcceleratorTextLabel; set => m_tpKeyboardAcceleratorTextLabel = value; }

	bool IAppBarButtonHelpersProvider.m_isWithKeyboardAcceleratorText { get => m_isWithKeyboardAcceleratorText; set => m_isWithKeyboardAcceleratorText = value; }

	double IAppBarButtonHelpersProvider.m_maxKeyboardAcceleratorTextWidth { get => m_maxKeyboardAcceleratorTextWidth; set => m_maxKeyboardAcceleratorTextWidth = value; }

	InputDeviceType IAppBarButtonHelpersProvider.m_inputDeviceTypeUsedToOpenOverflow { get => m_inputDeviceTypeUsedToOpenOverflow; set => m_inputDeviceTypeUsedToOpenOverflow = value; }

	string IAppBarButtonHelpersProvider.Label { get => Label; set => Label = value; }

	string IAppBarButtonHelpersProvider.KeyboardAcceleratorTextOverride { get => KeyboardAcceleratorTextOverride; set => KeyboardAcceleratorTextOverride = value; }

	IAppBarButtonTemplateSettings IAppBarButtonHelpersProvider.TemplateSettings { get => TemplateSettings; set => TemplateSettings = (AppBarToggleButtonTemplateSettings)value; }
	bool IAppBarButtonHelpersProvider.m_isTemplateApplied { get => m_isTemplateApplied; set => m_isTemplateApplied = value; }

	CommandBarDefaultLabelPosition IAppBarButtonHelpersProvider.GetEffectiveLabelPosition() => GetEffectiveLabelPosition();

	void IAppBarButtonHelpersProvider.GoToState(bool useTransitions, string stateName) => GoToState(useTransitions, stateName);

	void IAppBarButtonHelpersProvider.StopAnimationForWidthAdjustments() => StopAnimationForWidthAdjustments();
}
