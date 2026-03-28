// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutContext.cpp, commit 5f9e851133b3

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class LayoutContext
{
	// #pragma region ILayoutContext

	/// <summary>
	/// Gets or sets an object that represents the state of a layout.
	/// </summary>
	/// <value>An object that represents the state of a layout.</value>
	public object LayoutState
	{
		get => LayoutStateCore;
		set => LayoutStateCore = value;
	}

	// #pragma endregion

	// #pragma region ILayoutContextOverrides

	/// <summary>
	/// Implements the behavior of <see cref="LayoutState"/> in a derived or custom LayoutContext.
	/// </summary>
	/// <value>An object that represents the state of a layout.</value>
	// Uno-specific: widened to protected internal so RepeaterLayoutContext and LayoutContextAdapter
	// (same assembly) can override it directly.
	protected internal virtual object LayoutStateCore
	{
		get => throw new NotImplementedException();
		set => throw new NotImplementedException();
	}

	// #pragma endregion
}
