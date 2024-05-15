using Microsoft.UI.Content;
using Uno.Disposables;

namespace Uno.UI.Xaml.Core;

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

	private bool _hasPendingChangedEvent;
}
