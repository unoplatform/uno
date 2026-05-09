// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\ThemeGenerator.h, commit 978ab6363

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

internal struct TimingFunctionDescription
{
	public Point cp1;
	public Point cp2;
	public Point cp3;
	public Point cp4;

	public TimingFunctionDescription()
	{
		cp1 = new Point(0.0, 0.0);
		cp2 = new Point(0.0, 0.0);
		cp3 = new Point(1.0, 1.0);
		cp4 = new Point(1.0, 1.0);
	}

	public bool IsLinear()
	{
		return cp1.X == 0.0 && cp1.Y == 0.0 && cp2.X == 0.0 && cp2.Y == 0.0 && cp3.X == 1.0 && cp3.Y == 1.0 && cp4.X == 1.0 && cp4.Y == 1.0;
	}
}
