// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;

namespace Microsoft.UI.Xaml.Controls;

internal partial class ZoomFactorChangeWithAdditionalVelocity : ViewChangeBase
{
	public float ZoomFactorVelocity()
	{
		return m_zoomFactorVelocity;
	}

	public float AnticipatedZoomFactorChange()
	{
		return m_anticipatedZoomFactorChange;
	}

	public Vector2? CenterPoint()
	{
		return m_centerPoint;
	}

	public float? InertiaDecayRate()
	{
		return m_inertiaDecayRate;
	}

	private float m_zoomFactorVelocity;
	private float m_anticipatedZoomFactorChange;
	private Vector2? m_centerPoint;
	private float? m_inertiaDecayRate;
}
