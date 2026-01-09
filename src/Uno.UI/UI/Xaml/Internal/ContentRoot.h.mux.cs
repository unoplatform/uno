using System.Collections.Generic;
using Microsoft.UI.Content;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Internal;

namespace Uno.UI.Xaml.Core;

internal record struct KACollectionAndRefCountPair(ManagedWeakReference KeyboardAcceleratorCollectionWeak, int Count);

internal class VectorOfKACollectionAndRefCountPair : List<KACollectionAndRefCountPair>
{
}

partial class ContentRoot
{
	internal enum ChangeType
	{
		Size,
		RasterizationScale,
		IsVisible,
		Content
	};

	internal ContentIsland CompositionContent => _compositionContent;

	internal void AddPendingXamlRootChangedEvent(ContentRoot.ChangeType _/*ignored for now*/) => _hasPendingChangedEvent = true;

	private ContentIsland _compositionContent;
	private readonly SerialDisposable _compositionContentStateChangedToken = new();
	private readonly SerialDisposable _automationProviderRequestedToken = new();

	private VectorOfKACollectionAndRefCountPair _allLiveKeyboardAccelerators = new();

	private bool _hasPendingChangedEvent;
}
