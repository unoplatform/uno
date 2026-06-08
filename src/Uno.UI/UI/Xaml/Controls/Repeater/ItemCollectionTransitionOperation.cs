// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsRepeater.idl, commit 5f9e851133b3

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the type of transition operation to animate.
/// </summary>
public enum ItemCollectionTransitionOperation
{
	/// <summary>
	/// A data object was added to the collection.
	/// </summary>
	Add = 0,

	/// <summary>
	/// A data object was removed from the collection.
	/// </summary>
	Remove = 1,

	/// <summary>
	/// A data object was moved to a different location in the collection.
	/// </summary>
	Move = 2,
}
