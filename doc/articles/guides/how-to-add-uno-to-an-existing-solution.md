---
uid: Uno.Guides.AddUnoToExistingSolution
---

# Adding an Uno Platform Project to an existing solution

As of Uno 6.0, the Uno Platform solution templates [do not support being included in an existing solution](https://github.com/unoplatform/uno.templates/issues/641). This known issue can still be worked around by a few manual steps.

In order to add an Uno Platform project to an existing solution:

1. In a separate temporary folder, create a new project using the **Visual Studio** or `dotnet new` templates, using `MyProject` for its name.
1. Copy the generated individual project folders to your solution folder (`MyProject`, `MyProject.Server`, etc...)
1. For the `global.json` file:

    - If your solution already contains a `global.json` file, merge the `msbuild-sdks` section:

      ```json
      "msbuild-sdks": {
        "Uno.Sdk": "6.0.146"
      },
      ```

    - If not, copy the entire `global.json` and place it next to your existing sln.

1. For the `Directory.Build.Props` file, Uno Platform templates use [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) (CPM) by default, which might conflict with your existing solution.

    1. If your solution uses CPM, you'll only need to copy the `ImplicitUsings` and `Nullable` properties in your `MyProject.csproj`
    1. If your solution does not use CPM:

        - If you want to start using CPM, you can migrate to use it entirely and copy the contents of Uno's `Directory.Packages.props` to your own version of that file.
        - If you don't want to use CPM, search for each `PackageReference` in each of Uno's projects and add the versions found in Uno's `Directory.Packages.props`, if any, as it depends on the options chosen when creating the projects.

1. The other files `.vsconfig`, `.editorconfig` and `.gitignore` are completely optional and can be copied to your solution as needed.
1. Finally, manually add the projects to your solution using right-click on the root, click **Add**, then **Existing Project**
