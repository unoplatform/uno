// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ContentPresenter.h, tag winui3/release/1.8.2, commit bac7a9c33

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private object m_cachedContent;

	private bool m_bInOnApplyTemplate;
	private bool m_bDataContextInvalid = true;

	// Uno specific: the metadata options ContentProperty is registered with.
	private const FrameworkPropertyMetadataOptions ContentPropertyOptions = FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure;
}
