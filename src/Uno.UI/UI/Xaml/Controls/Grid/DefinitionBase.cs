// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System;
using System.Collections;
using XGRIDLENGTH = Windows.UI.Xaml.GridLength;
using XFLOAT = System.Double;

namespace Windows.UI.Xaml.Controls
{
	// Abstract base class for Grid Row/Column Definitions.
	internal interface DefinitionBase
	{
		internal XFLOAT GetUserSizeValue();

		internal GridUnitType GetUserSizeType();

		internal XFLOAT GetUserMaxSize();

		internal XFLOAT GetUserMinSize();

		internal XFLOAT GetEffectiveMinSize();

		internal void SetEffectiveMinSize(XFLOAT value);

		internal XFLOAT GetMeasureArrangeSize();

		internal void SetMeasureArrangeSize(XFLOAT value);

		internal XFLOAT GetSizeCache();

		internal void SetSizeCache(XFLOAT value);

		internal XFLOAT GetFinalOffset();

		internal void SetFinalOffset(XFLOAT value);

		internal GridUnitType GetEffectiveUnitType();

		internal void SetEffectiveUnitType(GridUnitType type);

		internal XFLOAT GetPreferredSize();

		internal void UpdateEffectiveMinSize(XFLOAT newValue);
	}
}
