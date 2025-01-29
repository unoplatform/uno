#nullable enable

namespace Microsoft.UI.Xaml.Controls;

internal interface IAppBarButtonHelpersProvider
{
	bool m_ownsToolTip { get; internal set; }

	TextBlock? m_tpKeyboardAcceleratorTextLabel { get; set; }

	DependencyProperty GetIsCompactDependencyProperty();

	DependencyProperty GetUseOverflowStyleDependencyProperty();

	DependencyProperty GetLabelPositionDependencyProperty();

	DependencyProperty GetLabelDependencyProperty();

	DependencyProperty GetIconDependencyProperty();

	DependencyProperty GetKeyboardAcceleratorTextDependencyProperty();
}
