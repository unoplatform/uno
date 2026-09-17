// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/core/native/text/Controls/TextBoxBaseAutomationPeer.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class RichEditBoxAutomationPeer
{
	// TextBoxBaseAutomationPeer.cpp, lines 77-120.
	/// <inheritdoc />
	protected override void SetFocusCore()
	{
		if (Owner is RichEditBox textBoxBase)
		{
#if !HAS_UNO
			// TODO Uno: Platform automation bridges supply input-origin notifications without a native content root.
			// CContentRoot* contentRoot = VisualTree::GetContentRootForElement(textBoxBase);
			// contentRoot->GetInputManager().OnSetFocusFromUIA();
#endif
			if (textBoxBase.FocusState == FocusState.Unfocused)
			{
				// SIP on phone blocks on ProgrammaticFocus. But when Assistive technologies like Narrator
				// puts insertion point on edit control it calls setfocus via UIA. As with this interaction
				// we do not have the information whether the interaction happened due to Touch or Keyboard,
				// setting Programmatic focus seemed ideal.This worked fine on phone as Xaml on phone was
				// not respecting PreventKeyboardDisplayOnProgrammaticFocus, but with that fixed now, when
				// Narrator interacts with edit boxes on phone with this setting SIP doesn't show up.
				// Here we want to make a very scoped change to set Pointer focus only when this property
				// is set and we are on edit controls.
				if (textBoxBase.PreventKeyboardDisplayOnProgrammaticFocus)
				{
					textBoxBase.Focus(FocusState.Pointer);
				}
				else
				{
					textBoxBase.Focus(FocusState.Programmatic);
				}
			}
		}
	}
}
