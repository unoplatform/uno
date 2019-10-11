# PasswordVault

The `PasswordVault` is a credentials manager that is persisted using a secured storage.
More info about its usage can be found here https://docs.microsoft.com/en-us/uwp/api/windows.security.credentials.passwordvault

## Supported platforms

|                      | Android | iOS     | MacOS   | Wasm    |
| -------------------- | ------- | ------- | ------- | ------- |
| `PasswordVault`      | Yes     | Yes     | No      | No      |
| `PasswordCredential` | Partial | Partial | Partial | Partial |

## Security

The `PasswordVault` is designed to be safe place to store user's credentials and tokens.
It's backed by the hardware encryption mechanism of each platform, which provides a high level of security.
However, the `PasswordVault` does **not** offer any memory security feature.

### Android
The implementation uses the __AndroidKeyStore__ which was introduced with API 18 (4.3).
The `KeyStore` is used to generate a symmetric key which is then used to encrypt and decrypt a file persisted in the application directory.
The key is managed by the `KeyStore` itself, which usually uses hardware component to persist it. The key is not even accessible to the application.

More info: https://developer.android.com/reference/java/security/KeyStore

### iOS
The `PasswordVault` is directly stored in the iOS `KeyChain` which is the recommended way to store secrets on iOS devices.
It's backed by hardware components which ensure that the data is almost impossible to retrieve if not granted.

More info: https://developer.apple.com/documentation/security/certificate_key_and_trust_services/keys/storing_keys_in_the_keychain

### MacOS
This platform was not evaluated.

### Wasm
There is no way to persist a secured data in a Web browser. Even if we generate a key to encrypt it,
there is no safe place to store this key except by relying on server components, which broke the offline support (and Progressive Web App).
So currently we preferred to **not** implement the `PasswordVault`. It will throw a `NotSupportedException` when you try to create a new instance.

## PasswordCredential
This class is implemented, however it never hides the password like the UWP does.
This means that the[`RetrievePassword`](https://docs.microsoft.com/en-us/uwp/api/windows.security.credentials.passwordcredential.retrievepassword#Windows_Security_Credentials_PasswordCredential_RetrievePassword) does nothing,
but we recommend to still use it in order to ensure cross-platform compatibility.

The [`Properties`](https://docs.microsoft.com/en-us/uwp/api/windows.security.credentials.passwordcredential.properties#Windows_Security_Credentials_PasswordCredential_Properties) is not implemented.
