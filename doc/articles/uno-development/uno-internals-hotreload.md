---
uid: Uno.Contributing.Internals.HotReload
---

# Hot Reload Phases

_This page details the internals of Hot Reload. To use Hot Reload, [head over here](xref:Uno.Features.HotReload)._

When a change is made to XAML or a C# code file, it's immediately picked up by compiler. However the updates aren't immediately sent to the app until the file is saved (assuming "Hot Reload on Save" is enabled) or the Hot Reload button is clicked in Visual Studio.

When Hot Reload is triggered, the changes, along with any associated metadata, is propagated to the running application.

There are two types of updates that can be sent:

- Incremental - these are deltas that are applied to existing types. For example a method can be modified, and the next time the method is invoked it will execute the new code
- Type Replacement - when a type has been attributed with the CreateNewOnMetadataUpdate attribute, instead of changes being sent as deltas, a whole new type is added. So for example if a change is made to MainPage, a new type, MainPage#1, is created and is available within the executing application.

After Hot Reload has propagated changes to the running application, it will look for any types that have been registered using the MetadataUpdateHandler attribute. If a type is found, it will attempt to run the UpdateApplication static method, passing in the types that have been modified. In the case of incremental updates, the types will be original types found within the application but they'll have been updated with the deltas that have been sent. In the case of type replacement updates, the types will contain the newly created types. The meta data on these types can be interrogated to determine the original type that they're replacing.

Uno core already registers a class with the MetadataUpdateHandler and has an UpdateApplication method that gets invoked when Hot Reload is triggered. This application does two things:

- for Type Replacement updates, it adds, or updates, values in mapping dictionaries it manages so that it's possible to translate between an original type and it's (current) replacement, and from a replacement type, back to the original type.
- it triggers an update to the UI (a UI Update) which walks the visual tree looking for elements where the type has been replaced. If an element is of a type that has been replace, a new element is created and used to replace the original element in the tree.

## Intercepting the UI Update

The main extensibility point for developers wanting to integrate with Hot Reload for Uno Platform applications is via the UI Update. The UI Update is the phase of Hot Reload where the visual tree is traversed and elements are updated according to the updated type information.

To intercept the UI Update the first thing to do is to create a static class with static methods that will be invoked at different points in the UI Update. The static class needs to be registered using the ElementMetadataUpdateHandler attribute.

In this example, the FrameUpdateHandler is registered as a handler for the Frame class. As the visual tree is traversed, when a Frame is encountered the appropriate methods on the FrameUpdateHandler will be invoked.

```csharp
[assembly:ElementMetadataUpdateHandler(typeof(Frame), typeof(FrameUpdateHandler))]
```

In this example, the VisualTreeHandler is registered as a handler without specifying a particular element type. Only the methods on the VisualTreeHandler that aren't specific to individual elements will be invoked, for example the BeforeVisualTreeUpdate and AfterVisualTreeUpdate method.

The following methods (at least one) can be implemented by the static class registered using the ElementMetadataUpdateHandler:

```csharp
static void BeforeVisualTreeUpdate(Type[]? updatedTypes);

static void AfterVisualTreeUpdate(Type[]? updatedTypes);

static void ReloadCompleted(Type[]? updatedTypes, bool uiUpdated);

static void ElementUpdate(FrameworkElement, Type[]?);

static void BeforeElementReplaced(FrameworkElement, FrameworkElement, Type[]?);

static void AfterElementReplaced(FrameworkElement, FrameworkElement, Type[]?);
```

### Content that is not in the visual tree

The UI Update only walks materialized elements, so content assigned to a `ContentControl` whose subtree never had a layout pass is invisible to it — the template never applied, so the content is not a visual child. The `ContentControl` and `Frame` element-update handlers re-create such content themselves, from `ElementUpdate`.

`BeforeElementReplaced` / `AfterElementReplaced` are **not** raised for those swaps, and they are not counted in the operation's replaced-element total — those notifications belong to the elements the walk owns. A handler that tracks a content instance should reconcile in `AfterVisualTreeUpdate`, which runs once after the whole phase. `CaptureState` / `RestoreState` do not run either: the content was never materialized, so there is no visual state to preserve.

## Pausing / Resuming UI Update

Pausing and resuming UI Update is done by calling

`TypeMappings.Pause()` and `TypeMappings.Resume()`

Note that pausing UI Updates doesn't stop the Hot Reload process. It only prevents the UI Update from running until UI Updates are resumed.

## Waiting for Hot Reload to be applied

When code needs to continue only after a file change has reached the running application, use `ClientHotReloadProcessor.UpdateRequest` with `WaitForHotReload` set to `true` and await `UpdateFileAsync`. The task completes after server processing and local Hot Reload processing, including the UI update.

```csharp
using System;
using System.Linq;
using System.Threading;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload;

var hotReload = RemoteControlClient.Instance?.Processors
    .OfType<ClientHotReloadProcessor>()
    .SingleOrDefault()
    ?? throw new InvalidOperationException("Hot Reload is unavailable.");

var request = new ClientHotReloadProcessor.UpdateRequest(
    "Pages/MainPage.xaml",
    "Old text",
    "New text",
    WaitForHotReload: true);

await hotReload.UpdateFileAsync(request, CancellationToken.None);
```

`FilePath` is relative to the solution root. The Hot Reload processor must have published its initial status before the request is sent.

Hot Reload normally triggers the UI update automatically. When the updated types are already available and the UI must be reapplied explicitly, call `UIUpdate.ForceRefresh(updatedTypes, CancellationToken.None)`. It returns after the UI-thread update pass completes and bypasses active `UIUpdate.Pause` handles.
