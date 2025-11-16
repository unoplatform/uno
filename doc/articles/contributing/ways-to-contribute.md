---
uid: Uno.Contributing.WaysToContribute
---

# Ways to contribute to Uno Platform

There are lots of ways to contribute to the Uno Platform, and all of them are appreciated. You can provide feedback, report bugs, give suggestions, contribute code, and share your Uno expertise with others.

## Provide feedback

The Uno Platform is an ongoing effort. Bug reports and feature requests are important for prioritizing where that effort goes, and identifying the most pressing user needs. Your feedback is important to us - we want to hear about your experience, scenarios, and requirements.

### Report a bug by opening an issue

If you think you've found a bug, the first thing to do is [search the existing issues](https://github.com/unoplatform/Uno/issues?q=is%3Aissue+is%3Aopen+label%3Akind%2Fbug) to check if it's already been reported. If it has been opened, provide additional commentary so we know more users are being affected.

The best way to get your bug fixed is to be as detailed as you can be about the problem.
**Providing a minimal project, stripped off as much code as possible, with steps to reproduce the problem (typed or screen-capture) is ideal.**

Here are questions you can consider before you file a bug to make sure you're not missing any important information. The bug submission template will also ask for this information.

Pre-Submission check:

1. Did you read the [documentation](xref:Uno.Documentation.Intro)?
2. Did you check the latest [pre-release](https://www.nuget.org/packages/Uno.UI/absoluteLatest) of Uno Platform to see if the issue has been fixed?
3. Does the issue exist on the WinUI project (in which case it is not an Uno Platform issue)

Issue Filing [via logging a new bug report](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fbug%2C+triage%2Funtriaged&template=bug-report.md).:

1. Did you include the snippet of the broken code in the issue?
2. What are the *EXACT* steps to reproduce this problem?
3. What specific Uno Platform version or build are you using?
4. What operating system are you using?
5. What platform(s) are you targeting that are experiencing the issue? Is it working on some platform(s) and not on others.
6. What IDE and version are you using?

[Log a new bug report](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fbug%2C+triage%2Funtriaged&template=bug-report.md)

GitHub supports [Markdown](https://help.github.com/articles/github-flavored-markdown/), so when filing issues, be sure to check the formatting in the 'Preview' tab before hitting submit.

### Request a feature

If you need a [WinUI feature](https://learn.microsoft.com/uwp/api/microsoft.ui.xaml.controls) that Uno Platform doesn't support yet, you should [submit a feature request](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fenhancement%2C+triage%2Funtriaged&template=enhancement.md). Check [existing issues first](https://github.com/unoplatform/uno/issues?q=is%3Aissue+is%3Aopen+label%3Akind%2Fenhancement) in case the same feature request already exists (in which case you can upvote the existing issue).

To help us understand and prioritize your request, please provide as much detail as possible about your scenario and why the feature or enhancement would be useful.

Wherever we can, we prefer to implement WinUI APIs for maximum cross-platform compatibility and existing code support, but for features/functionality not covered by WinUI's API, the same feature request process applies.

### Ask (and answer) questions

If you have a question, be sure first to check out our [documentation](xref:Uno.Documentation.Intro). But if you are still stuck, please visit [GitHub Discussions](https://github.com/unoplatform/uno/discussions) where our engineering team and community will be able to help you.

If you've already done some Uno development, maybe there's a GitHub discussion question you can answer, giving another user the benefit of your experience.

For a more direct conversation, [visit our Discord Server](https://platform.uno/discord).

### Spreading the word

We love to see the community grow, and whether you're trying Uno for the first time or you're an old pro, it's great to share whatever you've learnt.

You can:

- write a blog post
- present Uno at a local user group
- share your open-source Uno projects

Whatever you do, let us know [through X/Twitter](https://x.com/unoplatform), our [Discord Server](https://platform.uno/discord) in #contributing, or by emailing us at [info@platform.uno](mailto:info@platform.uno).

## Contributing code

The WinUI framework is pretty big, but many hands make light work. We welcome code and content contributions from the community, and the core team is more than happy to help new contributors ramp up and familiarize themselves with Uno Platform's codebase.

### Diving into the code

The [contributor's guide](xref:Uno.Contributing.Intro) has plenty of resources to help you get up and running with Uno's code, including a [guide to building and debugging Uno.UI](xref:Uno.Contributing.DebuggingUno), and a [guide to Uno's internals](xref:Uno.Contributing.Overview).

### Finding an issue to work on

Not sure what to work on? Take a look through the currently open [good first issues](https://github.com/unoplatform/Uno/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22) - these are all issues that have been identified by the core team as suitable for new contributors, with a well-defined scope and a relatively gentle learning curve.

Maybe you have something in mind to work on already. That's great! Make sure there's an [opened issue](https://github.com/unoplatform/Uno/issues) for it, either by creating it yourself or searching for an existing report, as described above.

In either case, once you're ready, leave a comment on the issue indicating that you'd like to work on it. A member of the core team will assign the issue to you, so as to make sure no one inadvertently duplicates your work (and you're not duplicating someone else's).

### Getting down to work

The [contributor's guide](xref:Uno.Contributing.Intro) has the information you need. The most important points are:

- Work in [your own fork of Uno](https://help.github.com/en/github/getting-started-with-github/fork-a-repo)
- Be sure to use the [Conventional Commits format](xref:Uno.Contributing.ConventionalCommits)
- Once you're done, you'll probably need to [add a test](xref:Uno.Contributing.Tests.CreatingTests)

### Creating a PR

Once you're ready to create a PR, check out the [Guidelines for pull requests](xref:Uno.Contributing.PullRequests) in Uno.

### Need help?

If there's anything you need, [ping us on our Discord Server](https://platform.uno/discord).

## Sponsors & Grants

Please consider sponsoring Uno Platform development, especially if your company benefits from this library.

Your contribution will go towards adding new features and closing issues raised by the community at Uno Platform backlog on GitHub ([Issues · unoplatform/uno (github.com)](https://github.com/unoplatform/uno/issues)) and making sure all functionality continues to meet our high-quality standards.

A grant for continuous full-time development has the biggest impact on progress. Periods of 2 to 5 days allow a contributor to tackle substantial complex issues that are otherwise left to linger until somebody can’t afford to not fix them.

Contact [@jlaban](https://github.com/jeromelaban) or [@sasakrsmanovic](https://github.com/sasakrsmanovic) to arrange a grant to Uno Platform and its core contributors.
