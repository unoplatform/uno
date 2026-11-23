// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressBarAutomationPeer.h

using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class ProgressBarAutomationPeer
{
	// IRangeValueProvider is necessary here to override IsReadOnly() to true.
	bool IRangeValueProvider.IsReadOnly => true;
}
