#nullable enable

using Uno.UI.Xaml.Islands;
using Windows.UI.Xaml.Input;

/*
    +------------------------------------------------------------------------------------+
    |                                      +---------------+                             |
    |                                      | CCoreServices |                             |
    |                                      +-------+-------+                             |
    |                                              |                                     |
    |                                              |                                     |
    |                                        +-----v----------------+                    |
    |                              +---------+ContentRootCoordinator|                    |
    |                              |         +-----------------+----+                    |
    |                              |                           |                         |
    |                              |                           |                         |
    |                       +------v-----+            +--------v---+                     |
    |                       |CContentRoot|            |CContentRoot|"Main Content Root"  |
    |                       +--+---------+            +------+-----+                     |
    |                          |                             |                           |
    |      +--------------+    |                             |      +---------------+    |
    |      | CFocusManager<----+                             +------> CFocusManager |    |
    |      +--------------+    |                             |      +---------------+    |
    |      +--------------+    |                             |      +--------------+     |
    |      | CInputManager<----+                             +------> CInputManager|     |
    |      +--------------+    |                             |      +--------------+     |
    |      +------------+      |                             |      +-----------+        |
    |      | VisualTree <------+                             +------> VisualTree|        |
    |      +------------+                                           +-----------+        |
    |                                                                                    |
    |                                                                                    |
    +------------------------------------------------------------------------------------+
*/

namespace Uno.UI.Xaml.Core;

internal enum ContentRootType
{
	CoreWindow,
	XamlIsland
}

partial class ContentRoot
{
	internal enum IslandType
	{
		Invalid,
		Raw,
		DesktopWindowContentBridge
	}

	internal enum LifetimeState
	{
		Normal,
		PreparingToClose,
		Closing,
		Closed
	}

	internal ContentRootType Type => _type;

	internal IslandType GetIslandType() => _islandType;

	/// <summary>
	/// Represents the visual tree associated with this content root.
	/// </summary>
	internal VisualTree VisualTree => _visualTree;

	// Support for the public XamlRootChanged event
	internal enum ChangeType
	{
		Size,
		RasterizationScale,
		IsVisible,
		Content
	}

	internal void AddPendingXamlRootChangedEvent(ChangeType _/*ignored for now*/) => _hasPendingChangedEvent = true;

#pragma warning disable CS0649 // Field is not used yet
	private LifetimeState _lifetimeState;
#pragma warning restore CS0649

	private ContentRootType _type;
	private IslandType _islandType;

	private readonly CoreServices _coreServices;
	private VisualTree _visualTree;
	private FocusManager _focusManager;

	//Microsoft::WRL::ComPtr<ixp::IContentIsland> _compositionContent;
	//EventRegistrationToken _compositionContentStateChangedToken = { };
	//EventRegistrationToken _automationProviderRequestedToken = { };

	private XamlIsland? _xamlIslandRoot;

	private InputManager _inputManager;

	private AccessKeyExport _akExport;

	//VectorOfKACollectionAndRefCountPair _allLiveKeyboardAccelerators;

	private FocusAdapter _focusAdapter;
	private ContentRootEventListener _contentRootEventListener;

	private bool _hasPendingChangedEvent;

	//private bool _isInputActive;
}
