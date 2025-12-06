---
uid: Uno.Contributing.Docs
---

# Contributing docs to Uno Platform

Good documentation is essential, and writing docs is a valued contribution that makes Uno more useful for everybody. This article covers *when* to write docs and *what* you should write, with a focus on two of the most common forms of documentation: step-by-step guides (ie howtos or tutorials), and feature documentation.

We have a few different fill-in-the-blanks style templates, linked below.

For the DocFX tool used to build the docs website, see [more info here](docfx.md).

## Key links

* how-to template: https://github.com/unoplatform/uno/blob/master/doc/.howto-template.md
* WinUI feature template: https://github.com/unoplatform/uno/blob/master/doc/.feature-template.md
* Uno-only feature template: https://github.com/unoplatform/uno/blob/master/doc/.feature-template-uno-only.md

## Resources

Some useful resources on writing good technical documentation:

* [ReactiveUI docs style guide](https://www.reactiveui.net/contribute/content-style-guide/)
* [Divio's Documentation System](https://documentation.divio.com/)

## Writing step-by-step guides

Step-by-step guides that address a particular problem or use case that multiple developers are likely to encounter.

* For longer tutorials, it's fine to split the content over multiple pages.
* Guides should always be accompanied by working code. Standalone applications should be added to the [Uno.Samples repository](https://github.com/unoplatform/Uno.Samples) and linked to from the associated tutorial.
* Use the [how-to template](https://github.com/unoplatform/uno/blob/master/doc/.howto-template.md) as a starting point.
* Structure guides as a series of clear, actionable steps. After carrying out every step as described, the user should end up with working code that demonstrates the objective of the guide (either a standalone sample, or a new feature in their existing app).

## Documenting features

Let's say you're implementing a new Uno Platform feature. Do you need to add documentation for it?

It depends.

There's two different cases:

### I'm implementing a feature from the WinUI contract

Uno's API matches WinUI's API, and most of the time, a new Uno feature will map to an existing WinUI feature.

In this case, to the extent that the behavior you're adding is the same as the WinUI behavior, you **don't** need to add documentation. The existing WinUI documentation is fine. It's already linked to from [Uno's reference documentation](../implemented-views.md).

What if the Uno behavior deviates from WinUI behavior?

If it's just a case of part of the functionality not being implemented yet, **and** the developer using the corresponding type/method/event would discover that it's marked with the `[NotImplemented]` attribute, then that's fine - there's no need to document it further.

But sometimes the functionality **can't** be supported, due to intrinsic limitations of the target platform. (This is more likely to be the case with non-UI features.) And/or part of the functionality may implicitly fail to work, even though none of the entry points are marked `NotImplemented`.

In those cases, it's important to add documentation. Copy the [WinUI feature template](https://github.com/unoplatform/uno/blob/master/doc/.feature-template.md) to the [features directory](https://github.com/unoplatform/uno/tree/master/doc/articles/features) (or [controls directory](https://github.com/unoplatform/uno/tree/master/doc/articles/controls) for controls inheriting from `FrameworkElement`) and fill in the appropriate sections. Make sure to fill in the matrix describing which features are supported on which platforms.

### I'm implementing a feature that's not part of WinUI

Rarely, features are added to Uno Platform that aren't part of WinUI (`VisibleBoundsPadding` and `ElevatedView` are two examples). Somewhat more commonly, platform-specific functionality or options are added to an existing feature.

It's important to document these novel features when you add them, since they aren't covered anywhere else. Again, copy the [Uno-only feature template here](https://github.com/unoplatform/uno/blob/master/doc/.feature-template-uno-only.md) to the [features directory](https://github.com/unoplatform/uno/tree/master/doc/articles/features) and fill in the sections.

## Updating the documentation from linked repositories

The Uno Platform documentation is aggregated onto https://platform.uno/docs/articles/intro.html, including some repositories that are in the unoplatform GitHub organization.

### Updating the documentation for other repositories

The CI includes a step to clones repositories at a specified commit or branch, and provides the path to the DocFX table of content.

To include documentation (let's take Uno.Themes as an example):

* Update your documentation in `Uno.Themes` repository, preferably in a folder named `doc`
* Include a file named `toc.yml`, using this file as a template:

  ```yml
  items:
  - name: Introduction to Uno Themes
    href: ../Readme.md
  ```

* Once you have merged your changes into `Uno.Themes`' `main` (or the default branch), take the commit id and place it in the `doc/import_external_docs.ps1` of the uno repository, as follows:

   ```powershell
   @("https://github.com/unoplatform/uno.themes", "uno.themes", "INSERT_COMMIT_HASH_OR_BRANCH_NAME"),
   ```

* Create a PR and validate that the content you added is appearing in the Uno Platform `toc.yml` where `external/uno.themes/` is visible. Ensure href path ends by `toc.yml` for all nodes to be visible.

## Documentation deployment environments

The Uno Platform documentation is deployed to three different environments, each serving a specific purpose in the documentation lifecycle:

### Canary Environment

The **Canary** environment reflects the latest changes from the `master` branch and default branches of all external documentation repositories. It is automatically updated on every commit to the `master` branch.

- **Purpose**: Active iteration testing and previewing upcoming documentation changes
- **Source**: Default/main branches of all repositories
- **Import Script**: `doc/import_external_docs_canary.ps1`
- **Build Target**: `GenerateDocCanary` in `build/Uno.UI.Build.csproj`
- **Deployment**: Automatic on every commit to `master`

Use the Canary environment when you want to:
- Preview documentation changes before they are included in a release
- Test documentation for features that are still in development
- Validate that external documentation imports work correctly with the latest changes

### Staging Environment

The **Staging** environment mirrors what will go to production, built from stable release branches or default branches depending on the repository's release status.

- **Purpose**: Final verification before production deployment
- **Source**: Stable release branches (e.g., `release/stable/6.4`) or default branches
- **Import Script**: `doc/import_external_docs.ps1`
- **Build Target**: `GenerateDoc` in `build/Uno.UI.Build.csproj`
- **Deployment**: Automatic on commits to `master` or `release/stable/*` branches

Use the Staging environment when you want to:
- Verify documentation that will be deployed to production
- Test critical documentation fixes before they go live
- Ensure that stable documentation is correctly integrated with the latest release

### Production Environment

The **Production** environment is the live documentation site at https://platform.uno/docs/articles/intro.html.

- **Purpose**: Live documentation for public consumption
- **Source**: Same as staging (stable release branches)
- **Deployment**: Manual approval required after staging deployment

Production deployment requires manual approval to ensure:
- Final quality checks are performed
- Documentation aligns with the current stable release
- Critical fixes can be deployed independently of code releases

### Pipeline Architecture

The documentation deployment uses Azure DevOps pipeline stages instead of a separate release pipeline:

1. **Setup Stage**: Validates commits, spelling, and markdown formatting
2. **Docs Generation Stage**: Builds both canary and staging documentation artifacts
3. **Deploy Canary Stage**: Automatically deploys canary docs to the canary environment (master branch only)
4. **Deploy Staging Stage**: Automatically deploys staging docs to the staging environment (master and stable branches)
5. **Deploy Production Stage**: Deploys staging docs to production after manual approval (master and stable branches)

This architecture ensures:
- Documentation deployment is automated, repeatable, and traceable
- The deployment process is integrated with the main build pipeline
- Different environments can be updated independently based on branch and approval rules
