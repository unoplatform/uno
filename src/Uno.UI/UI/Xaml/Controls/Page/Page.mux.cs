// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Page_Partial.cpp

#nullable enable

using System;
using System.Runtime.CompilerServices;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
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
				// Uno specific: If the page is focusable itself, we want to
				// give it focus instead of the first element.
				if (FocusProperties.IsFocusable(this))
				{
					this.SetFocusedElement(
						this,
						FocusState.Programmatic,
						animateIfBringIntoView: false);
					return;
				}

				// Set the focus on the first focusable control
				var spFirstFocusableElementCDO = focusManager?.GetFirstFocusableElement(this);

				if (spFirstFocusableElementCDO != null && focusManager != null)
				{
					var spFirstFocusableElementDO = spFirstFocusableElementCDO;

					focusManager.InitialFocus = true;

					TrySetFocusedElement(spFirstFocusableElementDO);

					focusManager.InitialFocus = false;
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

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void TrySetFocusedElement(DependencyObject spFirstFocusableElementDO)
		{
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
	}
}
