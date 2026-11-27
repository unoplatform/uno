// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Hyperlink.h, Hyperlink.cpp, HyperLink_Partial.cpp

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Documents
{
	public partial class Hyperlink
	{
		internal
			bool IsFocusable()
		{
			var element = GetContainingFrameworkElement();
			return
				element != null &&
#if __CROSSRUNTIME__
				// WinUI checks element.IsActive && IsActive; the TextElement side has no Uno equivalent yet.
				element.IsActiveOrAttachedUnderActiveAncestor() &&
#endif
				element is not Control { IsEnabled: false } &&
				element.Visibility == Visibility.Visible &&
				element.AreAllAncestorsVisible() &&
				IsTabStop;
		}

		internal IFocusable? GetIFocusable() =>
			_focusableHelper;

		//------------------------------------------------------------------------
		//
		//  Click handler for the Hyperlink - performs URI navigation.
		//
		//------------------------------------------------------------------------
		private void OnClick()
		{
			var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.InvokePatternOnInvoked);
			if (bAutomationListener)
			{
				var spAutomationPeer = GetOrCreateAutomationPeer();
				if (spAutomationPeer is not null)
				{
					spAutomationPeer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
				}
			}

			var spNavigateUri = NavigateUri;
			if (spNavigateUri is not null)
			{
				// Invoke default protocol handler app using Windows.System.Launcher
				_ = Launcher.LaunchUriAsync(spNavigateUri);
			}

			// Request a play invoke sound for click Hyperlink
			ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Invoke, this);
		}

		internal void AutomationHyperlinkClick()
		{
			// Call Navigate on core object that handles both Click event as well as NavigateUri.
			Navigate();
		}

		// MUX Reference Hyperlink::OnCreateAutomationPeerImpl — Hyperlink exposes a HyperlinkAutomationPeer.
		private protected override Automation.Peers.AutomationPeer OnCreateAutomationPeerCore()
			=> new Automation.Peers.HyperlinkAutomationPeer(this);
	}
}
