---
uid: Uno.Contributing.PullRequests
---

# Guidelines for pull-requests

If you don't know what a pull request is, read the [About pull requests documentation from GitHub](https://docs.github.com/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/about-pull-requests).

## Creating a PR

If you are an outside contributor, please fork the Uno Platform repository you would like to contribute to your account. See the GitHub documentation for [forking a repo](https://help.github.com/articles/fork-a-repo/) if you have any questions about this.

Make sure the repository can build and all tests pass, as well as follow the current [coding guidelines](xref:Uno.Contributing.CodeStyle).

Pull requests should all be made to the **master** branch.

### Updating your branch on top of the latest of the default branch

Make sure to rebase your work on the latest default branch of the Uno repository, when working on a fork:

- Add the official uno repository to your remotes:

    ```bash
    git remote add uno-origin https://github.com/unoplatform/uno
    ```

- Fetch the official repository

   ```bash
    git fetch uno-origin
   ```

- Rebase your work on the default branch

    ```bash
    git rebase uno-origin/master
    ```

- Then push your branch to your fork:

    ```bash
    git push -f
    ```

**Commit/Pull Request Format**

All commits **must** be in the [Conventional Commits format](xref:Uno.Contributing.ConventionalCommits), otherwise the build will fail.

We use this convention to automatically generate release notes for new releases, and means that your commit messages will appear untouched in the release notes.

Make sure that:

- You create only the least possible commits, where each commit details a specific added feature or bug fix.
- Try using the category feature as frequently as possible. (e.g. `feat(NavigationView): Updated the main panel`, or `fix(ProgressRing): Fix visibility`)
- Squash your commits, using interactive rebase:

   ```bash
   git fetch uno-origin
   git rebase uno-origin/master -i # Rebase your branch on top of the latest master, squash using fixups
   git push -f
   ```

- If you're fixing a regression introduced by a PR that has not been released in a stable version yet, use the `reg` category. Example: `fix(reg): Fixing issue of previous PR`.

When opening a PR, you'll see the description is filled by a template. Make sure to read through the template and fill the missing parts in it.

If you haven't [added tests](xref:Uno.Contributing.Tests.CreatingTests) appropriate to your changes, the reviewers will probably ask you to add some.

## Reviewing

Maintainers, contributors, and the community can participate in reviewing pull-requests. We require `two approvals` before the pull-request can be merged. Please apply the appropriate labels to the pull-request when reviewing. If the pull-request requires minor touch ups, consider doing them in the GitHub editor rather than asking the initiator of the pull-request to do them for you.
The history should be squashed to meaningful commits, and the branch should be rebased to a recent commit.

## Merging

Once a PR is reviewed and approved. One of the team members will merge it when it passes CI and is ready to merge.

If the branch is within the `unoplatform/uno` repository then the branch will be automatically deleted after merging by the [delete-merged-branch](https://github.com/apps/delete-merged-branch) robot.
