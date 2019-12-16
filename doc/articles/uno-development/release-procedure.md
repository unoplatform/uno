# Uno.UI release procedure

Uno.UI uses [GitVersion])(https://gitversion.readthedocs.io/en/latest/) for its versioning, in `mainline` mode for the `release/stable` branches, and **ContinuousDeployment** for the `master` branch.

Tagging is the main driver for planning releases.

## Branches 
- On the `master` branch, the main development is happening and is considered unstable. The nuget packages produced end with `-dev.X`, where X is the number of commits since the last initiated release.
- On the `release/beta` branch, stabilisation occurs to get a stable release. The version is inherited from the branch point from master. The nuget packages produced end with `-beta.X`, where X is the number of commits since the last stable release.
- On the `release/stable` branch, stable nuget packages are produced for each pushed merge or commit. The **Patch** number is increased by the number of commits since the release tagging occured.
- On `dev`, `feature`, and `project` branches, the behavior is to inherit from the base branch it was created from and create a nuget package with an experimental tag. Those branches must not be tagged.

## Creating a release

### When planning for a beta
- Once a release is planned, make a branch in `release/beta` (e.g. `release/beta/1.29`), and tag the commit using the requested version (e.g. `1.29`). Do not include the patch number, as it will be added by GitHub when publishing a release. Tagging will automatically increased the version in the `master` branch by a **minor** number.
- Make stabilisation fixes to the `release/beta/1.29` branch.
- Once the stabilization fixes are done, take the last `release/beta/1.29` commit and make a `release/stable/1.29` branch. Commits to this branch will automatically keep the `1.29` version, as the base **beta branch** was tagged `1.29`.
- Publish the release on GitHub using the patch number (e.g. `1.29.0` if there where no changes)

### When planning for a release without a beta
- Once a release is planned, make a branch in `release/stable` (e.g. `release/stable/1.29`), and tag the commit using the requested version (e.g. `1.29`). Tagging will automatically the version in the `master` increased by a **minor** number.
- Commits to `release/stable/1.29` will automatically keep the `1.29` version.
- Publish the release on GitHub using the patch number (e.g. `1.29.0` if there where no changes)
