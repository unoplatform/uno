// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.idl, commit 3cae15f0

using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Creates animated visuals and can defer animation creation until playback begins.
	/// </summary>
	public partial interface IAnimatedVisualSource3
	{
		/// <summary>
		/// Creates an animated visual for the specified compositor.
		/// </summary>
		/// <param name="compositor">The compositor that will host the animated visual.</param>
		/// <param name="diagnostics">Diagnostics data produced while creating the animated visual.</param>
		/// <param name="createAnimations">true to create animations immediately; otherwise, false.</param>
		/// <returns>The created animated visual.</returns>
		IAnimatedVisual2 TryCreateAnimatedVisual(Compositor compositor, out object diagnostics, bool createAnimations);
	}
}
