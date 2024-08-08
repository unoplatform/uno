---
uid: Uno.UI.CommonIssues.UWP
---

# Issues related to UWP projects

## Error: Could not copy the file

Error message: "Error: Could not copy the file `<file_path>` because it was not found. `<ProjectName>`".

Example:

```console
Error: Could not copy the file C:\Users\username\.nuget\packages\uno.extensions.hosting.uwp\2.4.2\lib\uap10.0.18362\Uno.Extensions.Hosting.UWP\buildTransitive\Uno.Extensions.Hosting.UWP.props because it was not found. MyProject.Uwp
```

### Workaround for Dependent PRI Files Exclusion in NuGet Packaging

Follow the steps below to add the workaround to your project:

1. Open your UWP `.csproj` file.

2. Add the following targets to the file:

    ```xml
    <Target Name="AdjustGetPackagingOutput1" AfterTargets="GetMrtPackagingOutputs">
        <Message Importance="high" Text="Applying NuGet packaging workaround for dependent PRI files exclusion" />
        <ItemGroup>
            <_OtherPriFiles Include="@(PackagingOutputs)" Condition="'%(Extension)' == '.pri' and ('%(PackagingOutputs.ReferenceSourceTarget)' == 'ProjectReference' or '%(PackagingOutputs.NugetSourceType)'=='Package')" />
            <PackagingOutputs Remove="@(_OtherPriFiles)" />
        </ItemGroup>
    </Target>

    <Target Name="AdjustGetPackagingOutput2" BeforeTargets="AddPriPayloadFilesToCopyToOutputDirectoryItems">
        <Message Importance="high" Text="Applying NuGet packaging workaround for dependent PRI files exclusion" />
        <ItemGroup>
            <_OtherPriFiles1 Include="@(_ReferenceRelatedPaths)" Condition="'%(Extension)' == '.pri' and ('%(_ReferenceRelatedPaths.ReferenceSourceTarget)' == 'ProjectReference' or '%(_ReferenceRelatedPaths.NugetSourceType)'=='Package')" />
            <_ReferenceRelatedPaths Remove="@(_OtherPriFiles1)" />

            <_OtherPriFiles2 Include="@(ReferenceCopyLocalPaths)" Condition="'%(Extension)' == '.pri' and ('%(ReferenceCopyLocalPaths.ReferenceSourceTarget)' == 'ProjectReference' or '%(ReferenceCopyLocalPaths.NugetSourceType)'=='Package')" />
            <ReferenceCopyLocalPaths Remove="@(_OtherPriFiles2)" />
        </ItemGroup>
    </Target>
    ```
