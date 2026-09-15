// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Automation.Peers;

internal sealed class AutomationPeerUnavailableException : InvalidOperationException
{
	private const int UIA_E_INVALIDOPERATION = unchecked((int)0x80131509);

	internal AutomationPeerUnavailableException()
		: base("UIA element is not available")
	{
		HResult = UIA_E_INVALIDOPERATION;
	}
}
