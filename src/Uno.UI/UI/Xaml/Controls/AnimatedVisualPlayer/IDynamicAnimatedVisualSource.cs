// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.idl, commit 3cae15f0

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// A source that can invalidate the animated visual it produces.
	/// </summary>
	public partial interface IDynamicAnimatedVisualSource : IAnimatedVisualSource
	{
		/// <summary>
		/// Raised when the animated visual produced by this source has changed and should be recreated.
		/// </summary>
		event TypedEventHandler<IDynamicAnimatedVisualSource, object> AnimatedVisualInvalidated;
	}
}
