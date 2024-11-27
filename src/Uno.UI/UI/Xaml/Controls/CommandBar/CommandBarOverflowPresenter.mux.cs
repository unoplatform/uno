// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\CommandBarOverflowPresenter_Partial.cpp, tag winui3/release/1.6.3, commit 66d24dfff3b2763ab3be096a2c7cbaafc81b31eb

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Displays the overflow content of a CommandBar.
/// </summary>
public partial class CommandBarOverflowPresenter : ItemsControl
{
	private bool m_useFullWidth;
	private bool m_shouldOpenUp;

	/// <summary>
	/// Initializes a new instance of the CommandBarOverflowPresenter class.
	/// </summary>
	public CommandBarOverflowPresenter()
	{
		m_useFullWidth = false;
		m_shouldOpenUp = false;

#if HAS_UNO // Uno specific: Set StackPanel as default ItemsPanel for CommandBarOverflowPresenter
		ItemsPanel = new ItemsPanelTemplate(() => new StackPanel() { HorizontalAlignment = HorizontalAlignment.Stretch });
#endif
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		UpdateVisualState(false);
	}

	public void SetDisplayModeState(bool isFullWidth, bool isOpenUp)
	{
		m_useFullWidth = isFullWidth;
		m_shouldOpenUp = isOpenUp;

		UpdateVisualState(false);
	}

	private protected override void ChangeVisualState(bool useTransitions)
	{
		base.ChangeVisualState(useTransitions);

		if (m_useFullWidth && m_shouldOpenUp)
		{
			GoToState(useTransitions, "FullWidthOpenUp");
		}
		else if (m_useFullWidth && !m_shouldOpenUp)
		{
			GoToState(useTransitions, "FullWidthOpenDown");
		}
		else
		{
			GoToState(useTransitions, "DisplayModeDefault");
		}
	}

#if HAS_UNO
	// Uno Specific: Need to clear the ItemContainerStyle so the style will not stay when the commands
	// are moved from this Secondary ItemsControl to the CommandBar's Primary ItemsControl
	protected override void ClearContainerForItemOverride(DependencyObject element, object item)
	{
		base.ClearContainerForItemOverride(element, item);
		element.ClearValue(StyleProperty);
	}
#endif
}
