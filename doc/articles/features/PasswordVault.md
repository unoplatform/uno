---
uid: Uno.Features.PasswordVault
---

# Credentials storage

> [!TIP]
> This article covers Uno-specific information for `Windows.Security.Credentials.PasswordVault` API. For a full description of the feature and instructions on using it, see [PasswordVault Class](https://learn.microsoft.com/uwp/api/windows.security.credentials.passwordvault).

* The `PasswordVault` is a credentials manager that is persisted using a secured storage.
* `PasswordCredential` is used to manipulate passwords in the vault.

## Supported features

| Feature              | Windows (WinAppSDK) | Windows (Skia) | Android | iOS | macOS (Skia) | Linux (Skia) | WebAssembly | Other Skia |
|----------------------|---------------------|----------------|---------|-----|--------------|--------------|-------------|------------|
| `PasswordVault`      | ✔                   | ✔              | ✔       | ✔   | ✔            | ✔            | ✖           | ✖          |
| `PasswordCredential` | ✔                   | Partial        | Partial | Partial | Partial  | Partial      | Type only   | Type only  |

"Other Skia" covers Skia hosts without a registered secure-storage extension, such as
the WPF/XAML Islands host.

## `PasswordVault`

The `PasswordVault` is designed to store credentials and tokens in the
operating system's credential store. It does **not** provide memory protection
after a password has been returned to application code.

Below see the implementation information for each platform:

### [**Android**](#tab/android)

The implementation uses the `AndroidKeyStore` which was introduced with API 18 (4.3).
The `KeyStore` is used to generate a symmetric key which is then used to encrypt and decrypt a file persisted in the application directory.
The key is managed by the `KeyStore` itself, which usually uses the hardware component to persist it. The key is not even accessible to the application.

For more information, see [KeyStore](https://developer.android.com/reference/java/security/KeyStore).

### [**iOS**](#tab/iOS)

The `PasswordVault` is directly stored in the iOS `KeyChain` which is the recommended way to store secrets on iOS devices.
It's backed by hardware components that ensure that the data is almost impossible to retrieve if not granted.

For more information, see [Storing Keys in the Keychain](https://developer.apple.com/documentation/security/certificate_key_and_trust_services/keys/storing_keys_in_the_keychain).

### [**Windows (Skia)**](#tab/windows-skia)

The Windows Skia implementation protects the serialized vault with DPAPI
(`CryptProtectData` in the current-user scope, with `CRYPTPROTECT_UI_FORBIDDEN`)
and stores the ciphertext in the application's local folder. The protection key
belongs to the Windows user profile and is never available to the application,
so another Windows user cannot read the vault.

Entropy derived from the application identity is mixed into the protection, which
means changing that identity makes an existing vault unreadable.

> [!NOTE]
> Windows Credential Manager is not used as the backing store: its
> `CRED_MAX_CREDENTIAL_BLOB_SIZE` limit of 2560 bytes cannot hold a serialized
> vault, while the Windows `PasswordVault` accepts individual passwords well past
> that size.

### [**macOS (Skia)**](#tab/macOS)

The macOS Skia implementation stores the serialized vault in the current
user's Keychain by using the Security framework. The item is a generic password
whose service is `uno_passwordvault` and whose account is the application
package name.

### [**Linux (Skia)**](#tab/Linux)

The Linux implementation stores the serialized vault in the user's Secret
Service collection through `libsecret`. The target computer must provide
`libsecret-1.so.0`, a D-Bus user session, and an
`org.freedesktop.secrets` provider such as GNOME Keyring or KeePassXC.

`PasswordVault` fails instead of falling back to a file-based key when the
native credential store is missing, locked, or unavailable.

### [**WebAssembly**](#tab/WebAssembly)

There is no way to persist a secured data in a Web browser. Even if we generate a key to encrypt it,
there is no safe place to store this key except by relying on server components, which broke the offline support (and Progressive Web App).
So currently we preferred to **not** implement the `PasswordVault`. It will throw a `NotSupportedException` when you try to create a new instance.

---

## PasswordCredential

This class is implemented, however, it never hides the password like the WinUI does.
This means that the [`RetrievePassword` method](https://learn.microsoft.com/uwp/api/windows.security.credentials.passwordcredential.retrievepassword#Windows_Security_Credentials_PasswordCredential_RetrievePassword) does nothing, but we recommend still using it in order to ensure cross-platform compatibility.

The [`Properties` property](https://learn.microsoft.com/uwp/api/windows.security.credentials.passwordcredential.properties#Windows_Security_Credentials_PasswordCredential_Properties) is not implemented.

## Behavior parity

The following behaviors match the Windows implementation on every platform Uno
implements:

| Behavior | Result |
|----------|--------|
| `Retrieve(resource, userName)` | Case **insensitive** on both arguments, and returns the password |
| `FindAllByResource` / `FindAllByUserName` | Case **sensitive** |
| `Add` for an existing resource/user pair | Replaces the entry, including the stored casing of the user name |
| `Remove` | Matches on resource and user name only; the password is ignored |
| `Remove` of an absent credential | Throws |
| Missing item | Throws with `HResult` `0x80070490` (`HRESULT_FROM_WIN32(ERROR_NOT_FOUND)`) |
| `RetrieveAll` on an empty vault | Returns an empty list |
| `null` or empty arguments | Throw `ArgumentException` |
| Reads | Always observe writes made by other vault instances and processes |

Known differences:

* `RetrieveAll`, `FindAllByResource` and `FindAllByUserName` return credentials
  whose `Password` is already populated. Windows leaves it empty until
  `RetrievePassword` is called. Call `RetrievePassword` anyway so the code stays
  portable.
* Enumeration order is unspecified on Windows and is insertion order in Uno. Do
  not depend on it in either case.
* `PasswordCredential` accepts empty strings for `Resource`, `UserName` and
  `Password`; Windows rejects them in the constructor.

## Limitations

* Windows Skia, macOS and Linux store the serialized vault as one native item.
  Uno serializes `Add` and `Remove` operations across processes that share the
  same application-data identity. Modifying the native item outside
  `PasswordVault` is unsupported.
* The package identity scopes the native item. Changing the package or
  application assembly identity creates a separate vault.
* Linux requires `libsecret-1.so.0`, a D-Bus user session, and a Secret Service
  provider. Headless sessions commonly do not provide these.
* Uno does not fall back to reversible file encryption when a native credential
  store is unavailable.
* `PasswordVault` remains unsupported on WebAssembly and Skia hosts without a
  registered secure-storage extension. `PasswordCredential` can still be
  constructed there, but it cannot be persisted through `PasswordVault`.
* `PasswordCredential.Password` remains visible after retrieval, and
  `PasswordCredential.Properties` is not implemented.

## Sample

### Storing a credential

```csharp
var vault = new Windows.Security.Credentials.PasswordVault();
vault.Add(new Windows.Security.Credentials.PasswordCredential(
    "My App", username, password));
```

### Retrieving a credential

```csharp
var vault = new Windows.Security.Credentials.PasswordVault();
var credential = vault.Retrieve("My App", userName);
credential.RetrievePassword();
var password = credential.Password;
```
