// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusMovement.h

namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Represents the status of a focus operation.
	/// </summary>
	public partial class FocusMovementResult
	{
		internal FocusMovementResult()
		{
		}

		internal FocusMovementResult(bool wasMoved, bool wasCanceled)
		{
			WasMoved = wasMoved;
			WasCanceled = wasCanceled;
		}

		/// <summary>
		/// Gets a boolean value that indicates whether
		/// focus can be assigned to an object.
		/// </summary>
		public bool Succeeded { get; internal set; }

		internal bool WasMoved { get; }

		internal bool WasCanceled { get; }
	}
}
