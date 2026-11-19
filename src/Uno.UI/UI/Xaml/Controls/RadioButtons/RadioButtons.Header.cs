// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference RadioButtons.h

using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RadioButtons
	{
#if !HAS_UNO // Uno specific: the handlers are removed directly in OnRepeaterElementClearing.
		// This is an object that RadioButtons intends to attach to its child RadioButton elements.
		// It contains the revokers for the events on RadioButton that RadioButttons listens to
		// in order to manage selection.  Attaching the revokers to the object allows the parent
		// RadioButtons the ability to "Set it and forget it" since the lifetime of the RadioButton
		// and these event registrations are now intrically linked.
		private class ChildHandlers
		{
			public ToggleButton.Checked_revoker checkedRevoker;
			public ToggleButton.Unchecked_revoker uncheckedRevoker;
		}

		private DependencyProperty s_childHandlersProperty;
#endif

		private int m_selectedIndex = -1;
		// This is used to guard against reentrency when calling select, since select changes
		// the Selected Index/Item which in turn calls select.
		private bool m_currentlySelecting;
		// We block selection before the control has loaded.
		// This is to ensure that we do not overwrite a provided Selected Index/Item value.
		private bool m_blockSelecting = true;

		private ItemsRepeater m_repeater;

		private readonly RadioButtonsElementFactory m_radioButtonsElementFactory;

		private readonly SerialDisposable m_repeaterLoadedRevoker = new();
		private readonly SerialDisposable m_isEnabledChangedRevoker = new();
		private readonly SerialDisposable m_itemsSourceChanged = new();
		private readonly SerialDisposable m_repeaterElementPreparedRevoker = new();
		private readonly SerialDisposable m_repeaterElementClearingRevoker = new();
		private readonly SerialDisposable m_repeaterElementIndexChangedRevoker = new();

		//Test hooks helpers, only function while m_testHooksEnabled == true
		private bool m_testHooksEnabled;

		private const string s_repeaterName = "InnerRepeater";
#if !HAS_UNO
		private const string s_childHandlersPropertyName = "ChildHandlers";
#endif
	}
}
