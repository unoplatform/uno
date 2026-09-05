// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls;

// Manages virtualization windows (visible/realization).
// This class does the equivalent behavior as ViewportManagerWithPlatformFeatures class
// except that here we do not use EffectiveViewport and ScrollAnchoring features added to the framework in RS5.
// Instead we use the IRepeaterScrollingSurface internal API. This class is used when building in MUX and
// should work down-level.
internal partial class ViewportManagerDownLevel : ViewportManager
{
}
