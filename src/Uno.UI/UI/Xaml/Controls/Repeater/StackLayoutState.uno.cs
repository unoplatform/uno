// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if !__SKIA__
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Uno-specific cached state used by the viewport-change optimization
// implemented in StackLayout.IsSignificantViewportChange on native targets.
partial class StackLayoutState
{
	internal double Uno_LastKnownAverageElementSize;
	internal int Uno_LastKnownRealizedElementsCount;
	internal int Uno_LastKnownItemsCount;
	internal Size Uno_LastKnownDesiredSize;
}
#endif
