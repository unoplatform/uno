// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/dxaml/lib/winrtgeneratedclasses/RichEditBoxTextChangingEventArgs.g.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

using Microsoft.UI.Dispatching;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBoxTextChangingEventArgs
{
	// Constructors/destructors.
	internal RichEditBoxTextChangingEventArgs() => m_isContentChanging = false;

#if !HAS_UNO
	// TODO Uno: The native destructor is empty; CLR type identity replaces QueryInterfaceImpl/AddRefOuter and the abstract activation factory.
	// DirectUI::RichEditBoxTextChangingEventArgs::~RichEditBoxTextChangingEventArgs()
	// {
	// }
#endif

	// Properties.
	private bool GetIsContentChanging()
	{
		DispatcherQueue.CheckThreadAccess();
		return m_isContentChanging;
	}

	private void SetIsContentChanging(bool value)
	{
		DispatcherQueue.CheckThreadAccess();
		m_isContentChanging = value;
	}
}
