#### Application ID

This option sets the iOS/macOS application [Bundle ID](https://developer.apple.com/documentation/appstoreconnectapi/bundle_ids) to be used in the App Store.  

The Application ID is also used as the Win App SDK
Application ID setting for the app.

```dotnetcli
dotnet new unoapp -id com.mycompany.myapp
```

#### Publisher

This option sets the Publisher name in the Win App SDK settings.

```dotnetcli
dotnet new unoapp -pub 'O=My Company, C=US'
```
