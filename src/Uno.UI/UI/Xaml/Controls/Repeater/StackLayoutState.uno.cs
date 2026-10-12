// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls;

partial class StackLayoutState
{
	/// <summary>
	/// The layout origin reported by the previous measure pass, held across passes by
	/// <see cref="StackLayout.StabilizeExtentOrigin"/>. NaN until a pass reports one, and reset
	/// back to NaN by <see cref="ResetExtentOrigin"/> so a held origin never outlives the content or context it came from.
	/// </summary>
	internal double _lastReportedExtentMajorStart = double.NaN;

	/// <summary>The <see cref="StackLayout.ExtentOriginVersion"/> the held origin was computed for.</summary>
	internal uint _extentOriginVersion = uint.MaxValue;

	internal void ResetExtentOrigin()
	{
		_lastReportedExtentMajorStart = double.NaN;
		_extentOriginVersion = uint.MaxValue;
	}
}
