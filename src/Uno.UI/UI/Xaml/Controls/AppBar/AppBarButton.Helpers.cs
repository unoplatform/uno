#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarButton : IAppBarButtonHelpersProvider
{
	DependencyProperty IAppBarButtonHelpersProvider.GetIsCompactDependencyProperty() => IsCompactProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetUseOverflowStyleDependencyProperty() => UseOverflowStyleProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetLabelPositionDependencyProperty() => LabelPositionProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetLabelDependencyProperty() => LabelProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetIconDependencyProperty() => IconProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetKeyboardAcceleratorTextDependencyProperty() => KeyboardAcceleratorTextOverrideProperty;

	bool IAppBarButtonHelpersProvider.m_isTemplateApplied { get => m_isTemplateApplied; set => m_isTemplateApplied = value; }

	bool IAppBarButtonHelpersProvider.m_ownsToolTip { get => m_ownsToolTip; set => m_ownsToolTip = value; }

	TextBlock? IAppBarButtonHelpersProvider.m_tpKeyboardAcceleratorTextLabel { get => m_tpKeyboardAcceleratorTextLabel; set => m_tpKeyboardAcceleratorTextLabel = value; }

	bool IAppBarButtonHelpersProvider.m_isWithKeyboardAcceleratorText { get => m_isWithKeyboardAcceleratorText; set => m_isWithKeyboardAcceleratorText = value; }

	double IAppBarButtonHelpersProvider.m_maxKeyboardAcceleratorTextWidth { get => m_maxKeyboardAcceleratorTextWidth; set => m_maxKeyboardAcceleratorTextWidth = value; }

	InputDeviceType IAppBarButtonHelpersProvider.m_inputDeviceTypeUsedToOpenOverflow { get => m_inputDeviceTypeUsedToOpenOverflow; set => m_inputDeviceTypeUsedToOpenOverflow = value; }

	string IAppBarButtonHelpersProvider.Label { get => Label; set => Label = value; }

	string IAppBarButtonHelpersProvider.KeyboardAcceleratorTextOverride { get => KeyboardAcceleratorTextOverride; set => KeyboardAcceleratorTextOverride = value; }

	IAppBarButtonTemplateSettings IAppBarButtonHelpersProvider.TemplateSettings { get => TemplateSettings; set => TemplateSettings = (AppBarButtonTemplateSettings)value; }
}
