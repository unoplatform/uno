// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.idl, commit 3cae15f0

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// An animated Composition.Visual that can create and destroy its animations on demand.
	/// </summary>
	public partial interface IAnimatedVisual2 : IAnimatedVisual
	{
		/// <summary>
		/// Creates the animations used by the animated visual.
		/// </summary>
		void CreateAnimations();

		/// <summary>
		/// Destroys the animations used by the animated visual.
		/// </summary>
		void DestroyAnimations();
	}
}
