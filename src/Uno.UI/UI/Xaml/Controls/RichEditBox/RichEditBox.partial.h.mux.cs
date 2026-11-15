// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/dxaml/lib/RichEditBox_Partial.h, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	private RichEditBoxTextChangingEventArgs? m_textChangingEventArgs;
	private RichEditBoxSelectionChangingEventArgs? m_selectionChangingEventArgs;

	// "Header" template part

	// Holds a reference to the Header Content Presenter so we can collapse it when not in use
	private UIElement? m_tpHeaderPresenter;
#if !HAS_UNO
	// TODO Uno: Feature_HeaderPlacement is not part of the supported WinUI contract.
	// TrackerPtr<xaml::IUIElement> m_requiredHeaderPresenter;
#endif
	private UIElement? m_tpPlaceholderTextPresenter;

	// We dont want OnPropertyChanged to trigger UpdateHeaderPresenterVisibility while the template is being loaded
	// so we use this as a flag while initializing the object
	private bool m_isInitializing;

	private readonly SerialDisposable m_storyboardCompletedToken = new();

	private bool m_isAnimatingHeight;

	private void SetPlaceholderTextPresenter(UIElement? pValue)
	{
		m_tpPlaceholderTextPresenter = pValue;
#if HAS_UNO
		OnManagedPlaceholderPresenterChanged(pValue);
#endif
	}
}
