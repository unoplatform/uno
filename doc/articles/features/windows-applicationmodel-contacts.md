---
uid: Uno.Features.WAMContacts
---

# Contacts

> [!TIP]
> This article covers Uno-specific information for `Windows.ApplicationModel.Contacts` namespace. For a full description of the feature and instructions on using it, see [Windows.ApplicationModel.Contacts Namespace](https://learn.microsoft.com/uwp/api/windows.applicationmodel.contacts).

* The `Windows.ApplicationModel.Contacts.ContactPicker` namespace provides classes for picking contacts from the OS contact store.

## `ContactPicker`

The `ContactPicker` class allows picking contacts from the OS contact store.

To check whether the class is supported on the target platform, use `IsSupportedAsync()` method:

```csharp
if (await ContactPicker.IsSupportedAsync())
{
    // you can use ContactPicker
}
```

To pick contacts, call either the `PickContactAsync()` or `PickContactsAsync()` method. Uno Platform supports picking multiple contacts on Windows, iOS, WebAssembly, and Tizen. On Android, we currently support picking a single contact only (for simplicity, the `PickContactsAsync()` method can still be called, but will still let the user pick only a single contact).

### Platform-specific

#### Android

Your app must declare `android.permission.READ_CONTACTS` permission:

```csharp
[assembly: UsesPermission("android.permission.READ_CONTACTS")]
```

#### Tizen

Your app must declare `http://tizen.org/privilege/contact.read` and `http://tizen.org/privilege/appmanager.launch` permissions in the app manifest:

```xml
<privileges>
    <privilege>http://tizen.org/privilege/contact.read</privilege>
    <privilege>http://tizen.org/privilege/appmanager.launch</privilege>
</privileges>
```
