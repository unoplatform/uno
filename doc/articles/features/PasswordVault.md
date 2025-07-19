---
uid: Uno.Features.PasswordVault
---

# Credentials storage

> [!TIP]
> This article covers Uno-specific information for `Windows.Security.Credentials.PasswordVault` API. For a full description of the feature and instructions on using it, see [PasswordVault Class](https://learn.microsoft.com/uwp/api/windows.security.credentials.passwordvault).

* The `PasswordVault` is a credentials manager that is persisted using a secured storage.
* `PasswordCredential` is used to manipulate passwords in the vault.

## Supported features

| Feature              | Windows | Android | iOS     | Web (WASM) | Linux (Skia) | Win 7 (Skia) | Tizen |
|----------------------|---------|---------|---------|--------------------------|---------------|-------|
| `PasswordVault`      | ✔       | ✔      | ✔       | ✖         | ✖            | ✖           | ✖     |
| `PasswordCredential` | ✔       | Partial | Partial | Partial    | ✖            | ✖          | ✖     |

## `PasswordVault`

The `PasswordVault` is designed to be a safe place to store the user's credentials and tokens.
It's backed by the hardware encryption mechanism of each platform, which provides a high level of security.
However, the `PasswordVault` does **not** offer any memory security feature.

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

### [**WebAssembly**](#tab/WebAssembly)

There is no way to persist a secured data in a Web browser. Even if we generate a key to encrypt it,
there is no safe place to store this key except by relying on server components, which broke the offline support (and Progressive Web App).
So currently we preferred to **not** implement the `PasswordVault`. It will throw a `NotSupportedException` when you try to create a new instance.

---

## PasswordCredential

This class is implemented, however, it never hides the password like the WinUI does.
This means that the [`RetrievePassword` method](https://learn.microsoft.com/uwp/api/windows.security.credentials.passwordcredential.retrievepassword#Windows_Security_Credentials_PasswordCredential_RetrievePassword) does nothing, but we recommend still using it in order to ensure cross-platform compatibility.

The [`Properties` property](https://learn.microsoft.com/uwp/api/windows.security.credentials.passwordcredential.properties#Windows_Security_Credentials_PasswordCredential_Properties) is not implemented.

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
