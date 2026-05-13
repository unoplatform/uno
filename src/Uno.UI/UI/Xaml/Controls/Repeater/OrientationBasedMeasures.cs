// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference OrientationBasedMeasures.h, commit 4b206bce3

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Note: WinUI declares OrientationBasedMeasures as a base class. C# does not
// support multiple inheritance, so the Uno port models it as an interface that
// each layout class implements explicitly. Helper math lives in
// OrientationBasedMeasuresExtensions; the per-class wrappers in
// {Class}.OrientationBasedMeasures.cs surface those helpers as instance methods,
// matching the call sites in the C++ source.
internal interface OrientationBasedMeasures
{
	ScrollOrientation ScrollOrientation { get; set; }
}
