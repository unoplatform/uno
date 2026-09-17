// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/plat/win/desktop/WindowLessSiteHost.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class RichEditBoxAutomationPeer
{
	// WindowLessSiteHost.cpp, lines 281-321 (GetUnwrappedPattern).
	/// <inheritdoc />
	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		var isRichEdit = ((RichEditBox)Owner).AcceptsRichText();

		// Restrict RichEditBox from supporting Value pattern. RichEditBox provides complex content where value pattern would result in loss of data
		// also we dont raise value propertychange event in case of RichEditBox.
		if (!isRichEdit || patternInterface != PatternInterface.Value)
		{
#if HAS_UNO
			return GetManagedPatternProvider(patternInterface);
#else
			// TODO Uno: The managed provider adapter replaces IRicheditWindowlessAccessibility::CreateProvider.
			// IFC(m_pProvider->GetPatternProvider(patternID, &pUnk));
			// *ppPattern = (void*) pUnk;
#endif
		}

		return null;
	}
}
