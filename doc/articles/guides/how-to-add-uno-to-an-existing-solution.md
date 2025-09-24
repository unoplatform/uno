---
uid: Uno.Guides.AddUnoToExistingSolution
---

# Adding an Uno Platform Project to an existing solution

This guide shows you how to add a new Uno Platform project to an existing .NET solution. This is useful when you want to:

- Add cross-platform capabilities to an existing .NET solution
- Gradually migrate from other UI frameworks to Uno Platform
- Reuse shared components across an existing enterprise solution
- Integrate Uno Platform projects into established development workflows

## Prerequisites

Before starting, ensure you have:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later installed
- [Uno Platform templates](xref:Uno.GetStarted.dotnet-new) installed
- An existing .NET solution where you want to add the Uno Platform project

## Limitations

As of Uno 6.0, the Uno Platform solution templates [do not support being included in an existing solution](https://github.com/unoplatform/uno.templates/issues/641). This known issue can be worked around by following the manual steps below.

## Step-by-step Guide

To add an Uno Platform project to an existing solution:

### 1. Create the Uno Platform Project

In a separate temporary folder, create a new Uno Platform project:

**Using dotnet CLI:**
```bash
# Basic project with recommended settings
dotnet new unoapp -preset recommended -o MyProject

# Or a blank project for minimal setup
dotnet new unoapp -preset blank -o MyProject

# Advanced example with specific features
dotnet new unoapp -preset blank -theme material -presentation mvux -di -nav regions -toolkit -o MyProject
```

**Using Visual Studio 2022:**
1. Create a new project using the Uno Platform template
2. Choose your desired options in the project wizard
3. Use `MyProject` as the project name (replace with your actual project name)

> [!TIP]
> Use the [Live Wizard](https://aka.platform.uno/wizard) to explore all available template options and generate the exact `dotnet new` command for your needs.

### 2. Copy Project Files

From the generated temporary project, copy the following folders to your existing solution directory:

- `MyProject/` (main project folder)
- `MyProject.Server/` (if you included server project)
- Any other platform-specific projects based on your template selection

Place these folders at the same level as your other projects in the solution.

### 3. Handle the global.json File

The `global.json` file is required for Uno Platform projects as it specifies the Uno.Sdk version.

**If your solution already has a `global.json` file:**

Merge the `msbuild-sdks` section from the Uno project's `global.json`:

```json
{
  "sdk": {
    "version": "9.0.100"
  },
  "msbuild-sdks": {
    "Uno.Sdk": "6.0.146"
  }
}
```

**If your solution doesn't have a `global.json` file:**

Copy the entire `global.json` file from the temporary Uno project and place it next to your existing `.sln` file.

### 4. Handle NuGet Package Management

Uno Platform templates use [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) (CPM) by default, which might conflict with your existing solution.

**If your solution already uses CPM:**

1. Copy the relevant properties from Uno's `Directory.Build.props` to your existing file:
   ```xml
   <PropertyGroup>
     <ImplicitUsings>enable</ImplicitUsings>
     <Nullable>enable</Nullable>
   </PropertyGroup>
   ```

2. Merge package versions from Uno's `Directory.Packages.props` into your existing `Directory.Packages.props`.

**If your solution doesn't use CPM:**

Choose one of these approaches:

**Option A: Migrate to CPM (Recommended)**
1. Copy Uno's `Directory.Build.props` and `Directory.Packages.props` to your solution root
2. Update existing projects to use CPM by removing version attributes from `PackageReference` elements
3. Add all package versions to the `Directory.Packages.props` file

**Option B: Convert Uno project to use traditional PackageReference**
1. Copy the `ImplicitUsings` and `Nullable` properties to your `MyProject.csproj`:
   ```xml
   <PropertyGroup>
     <ImplicitUsings>enable</ImplicitUsings>
     <Nullable>enable</Nullable>
   </PropertyGroup>
   ```
2. Convert each `<PackageReference Include="PackageName" />` in the Uno project files to include version numbers found in `Directory.Packages.props`

### 5. Add Projects to Solution

Add the Uno Platform projects to your existing solution:

**Using Visual Studio:**
1. Right-click on the solution root in Solution Explorer
2. Select **Add** → **Existing Project**
3. Navigate to and select the `.csproj` files you copied
4. Repeat for all project files (main project, server project, etc.)

**Using dotnet CLI:**
```bash
dotnet sln YourSolution.sln add MyProject/MyProject.csproj
dotnet sln YourSolution.sln add MyProject.Server/MyProject.Server.csproj
# Add other projects as needed
```

### 6. Optional Configuration Files

The following files from the Uno project template are optional but recommended:

- **`.vsconfig`**: Specifies required Visual Studio workloads for the project
- **`.editorconfig`**: Defines code style and formatting rules
- **`.gitignore`**: Excludes build artifacts and temporary files from version control

Copy these files to your solution root if you want to use them, or merge their contents with your existing files.

## Verification

After completing the setup:

1. **Build the solution** to ensure all projects compile successfully:
   ```bash
   dotnet build YourSolution.sln
   ```

2. **Verify platform targeting** by checking that the Uno project can target your desired platforms

3. **Test the Uno project** by running it on different platforms to ensure proper integration

## Troubleshooting

### Build Errors

**"Assets file doesn't have a target for..."**
- Delete `bin/` and `obj/` folders in the Uno project
- Run `dotnet restore YourSolution.sln`

**"The Uno.Sdk could not be found"**
- Verify the `global.json` file is correctly positioned at the solution root
- Check that the Uno.Sdk version in `global.json` is valid

**Package version conflicts**
- If using CPM, ensure all package versions are defined in `Directory.Packages.props`
- If not using CPM, verify all PackageReference elements have version attributes

### Integration Issues

**Shared code conflicts**
- Rename namespaces if there are conflicts with existing projects
- Use project references carefully to avoid circular dependencies

**Platform-specific issues**
- Ensure your development environment supports the target platforms
- Install required workloads using `dotnet workload install`

## Next Steps

After successfully adding the Uno Platform project to your solution:

- Explore [sharing code between projects](xref:Uno.Development.SharingCode)
- Learn about [platform-specific implementations](xref:Uno.Development.PlatformSpecificCsharp)
- Consider [migrating existing UI components](xref:Uno.Guides.MigratingFromXamarinToNet6) to Uno Platform

## Related Guides

- [Adding Platforms to an Existing Uno Project](xref:Uno.Guides.AddAdditionalPlatforms) - For adding more target platforms to an existing Uno project
- [Getting Started](xref:Uno.GetStarted) - For creating new Uno Platform projects from scratch
- [Solution Structure](xref:Uno.GetStarted.Wizard) - Understanding the structure of Uno Platform projects
