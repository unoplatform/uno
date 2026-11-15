// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/dxaml/lib/winrtgeneratedclasses/RichEditBoxSelectionChangingEventArgs.g.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

using Microsoft.UI.Dispatching;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBoxSelectionChangingEventArgs
{
	// Constructors/destructors.
	internal RichEditBoxSelectionChangingEventArgs()
	{
		m_selectionStart = 0;
		m_selectionLength = 0;
		m_cancel = false;
	}

#if !HAS_UNO
	// TODO Uno: The native destructor is empty; CLR type identity replaces QueryInterfaceImpl/AddRefOuter and the abstract activation factory.
	// DirectUI::RichEditBoxSelectionChangingEventArgs::~RichEditBoxSelectionChangingEventArgs()
	// {
	// }
#endif

	// Properties.
	private int GetSelectionStart()
	{
		DispatcherQueue.CheckThreadAccess();
		return m_selectionStart;
	}

	private void SetSelectionStart(int value)
	{
		DispatcherQueue.CheckThreadAccess();
		m_selectionStart = value;
	}

	private int GetSelectionLength()
	{
		DispatcherQueue.CheckThreadAccess();
		return m_selectionLength;
	}

	private void SetSelectionLength(int value)
	{
		DispatcherQueue.CheckThreadAccess();
		m_selectionLength = value;
	}

	private bool GetCancel()
	{
		DispatcherQueue.CheckThreadAccess();
		return m_cancel;
	}

	private void SetCancel(bool value)
	{
		DispatcherQueue.CheckThreadAccess();
		m_cancel = value;
	}
}
