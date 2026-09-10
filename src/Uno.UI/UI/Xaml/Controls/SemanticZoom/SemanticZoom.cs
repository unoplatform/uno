// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference Microsoft.UI.Xaml.Controls.cs, commit 3c9c168844

#nullable enable

using DirectUI;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a scrollable control that incorporates two views that have a semantic relationship.
/// </summary>
[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(ZoomedInView))]
public sealed partial class SemanticZoom : Control, IDirectManipulationStateChangeHandler, IBackButtonListener
{
}
