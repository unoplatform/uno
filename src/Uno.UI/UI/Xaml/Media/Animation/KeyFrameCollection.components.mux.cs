// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/KeyFrameCollection.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class KeyFrameCollection
{
	// Returns the flattened sorted collection to the caller.
	internal IReadOnlyList<IKeyFrame> GetSortedCollection()
	{
		var min = 0;
		var leftValue = 0.0f;
		var rightValue = 0.0f;

		EnsureSnapshot();

		// Note: Aside from the key time checks, this thing can be replaced with std::stable_sort.
		if (!m_isSorted)
		{
			for (var i = 0; i < GetCount(); i++)
			{
				min = i;
				for (var j = i + 1; j < GetCount(); j++)
				{
					// TODO Uno: KeyTime is a struct in Uno: there is no non-TimeSpan KeyTime to fail on.

					// Sort on the TimeSpan values
					leftValue = (float)_collection[min].KeyTime.TimeSpan.TotalSeconds;
					rightValue = (float)_collection[j].KeyTime.TimeSpan.TotalSeconds;

					if (leftValue > rightValue)
					{
						min = j;
					}
				}

				if (min != i)
				{
					(_collection[min], _collection[i]) = (_collection[i], _collection[min]);
				}
			}

			m_isSorted = true;
		}

		return _collection;
	}

#if !HAS_UNO
	// TODO Uno: NOT PORTED - AddCompositionKeyFrames: composition animations.
#endif
}
