// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if !__SKIA__
namespace Microsoft.UI.Xaml.Controls;

// Uno-specific cached state used by the viewport-change optimization
// implemented in FlowLayout.IsSignificantViewportChange on native targets.
partial class FlowLayoutState
{
	internal double Uno_LastKnownAverageLineSize;
}
#endif
