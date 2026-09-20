// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls;

partial class StackLayoutState
{
	/// <summary>
	/// The layout origin reported by the previous measure pass, held across passes by
	/// <see cref="StackLayout.StabilizeExtentOrigin"/>. NaN until a pass reports one, and reset
	/// back to NaN by OnElementSizesReset so a held origin never outlives the content it came from.
	/// </summary>
	internal double _lastReportedExtentMajorStart = double.NaN;
}
