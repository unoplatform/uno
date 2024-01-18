---
uid: Uno.Contributing.UpdatingDependencies
---

# Guidelines for updating dependencies

We use Dependabot to notify the team of any updates to dependencies. Once a week the robot will scan our dependencies and raise a pull-request if a new version is found. If an existing open pull-request is found for a dependency it will be closed and replaced with a new pull-request. The behavior of the robot is [controlled by this configuration file](https://github.com/unoplatform/Uno/blob/master/.dependabot/config.yml).

## internal dependencies

These dependencies don't change the public API surface and are typically safe to merge and we could potentially [configure mergify to automatically merge them if CI passes](https://medium.com/mergify/merging-bots-pull-requests-automatically-548ed0b4a424):

- BenchmarkDotNet
- [FluentAssertions](https://github.com/unoplatform/uno/pull/1196)
- [NUnit3TestAdapter](https://github.com/unoplatform/uno/pull/1455)
- [NUnit.Runners](https://github.com/unoplatform/uno/pull/1122)
- [Microsoft.AppCenter](https://github.com/unoplatform/uno/pull/1175)
- [Microsoft.SourceLink.GitHub](https://github.com/unoplatform/uno/pull/1204)
- [Microsoft.NET.Test.Sdk](https://github.com/unoplatform/uno/pull/1203)
- [MSTest.TestAdapter](https://github.com/unoplatform/uno/pull/1126)
- [MSTest.TestFramework](https://github.com/unoplatform/uno/pull/1128)
- Moq
- [Xamarin.Build.Download](https://github.com/unoplatform/uno/pull/1123)

These dependencies require manual adjustments before merging:

- [docfx.console](https://github.com/unoplatform/Uno/pull/1082/commits/c222caf8c23b35e19f6b33cd624cbfa714250bfe)
- `Microsoft.CodeAnalysis.*`. Those dependencies need to be aligned with the source generation task package, for which the dependency cannot be be explicitly provided.
- `Xamarin.GooglePlayServices.*`. Those dependencies are added per TargetFramework (Android SDK version), not updated.

## public dependencies

Updating these dependencies will require consumers to upgrade their dependencies and as such need consideration on a case by case basis is required before merging:

- [System.Reactive](https://github.com/unoplatform/uno/pull/1170#pullrequestreview-256670600). Currently only used in the `WpfHost` which eventually will be deprecated.
- [Microsoft.TypeScript.*](https://github.com/unoplatform/uno/pull/1129) child packages needs to be aligned with the other `Microsoft.TypeScript.*` packages.

## additional care required

These dependencies require care and human testing:

- [CommonServiceLocator](https://github.com/unoplatform/uno/pull/1174#issuecomment-507659717). This specific dependency needs to be removed from Uno.
- [`cef.redist.x86`](https://github.com/unoplatform/uno/pull/1173#issuecomment-507662267) needs to be kept in alignment with `CefSharp.Wpf`
- [CefSharp.Wpf](https://github.com/unoplatform/uno/pull/1173#issuecomment-507662267) needs to be kept in alignment with `cef.redist.x86`
- [Microsoft.CodeAnalysis.*](https://github.com/unoplatform/uno/pull/1169) children packages needs to be aligned with the other `Microsoft.CodeAnalysis` packages.
- [Microsoft.Build.*](https://github.com/unoplatform/uno/pull/1169) children packages needs to be aligned with the other `Microsoft.Build` packages, and need to be aligned with `Uno.SourceGenerationTasks` package features.
- [Microsoft.Extensions.Logging.*](https://github.com/unoplatform/uno/pull/1108/files#r300432589) child packages needs to be aligned with the other `Microsoft.Extensions.Logging` packages. Currently can't be upgraded because most recent versions are using thread, which are not supported on Wasm.
- [Microsoft.UI.Xaml](https://github.com/unoplatform/uno/pull/1503): This dependency is needs to be aligned with the currently supported API set found in Uno.

These dependencies require care and human testing to confirm compatibility with WebAssembly:

- [Microsoft.Extensions.Logging.Console](https://github.com/unoplatform/Uno/pull/894#issuecomment-495046929)

These dependencies are updated manually as part of the release process:

- Uno.SourceGenerationTasks
- Uno.Core

## chatops

You can trigger Dependabot actions by commenting on the pull-request:

```text
@dependabot recreate will recreate this PR, overwriting any edits that have been made to it
@dependabot ignore this [patch|minor|major] version will close this PR and stop Dependabot creating any more for this minor/major version (unless you reopen the PR or upgrade to it yourself)
@dependabot ignore this dependency will close this PR and stop Dependabot creating any more for this dependency (unless you reopen the PR or upgrade to it yourself)
```

Please do not use any of the `rebase|merge|squash and merge` chatops commands as they bypass our merging pull-request guidelines and `ready-to-merge` workflow.
