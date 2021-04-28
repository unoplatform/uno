# Hosting Uno Platform WebAssembly apps on Azure Static Web Apps

The Azure team has released a new service named [Azure Static Web Apps in Preview](https://docs.microsoft.com/en-us/azure/static-web-apps/overview).

This integration allows the publication of GitHub and Azure DevOps Git repositories to a service specialized for static web apps. It uses GitHub Actions or Azure DevOps to build and publish the application. The integration also supports the creation of temporary environments for Pull Requests, giving the ability to perform validations while a Pull Request is opened.

You can review [the Azure documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/configuration) to configure the server-side behavior of the application.

## Publishing a WebAssembly app
Here is how to publish an app from GitHub, using Uno Platform:
-	In a new repository, create a Uno Platform app using the following command:
    ```
    cd src
    dotnet new -i Uno.ProjectTemplates.Dotnet
    dotnet new unoapp -o MyApp
    ```
-	If the <TargetFramework> value in the `MyApp.Wasm.csproj` is not `net5.0`, [follow the upgrading steps provided here](https://github.com/unoplatform/uno/blob/master/doc/articles/migrating-from-previous-releases.md#migrating-webassembly-projects-to-net-5).
-	If in the `MyApp.Wasm\wwwroot`, you find a `web.config` file, delete it. This will enable brotli compression in Azure Static Web Apps.
-	Search for [Static Web Apps (Preview)](https://portal.azure.com/#create/Microsoft.StaticApp) in the Azure Portal
-	Fill the required fields in the creation form:

    ![visual-studio-installer-web](../Assets/aswa-create.png)
-	Select your repository using the **Sign in with GitHub** button
-	Select your organization, repository, and branch
-	In the build presets, select **Custom**, and keep the default values

    ![visual-studio-installer-web](../Assets/aswa-settings.png)

-	The click on **Review+Create** button. Azure will automatically create a new **GitHub Actions Workflow** in your repository. The workflow will automatically be started and will fail for this first run, as some parameters need to be adjusted.
-	In your repository’s newly added github workflow (in `.github/workflows`), add the following two steps, after the checkout step:

    ```yaml
    - name: Setup dotnet
    uses: actions/setup-dotnet@v1.7.2
    with:
        dotnet-version: '5.0.103'
            
    - run: |
        cd src/MyApp.Wasm
        dotnet build -c Release
    ```

-	In the Deploy step that was automatically added, change the `app_location` parameter to the following:
    ```yaml
    app_location: "src/MyApp.Wasm/bin/Release/net5.0/dist"
    ```
-	Once changed, the application will be built and deployed on your Azure Static Web App instance.

## Configuring Deep Linking with Fallback Routes

Azure Static Web Apps provides the [ability to configure many parts](https://docs.microsoft.com/en-us/azure/static-web-apps/configuration) of the application's behavior, and one particular feature is very useful to enable deep linking in applications.

You will need to [enable fallback routes](https://docs.microsoft.com/en-us/azure/static-web-apps/configuration#fallback-routes) in your application this way:
- In the `wwwroot` folder, [create a file named `staticwebapp.config.json`](https://docs.microsoft.com/en-us/azure/static-web-apps/configuration#file-location), with the following content:
  ```json
  {
    "navigationFallback": {
      "rewrite": "/index.html",
      "exclude": ["/package_*"]
    }
  }
  ```
- In your application, You'll need to get the active url of the browser this way:
  ```csharp
  if(System.Uri.TryCreate(
      Uno.Foundation.WebAssemblyRuntime.InvokeJS("document.location.search"),
      UriKind.RelativeOrAbsolute,
      out var browserUrl))
  {
      var appPath = browserUrl.GetComponents(System.UriComponents.Path, UriFormat.Unescaped);
      // TODO: Process the path in your application
  }
  ```
