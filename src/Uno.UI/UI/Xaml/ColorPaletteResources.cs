// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\core\core\elements\Resources.cpp, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml;

/// <summary>
/// A specialized <see cref="ResourceDictionary"/> containing color resources used by XAML elements.
/// </summary>
/// <remarks>
/// <para>ColorPaletteResources provides a set of color properties that map to system color resource keys.
/// Setting a property like <see cref="Accent"/> will add the corresponding resource key
/// ("SystemAccentColor") to the dictionary.</para>
/// </remarks>
public partial class ColorPaletteResources : ResourceDictionary
{
}
