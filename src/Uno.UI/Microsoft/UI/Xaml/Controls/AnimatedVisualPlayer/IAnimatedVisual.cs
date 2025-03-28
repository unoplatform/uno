// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.idl, commit c6174f1

using System;
using System.Numerics;
using Windows.UI.Composition;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	/// <summary>
	/// An animated Composition.Visual that can be used by other objects, such as an AnimatedVisualPlayer or AnimatedIcon.
	/// </summary>
	public partial interface IAnimatedVisual : IDisposable
	{
		/// <summary>
		/// Gets the root Visual of the animated visual.
		/// </summary>
		Visual RootVisual { get; }

		/// <summary>
		/// Gets the size of the animated visual.
		/// </summary>
		Vector2 Size { get; }

		/// <summary>
		/// Gets the duration of the animated visual.
		/// </summary>
		TimeSpan Duration { get; }
	}
}
