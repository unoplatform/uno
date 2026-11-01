// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference KeyFrameCollection.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class KeyFrameCollection
{
	// Scans the internal keyframe collection and makes it uniform
	internal void InitializeKeyFrames(float rOneIterationDuration)
	{
		EnsureSnapshot();

		m_isSorted = true;

		var min = 0.0f;

		// If our duration is zero or less, then coerce this value to avoid a division by zero
		//   or unexpected behavior with negative time.
		if (rOneIterationDuration <= 0.0f)
		{
			rOneIterationDuration = 1.0f;
		}

		// The sorted copy starts over from the collection order.
		_collection.Clear();
		_collection.AddRange(_snapshot);

		foreach (var currentFrame in _collection)
		{
			// TODO Uno: KeyTime is a struct in Uno and can't be missing (AG_E_RUNTIME_SB_BEGIN_INVALID_KEYTIME).
			var value = currentFrame.KeyTime;

			var newPercent = (float)value.TimeSpan.TotalSeconds / rOneIterationDuration;
			if (newPercent != currentFrame.Percent)
			{
				currentFrame.Percent = newPercent;
			}

			if (min > currentFrame.Percent)
			{
				m_isSorted = false;
			}

			min = currentFrame.Percent;
		}
	}
}
