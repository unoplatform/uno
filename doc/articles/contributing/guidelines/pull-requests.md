# Guidelines for pull-requests

## Reviewing

Maintainers, contributors and the community can participate in reviewing pull-requests. We require `two approvals` before the pull-request can be merged. Please apply the appropriate labels to the pull-request when reviewing. If the pull-request requires minor touch ups, consider doing them in the GitHub editor rather than asking the initiator of the pull-request to do them for you.
The history should be squashed to meaningful commits, and the branch should be rebased to a recent commit.

## Merging

We typically don't merge pull-requests by hand, instead we rely on automation and a process of the pull-request initiator adding the `ready-for-merge` label. If the person who initiated the pull-request does not have permission to add a label then the reviewers can add it when the contribution is ready to ship:

The automation logic is as follows:
* When a pull-request to `master`
* The `continuous integration tests` passes
* Has `2 or more approvals`
* There are `no requests for change`
* Is labelled with `ready-to-merge`
* Not labelled with `do-not-merge/breaking-changes` or `do-not-merge/work-in-progress`
* It will be automatically merged.

This logic is defined in [this file](https://github.com/unoplatform/Uno/blob/master/.mergify.yml).

Once a pull-request meets the above criteria Mergify will automatically update the pull-request with the contents of master. If CI passes, then Mergify will merge that pull-request. If multiple pull-requests are mergeable open then Mergify will queue the mergeable pull requests and update them one at a time serially, merging if CI passes.

If the branch is within the `unoplatform/uno` repository then the branch will be automatically deleted after merging by the [delete-merged-branch](https://github.com/apps/delete-merged-branch) robot.


