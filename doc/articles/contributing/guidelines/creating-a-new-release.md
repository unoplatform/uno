# Guidelines for creating a new release

## Overview

When a pull-request is merged to master a new development version is automatically pushed to NuGet.org. A development version can be identified by the `-dev` suffix ie. `major.minor.patch-dev.2294`.

A `Canary` is a version of a real-world application or particular version of Uno that is offered under professional support. There are continuous integration (CI) pipelines configured that consume these development builds. If the builds of these applications fail then then it's a early signal that overnight a breaking compilation change may have been accidentally introduced into Uno. This style of regression is rare as there are API approval tests that are run on every pull-request to master. Typically if a regression slips in then it's something that only integration testing would have picked up - ie. package incompatility between dependencies.

Uno does manual QA on `Canary` failures to determine the differences between the canary build and the previous stable version of Uno. Fixes to regressions are resolved as quickly as possible by the team.

When the team is nearing a release, a `Beta` candidate/branch is created. The canary process described above is then configured to consume the beta releases and any regression fix that is deemed required for the release is cherry-picked into the beta. Uno releases when there are no more regressions. Uno typically releases every two weeks or once a month. The team desires to release weekly and is actively investing into quality systems, tooling and processes to make this a reality.

Internally, a release of Uno is currently named after comic book characters and the team always increments the first letter. The previous pattern was alcohol/drink names.
