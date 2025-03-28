// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class CommandBarOverflowPresenter : ItemsControl
	{
		private bool m_useFullWidth;
		private bool m_shouldOpenUp;

		public CommandBarOverflowPresenter()
		{
			m_useFullWidth = false;
			m_shouldOpenUp = false;
#if XAMARIN
			ItemsPanel = new ItemsPanelTemplate(() => new StackPanel());
#endif
		}

		public void SetDisplayModeState(bool isFullWidth, bool isOpenUp)
		{
			m_useFullWidth = isFullWidth;
			m_shouldOpenUp = isOpenUp;

			UpdateVisualState(false);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

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

		// Uno Specific: Need to clear the ItemContainerStyle so the style will not stay when the commands
		// are moved from this Secondary ItemsControl to the CommandBar's Primary ItemsControl
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			base.ClearContainerForItemOverride(element, item);
			element.ClearValue(StyleProperty);
		}
	}
}
