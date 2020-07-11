# Ways to contribute to Uno

There are lots of ways to contribute to the Uno Platform, and all of them are appreciated. You can provide feedback, report bugs, give suggestions, contribute code, and share your Uno expertise with others.

## Provide feedback

The Uno Platform is an ongoing effort. Bug reports and feature requests are important for prioritizing where that effort goes, and identifying the most pressing user needs. Your feedback is important to us - we want to hear about your experience, scenarios, and requirements.

### Report a bug

If you think you've found a bug, the first thing to do is [search the existing issues](https://github.com/unoplatform/Uno/issues?q=is%3Aissue+is%3Aopen+label%3Akind%2Fbug) to check if it's already been reported.

If not, please [log a new bug report](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fbug%2C+triage%2Funtriaged&template=bug-report.md).

The best way to get your bug fixed is to be as detailed as you can be about the problem.
Providing a minimal project with steps to reproduce the problem is ideal.
Here are questions you can answer before you file a bug to make sure you're not missing any important information.

1. Did you read the [documentation](https://platform.uno/docs/articles/intro.html)?
2. Did you include the snippet of the broken code in the issue?
3. What are the *EXACT* steps to reproduce this problem?
4. What specific version or build are you using?
5. What operating system are you using?
6. What platform(s) are you targeting?

GitHub supports [Markdown](https://help.github.com/articles/github-flavored-markdown/), so when filing issues, be sure to check the formatting in the 'Preview' tab before hitting submit.

### Request a feature

If you need a [UWP feature](https://docs.microsoft.com/en-us/uwp/api/) or [WinUI feature](https://docs.microsoft.com/en-us/uwp/api/microsoft.ui.xaml.controls) that Uno doesn't support yet, you should [submit a feature request](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fenhancement%2C+triage%2Funtriaged&template=enhancement.md). Check [existing issues first](https://github.com/unoplatform/uno/issues?q=is%3Aissue+is%3Aopen+label%3Akind%2Fenhancement) in case the same feature request already exists (in which case you can upvote the existing issue).

To help us understand and prioritize your request, please provide as much detail as possible about your scenario and why the feature or enhancement would be useful.

Wherever we can, we prefer to implement UWP/WinUI APIs for maximum cross-platform compatibility and existing code support, but for features/functionality not covered by UWP's API, the same feature request process applies.

### Ask (and answer) questions

If you have a question, be sure first to check out our [documentation](https://platform.uno/docs/articles/intro.html). But if you are still stuck, you'll have a better chance of getting help on [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform) and we'll do our best to answer it. Questions asked there should be tagged with `uno-platform.`

Of course if you've already done some Uno development, maybe there's a [StackOverflow question](https://stackoverflow.com/questions/tagged/uno-platform) you can answer!

For a more direct conversation, [visit our Discord Channel #uno-platform](https://discord.gg/eBHZSKG).

### Spreading the word

We love to see the community grow, and whether you're trying Uno for the first time or you're an old pro, it's great to share whatever you've learnt.

You can:

 - write a blog post
 - present Uno at a local user group
 - share your open-source Uno projects

Whatever you do, let us know [through Twitter](https://twitter.com/unoplatform) or via info@platform.uno.

## Contributing code

The UWP framework is pretty big, but many hands make light work. We welcome code and content contributions from the community, and the core team is more than happy to help new contributors ramp up and familiarize themselves with Uno's codebase.

### Diving into the code

The [contributor's guide](contributing-intro.md) has plenty of resources to help you get up and running with Uno's code, including a [guide to building and debugging Uno.UI](debugging-uno-ui.md), and a [guide to Uno's internals](uno-internals-overview.md). 

### Finding an issue to work on

Not sure what to work on? Take a look through the currently open [good first issues](https://github.com/unoplatform/Uno/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22) - these are all issues that have been identified by the core team as suitable for new contributors, with a well-defined scope and a relatively gentle learning curve.

Maybe you have something in mind to work on already. That's great! Make sure there's an [opened issue](https://github.com/unoplatform/Uno/issues) for it, either by creating it yourself or searching for an existing report, as described above.

In either case, once you're ready, leave a comment on the issue indicating that you'd like to work on it. A member of the core team will assign the issue to you, so as to make sure no one inadvertently duplicates your work (and you're not duplicating someone else's).

### Getting down to work

The [contributor's guide](contributing-intro.md) has the information you need. The most important points are:
 - Work in [your own fork of Uno](https://help.github.com/en/github/getting-started-with-github/fork-a-repo)
 - Be sure to use the [Conventional Commits format](git-conventional-commits.md)
 - Once you're done, you'll probably need to [add a test](../contributing/guidelines/creating-tests.md)

### Creating a PR

Once you're ready to create a PR, check out the [Guidelines for pull requests](../contributing/guidelines/pull-requests.md) in Uno.

### Need help?

If there's anything you need, [give us a shout in the #uno-platform Discord Channel](https://discord.gg/eBHZSKG).