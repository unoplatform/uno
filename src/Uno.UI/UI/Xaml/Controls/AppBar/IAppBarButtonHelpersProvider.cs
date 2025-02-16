#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Input;

namespace Uno.UI.Xaml.Controls;

internal interface IAppBarButtonHelpersProvider
{
	bool m_isTemplateApplied { get; set; }

	bool m_ownsToolTip { get; internal set; }

	TextBlock? m_tpKeyboardAcceleratorTextLabel { get; set; }

	bool m_isWithKeyboardAcceleratorText { get; set; }

	double m_maxKeyboardAcceleratorTextWidth { get; set; }

	InputDeviceType m_inputDeviceTypeUsedToOpenOverflow { get; set; }

	string Label { get; set; }

	string KeyboardAcceleratorTextOverride { get; set; }

	IAppBarButtonTemplateSettings TemplateSettings { get; set; }

	void GoToState(bool useTransitions, string stateName);

	void UpdateInternalStyles();

	void StopAnimationForWidthAdjustments();

	CommandBarDefaultLabelPosition GetEffectiveLabelPosition();

	DependencyProperty GetIsCompactDependencyProperty();

	DependencyProperty GetUseOverflowStyleDependencyProperty();

	DependencyProperty GetLabelPositionDependencyProperty();

	DependencyProperty GetLabelDependencyProperty();

	DependencyProperty GetIconDependencyProperty();

	DependencyProperty GetKeyboardAcceleratorTextDependencyProperty();
}
