// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;

namespace Windows.UI.Xaml.Controls;

internal partial class ZoomFactorChange : ViewChange
{
	public float ZoomFactor()
	{
		return m_zoomFactor;
	}

	public Vector2? CenterPoint()
	{
		return m_centerPoint;
	}

	private float m_zoomFactor;
	private Vector2? m_centerPoint;
}
