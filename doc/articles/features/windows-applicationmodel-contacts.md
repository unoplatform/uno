# Uno Support for Windows.ApplicationModel.Contacts

## `ContactPicker`

The `ContactPicker` class allows picking contacts from the OS contact store.
 
To check whether the class is supported on the target platform, use `IsSupportedAsync()` method:

```
if (await ContactPicker.IsSupportedAsync())
{
    // you can use ContactPicker    
}
```

To pick contacts call either the `PickContactAsync()` or `PickContactsAsync()` method. Uno Platform supports picking multiple contacts on Windows, iOS, WebAssembly and Tizen. On Android we currently support picking a single contact only (for simplicity, the `PickContactsAsync()` method can still be called, but will still let the user pick only a single contact).

### Platfrom-specific

#### Android

Your app must declare `android.permission.READ_CONTACTS` permission:

```
[assembly: UsesPermission("android.permission.READ_CONTACTS")]
```

#### Tizen

Your app must declare `http://tizen.org/privilege/contact.read` and `http://tizen.org/privilege/appmanager.launch` permissions in the app manifest:

```
<privileges>
    <privilege>http://tizen.org/privilege/contact.read</privilege>
    <privilege>http://tizen.org/privilege/appmanager.launch</privilege>
</privileges>
```