// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference KeyFrameCollection.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// The time-sorted key frames of a key frame animation (CKeyFrameCollection).
/// </summary>
/// <remarks>
/// TODO Uno specific: WinUI's typed key frame collections derive from CKeyFrameCollection and sort themselves in place.
/// Uno's public typed collections are plain DependencyObjectCollections, so the sorting state lives here, over a copy,
/// which leaves the public KeyFrames order untouched.
/// </remarks>
internal sealed partial class KeyFrameCollection
{
	private readonly Func<IEnumerable<IKeyFrame>?> _source;

	// The source items in collection order, and the same items as sorted by GetSortedCollection.
	private readonly List<IKeyFrame> _snapshot = new();
	private readonly List<IKeyFrame> _collection = new();

	private bool m_isSorted;

	public KeyFrameCollection(Func<IEnumerable<IKeyFrame>?> source) => _source = source;

	private int GetCount() => _collection.Count;

	// Takes the items of the source collection again when they changed since the last snapshot.
	private void EnsureSnapshot()
	{
		var source = _source();
		var index = 0;
		var isSame = true;

		if (source is not null)
		{
			foreach (var keyFrame in source)
			{
				if (index >= _snapshot.Count || !ReferenceEquals(_snapshot[index], keyFrame))
				{
					isSame = false;
					break;
				}

				index++;
			}
		}

		if (isSame && index == _snapshot.Count)
		{
			return;
		}

		_snapshot.Clear();
		if (source is not null)
		{
			_snapshot.AddRange(source);
		}

		_collection.Clear();
		_collection.AddRange(_snapshot);
		m_isSorted = false;
	}
}
