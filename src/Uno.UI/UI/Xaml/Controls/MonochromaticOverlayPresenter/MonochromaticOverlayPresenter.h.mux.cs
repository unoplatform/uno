// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference MonochromaticOverlayPresenter.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using Microsoft.UI.Composition;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class MonochromaticOverlayPresenter : Grid
{
	private CompositionEffectFactory? _effectFactory;
	private Color _replacementColor;
	private bool _needsBrushUpdate;
}
