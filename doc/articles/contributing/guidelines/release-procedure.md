---
uid: Uno.Contributing.ReleaseProcedure
---

# Uno.UI release procedure

Uno.UI uses [NBGV](https://github.com/dotnet/nbgv) for its versioning.

Tagging is the main driver for planning releases.

## Branches

- On the `master` branch, the main development is happening and is considered unstable. The nuget packages produced end with `-dev.X`, where X is the number of commits since the last initiated release.
- On the `release/beta` branch, stabilization occurs to get a stable release. The version is inherited from the branch point from master. The nuget packages produced end with `-beta.X`, where X is the number of commits since the last stable release.
- On the `release/stable` branch, stable nuget packages are produced for each pushed merge or commit. The **Patch** number is increased by the number of commits since the release tagging occurred.
- On `dev`, `feature`, and `project` branches, the behavior is to inherit from the base branch it was created from and create a nuget package with an experimental tag. Those branches must not be tagged.

## Creating a release

### When planning for a beta

- Once a release is planned, make a branch in `release/beta` (e.g. `release/beta/1.29`), and tag the commit using the requested version (e.g. `1.29`). Do not include the patch number, as it will be added by GitHub when publishing a release. Tagging will automatically increased the version in the `master` branch by a **minor** number.
- Make stabilization fixes to the `release/beta/1.29` branch.
- Once the stabilization fixes are done, take the last `release/beta/1.29` commit and make a `release/stable/1.29` branch. Commits to this branch will automatically keep the `1.29` version, as the base **beta branch** was tagged `1.29`.
- Publish the release on GitHub using the patch number (e.g. `1.29.0` if there where no changes)

### When planning for a release without a beta

- Once a release is planned, make a branch in `release/stable` (e.g. `release/stable/1.29`), and tag the commit using the requested version (e.g. `1.29`). Tagging will automatically the version in the `master` increased by a **minor** number.
- Commits to `release/stable/1.29` will automatically keep the `1.29` version.
- Publish the release on GitHub using the patch number (e.g. `1.29.0` if there where no changes)

## Canaries

A 'canary' in Uno parlance is a version of a real-world application (or class library) that is used to test changes to Uno. There are continuous integration (CI) pipelines configured that consume the latest development builds of Uno and produce new nightly canary versions of applications (eg [Calculator](https://github.com/unoplatform/calculator), [UADO](https://github.com/unoplatform/uado) etc). If the builds of these applications fail then then it's an early signal that overnight a breaking compilation change may have been accidentally introduced into Uno. This style of regression (binary breaking change) is rare as there are API approval tests that are run on every pull-request to master. Typically if a regression slips in then it's something that only integration testing would have picked up - ie. package incompatibility between dependencies.

The Uno team does manual QA on canary failures to determine the differences between the canary build and the previous stable version of Uno. Fixes to regressions are resolved as quickly as possible by the team.
