# Specific considerations for WinAppSDK 

## Unpackaged application support
By default the **Uno Platform App** Visual Studio template creates a packaged application. If you want to add unpackaged support, you'll need to do the following:
- Add a new entry in the launchSettings.json file:
    ```json
    {
        "profiles": {
            "UnoWinUIQuickStart.Windows (Package)": {
                "commandName": "MsixPackage"
            },
            "UnoWinUIQuickStart.Windows (Unpackaged)": {
                "commandName": "Project"
            }
        }
    }
    ```
- Add this new set of properties in your `.Windows` csproj:
    ```xml
  	<PropertyGroup>
		<!-- Bundles the WinAppSDK binaries (Uncomment for unpackaged builds) -->
		<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
		<!-- This bundles the .NET Core libraries (Uncomment for packaged builds)  -->
		<!-- <SelfContained>true</SelfContained> -->
	</PropertyGroup>
    ```
    You will need to adjust which property is enabled based on the deployment target that you are choosing. Both properties are not supported (as of WinAppSDK 1.0.3).
