// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Page_Partial.cpp

#nullable enable

using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class Page
	{
		private protected override void OnLoaded()
		{
			base.OnLoaded();
			var spCurrentFocusedElement = this.GetFocusedElement();

			var focusManager = VisualTree.GetFocusManagerForElement(this);
			bool setDefaultFocus = focusManager?.IsPluginFocused() == true;

			if (setDefaultFocus && spCurrentFocusedElement == null)
			{
				// Set the focus on the first focusable control
				var spFirstFocusableElementCDO = focusManager?.GetFirstFocusableElement(this);

				if (spFirstFocusableElementCDO != null && focusManager != null)
				{
					var spFirstFocusableElementDO = spFirstFocusableElementCDO;

					focusManager.InitialFocus = true;

					try
					{
						var focusUpdated = this.SetFocusedElement(
							spFirstFocusableElementDO,
							FocusState.Programmatic,
							false /*animateIfBringIntoView*/);
					}
					catch (Exception ex)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().LogError($"Setting initial page focus failed: {ex}");
						}
					}
				}

				if (spFirstFocusableElementCDO == null)
				{
					// Narrator listens for focus changed events to determine when the UI Automation tree needs refreshed. If we don't set default focus (on Phone) or if we fail to find a focusable element,
					// we will need notify the narror of the UIA tree change when page is loaded.
					var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.AutomationFocusChanged);

					if (bAutomationListener)
					{
						Uno.UI.Xaml.Core.CoreServices.Instance.UIARaiseFocusChangedEventOnUIAWindow(this);
					}
				}
			}
		}
	}
}
