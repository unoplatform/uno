# SourceGenerators folder

1. The `Uno.UI.SourceGenerator` project is referenced by `XamlGenerationTests`.
2. During compilation XAML within `XamlGenerationTests` will be code generated. It's worth looking at `XamlGenerationTests/XamlGenerationTests.csproj` for insights on how this works, specifically `TargetFrameworksCI` and the `PackageReferences`.
3. If this fails, then the CI build will fail.

Thus `XamlGenerationTests` is the test suite for `Uno.UI.SourceGenerator` and is typically [where tests should be added](https://github.com/unoplatform/uno/pull/1278#discussion_r305829755).