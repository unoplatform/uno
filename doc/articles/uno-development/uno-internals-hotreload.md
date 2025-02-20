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

## Pausing / Resuming UI Update

Pausing and resuming UI Update is done by calling

`TypeMappings.Pause()` and `TypeMappings.Resume()`

Note that pausing UI Updates doesn't stop the Hot Reload process. It only prevents the UI Update from running until UI Updates are resumed.

<!---
## Waiting for Hot Reload to be applied

// TODO: Give an example of how to await UI Updates (eg https://github.com/unoplatform/uno/blob/0340cc1394994cdbd525d61de611a0531c38bcc7/src/Uno.UI.RuntimeTests/Tests/HotReload/Frame/HRApp/Tests/Given_Frame.cs#L9-L37)
-->
