// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TimelineCollection.cpp, TimingCollection.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

public sealed partial class TimelineCollection : DependencyObjectCollection<Timeline>, IList<Timeline>, IEnumerable<Timeline>
{
	private readonly Timeline? _timingOwner;

	public TimelineCollection()
	{
	}

	internal TimelineCollection(Timeline owner, bool isAutoPropertyInheritanceEnabled)
	{
		_timingOwner = owner;
		IsAutoPropertyInheritanceEnabled = isAutoPropertyInheritanceEnabled;
		this.SetParent(owner);
	}

	public new void Add(Timeline element) => base.Add(element);

	private protected override void OnModifying() => _timingOwner?.CheckCanBeModified();

	// CTimingCollection::RemoveAt ignores the removal while the storyboard runs.
	private protected override bool CanRemoveItems() => _timingOwner?.CanBeModified() ?? true;

	private protected override void OnAdded(Timeline d)
	{
		base.OnAdded(d);
		d.SetTimingParent(_timingOwner);
	}

	private protected override void OnRemoved(Timeline d)
	{
		base.OnRemoved(d);
		d.SetTimingParent(null);
	}
}
