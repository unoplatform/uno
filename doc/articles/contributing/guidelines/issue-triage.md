# Guidelines for issue triage


## Purpose

Speed up issue management.

The detailed list of labels can be found at https://github.com/unoplatform/uno/labels.

While working on triaging issues you may not have the privilege to assign a specific label and in that case add a comment in the issue with your findings.

Here are a few predetermined searches on issues for convenience:

* [Longest untriaged issues](https://github.com/unoplatform/uno/issues?q=is%3Aissue+is%3Aopen+sort%3Acreated-asc) (sorted by age)
* [Newest incoming issues](https://github.com/unoplatform/uno/issues?q=is%3Aopen+is%3Aissue)
* [Busy untriaged issues](https://github.com/unoplatform/uno/issues?q=is%3Aissue+is%3Aopen+sort%3Acomments-desc) (sorted by number of comments)
* [Issues that need more attention](https://github.com/unoplatform/uno/issues?q=is%3Aissue+is%3Aopen+sort%3Acomments-asc)



## Scope

These guidelines serves as a primary document for triaging incoming issues to Uno. Maintainers and contributors are encouraged to either use these guidelines, or use this as a starting point if necessary.

## Security

Security related matters should be disclosed in private [as per our policy on GitHub](https://github.com/unoplatform/uno/security/policy). If a security matter is raised as an issue, please capture the relevant information, [delete the GitHub issue](https://help.github.com/en/articles/deleting-an-issue) and follow up via email. This [tool can be used to find](https://emailaddress.github.io/) (almost) any GitHub user's email address.

## Support requests

These issues should be labeled with `triage/support`, directed to our support structures (see below) and then closed _but_ before doing so consider why the support request was raised.

Is something unclear? Do we need to improve our documentation or samples?

Community support requests should be directed to:

* Our [documentation](https://platform.uno/docs/) and [troubleshooting guide](https://platform.uno/docs/troubleshooting-guide).
* [Stack Overflow](https://stackoverflow.com/questions/tagged/uno-platform).
* The [Uno gitter room](https://gitter.im/uno-platform/Lobby)
* On [Twitter using the #unoplatform](https://twitter.com/search?q=%23unoplatform) hashtag.

Organizations that want a deeper level of support beyond community support, should be directed to [contact unoplatform](https://platform.uno/contact/) to discuss obtaining professional support.


## Validate if the issue is a bug

Confirm if the problem is a bug by reproducing it. If a test case has not been supplied ask the reporter to supply one. If reproducible, move to the next step of defining priority. You may need to contact the issue reporter in the following cases:

* Do a quick duplicate search to see if the issue has been reported already. If a duplicate is found, let the issue reporter know it by marking it duplicate. Label such issues as `triage/duplicate`.
* If you can not reproduce the issue, label it as a `triage/not-reproducible`. Contact the issue reporter with your findings and close the issue if both the parties agree that it could not be reproduced.
* If you need more information to further work on the issue, let the reporter know it by adding an issue comment followed by label `triage/needs-information`.

In all cases, if you do not get a response in 20 days then close the issue with an appropriate comment.

## Define priority

We use GitHub issue labels for prioritization. The absence of a priority label means the bug has not been reviewed and prioritized yet.

We try to apply these priority labels consistently across the entire project, but if you notice an issue that you believe to be incorrectly prioritized, please do let us know and we will evaluate your counter-proposal.

If you want an issue resolved faster, please tell the maintainers that you are willing to send in a pull-request and we'll do everything we can to bring you up to speed with becoming a contributor.

* `priority/critical-urgent`: Must be actively worked on as someone's top priority right now. Stuff is burning. If it's not being actively worked on, someone is expected to drop what they're doing immediately to work on it. Team leaders are responsible for making sure that all the issues, labeled with this priority, are being actively worked on. Examples include time-sensitive hotfixes, broken builds or tests and critical security issues.

* `priority/important-soon`: Must be staffed and worked on either currently, or very soon, ideally in time for the next release.

* `priority/important-longterm`: Important over the long term, but may not be currently staffed and/or may require multiple releases to complete.

* `priority/backlog`: There appears to be general agreement that this would be good to have, but we may not have anyone available to work on it right now or in the immediate future. Community contributions would be most welcome in the meantime (although it might take a while to get them reviewed if reviewers are fully occupied with higher priority issues, for example immediately before a release).

* `priority/awaiting-more-evidence`: Possibly useful, but not yet enough support to actually get it done. These are mostly place-holders for potentially good ideas, so that they don't get completely forgotten, and can be referenced or deduped every time they come up.

## Closing issues

Issues that are identified as a support request, duplicate, not-reproducible or lacks enough information from reporter should be closed using the following guidelines explained in this file. Also, any issues that can not be resolved because of any particular reason should be closed. These issues should have one or more of following self-readable labels:

* `triage/support`: Indicates an issues is not a bug but a support request.
* `triage/duplicate`: Indicates an issue is a duplicate of other open issue.
* `triage/not-reproducible`: Indicates an issue can not be reproduced as described.
* `triage/needs-information`: Indicates an issue needs more information in order to work on it.
* `triage/unresolved`: Indicates an issue that can not be resolved.

## Help Wanted issues

We use two labels [help wanted](https://github.com/unoplatform/uno/issues?q=is%3Aopen+is%3Aissue+label%3A%22help+wanted%22+sort%3Aupdated-desc) and [good first issue](https://github.com/unoplatform/uno/issues?q=is%3Aopen+is%3Aissue+label%3A%22good+first+issue%22+sort%3Aupdated-desc) to identify issues that have been specially curated for new contributors.

The `good first issue` label is a subset of `help wanted` label, indicating that members have committed to providing extra assistance for new contributors. All `good first issue` items also have the `help wanted` label.

Items marked with the `good first issue` label are intended for _first-time
contributors_. It indicates that members will keep an eye out for these pull
requests and shepherd them through our processes.

**New contributors should not be left to find an approver, ping for reviews or identify that their build failed due to a flake.**

This makes new contributors feel welcome, valued, and assures them that they will have an extra level of help with their first contribution.

After a contributor has successfully completed 1-2 `good first issue`'s, they should be ready to move on to `help wanted` items, saving remaining `good first issue`'s for other new contributors.

These items need to ensure that they follow the guidelines for `help wanted` labels (above) in addition to meeting the following criteria:

- **No Barrier to Entry**

  The task is something that a new contributor can tackle without advanced setup, or domain knowledge.

- **Solution Explained**

  The recommended solution is clearly described in the issue.

- **Provides Context**

  If background knowledge is required, this should be explicitly mentioned and a list of suggested readings included.

- **Gives Examples**

  Link to examples of similar implementations so new contributors have a reference guide for their changes.

- **Identifies Relevant Code**

  The relevant code and tests to be changed should be linked in the issue.

- **Ready to Test**

  There should be existing tests that can be modified, or existing test cases fit to be copied. If the area of code doesn't have tests, before labelling the issue, add a test fixture. This prep often makes a great `help wanted` task!

## Quick Replies

When commenting on an issue or pull request, there's a [feature in GitHub](https://help.github.com/en/articles/using-saved-replies) where you can add a saved reply that you've already set up. The saved reply can be the entire comment or if you want to customize it, you can add or delete content. Below you'll find some suggestions that maintainers and contributors can use as a starting point.

### already fixed

> Thanks for reporting this!
>
> This has already been fixed in [INSERT URL OF PR HERE]. It should be available in version [VERSION NUMBER HERE].

### closing inactive

> Closing this out due to inactivity. @[NAME] if you're able to provide the requested information we're happy to revisit the issue.

### duplicate

> Thanks for taking the time to contribute!
>
> We noticed that this is a duplicate of [ISSUE URL]. Please follow that issue for further updates.

### future proposal

> Thanks for the suggestion!
>
> This idea is interesting for the future, but this is beyond the scope of our [current roadmap](https://github.com/unoplatform/Uno/projects/1).
>
> I've added the `priority/awaiting-more-evidence` label to this issue and will keep it open until there's more evidence. If you believe this to be incorrectly prioritized, please do let us know and we will evaluate your counter-proposal. If you want to contribute the implementation, please tell the maintainers that you are willing to send in a pull-request and we'll do everything we can to bring you up to speed with becoming a contributor.

### needs information or reproduction (android)

> Thanks for the report!
>
> Specific things I'd like to learn about are what Android API level you are using and if you are testing in a simulator or on a device.
>
> Could you please provide a test case that reproduces the issue and provide [logs from logcat](https://docs.microsoft.com/en-us/xamarin/android/deploy-test/debugging/android-debug-log?tabs=windows)?

### needs template

> Thanks for reaching out!
>
> We require the template to be filled out when submitting all issues. We do this so that we can be certain we have all the information we need to address your submission efficiently. This allows the maintainers to spend more time fixing bugs, implementing enhancements, and reviewing and merging pull requests.
>
> Thanks for understanding and meeting us halfway!

### no response

> Thank you for your issue!
>
> We haven't gotten a response to our questions in our comment [insert URL here]. With only the information that is currently in the issue, we don't have enough information to take action. I'm going to close this but don't hesitate to reach out if you have or find the answers we need, we'll be happy to reopen the issue.

### not a priority

> Thanks for the suggestion @[NAME]!
>
> This would be good to have, but we may not have anyone available to work on it right now or in the immediate future.
>
> I've added the `priority/backlog` label to this issue and community contributions would be most welcome in the meantime. If you want to contribute the implementation, please tell the maintainers that you are willing to send in a pull-request and we'll do everything we can to bring you up to speed with becoming a contributor.

### open a new issue

> @[NAME] please open a new issue and fill out the issue template so that we're better able to help you.


### send in a pull-request

> This would be good to have and we'd love it if you or someone from the community could contribute the implementation. Please let us know if you want to send in the PR and we'll do everything we can to bring you up to speed with becoming a contributor.

## Acknowledgements

- This document was based off the [Kubernetes community sig guidelines](https://github.com/kubernetes/community/blob/e4eabb7c3c26ce4463f1ab89e0ad94a7a7671b08/contributors/guide/issue-triage.md).


