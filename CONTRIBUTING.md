# How to contribute

There are lots of ways to contribute to the Uno Platform, and we appreciate the help from the community. You can provide feedback, report bugs, give suggestions, contribute code, and participate in the platform discussions.

Contribute to Uno in your browser using [GitPod.io](https://gitpod.io): [![Open Uno in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/unoplatform/uno).

## Code of conduct

To better foster an open, innovative, and inclusive community, please refer to our [Code of Conduct](CODE_OF_CONDUCT.md) when contributing.

## Provide feedback

The Uno Platform is an ongoing effort, and as Microsoft progresses on UWP, the Uno platform follows the trails. While the development remained closed for a while, the areas covered by Uno may not suit everyone's needs, which is why your feedback is important to us.

We want to hear about your experience, scenarios, and requirements.

### Report a bug

If you think you've found a bug, please log a new issue in the [Uno Platform GitHub issue tracker](https://github.com/unoplatform/uno/issues). When filing issues, please use our [bug filing template](https://github.com/unoplatform/uno/blob/master/.github/ISSUE_TEMPLATE/bug-report.md).

The best way to get your bug fixed is to be as detailed as you can be about the problem.
Providing a minimal project with steps to reproduce the problem is ideal.
Here are questions you can answer before you file a bug to make sure you're not missing any important information.

1. Did you read the [documentation](https://github.com/unoplatform/uno/tree/master/doc/index.md)?
2. Did you include the snippet of the broken code in the issue?
3. What are the *EXACT* steps to reproduce this problem?
4. What specific version or build are you using?
5. What operating system are you using?

GitHub supports [markdown](https://help.github.com/articles/github-flavored-markdown/), so when filing bugs make sure you check the formatting before clicking submit.

### Make a suggestion

If you have an idea for a new feature or enhancement, let us know by filing an [issue](https://github.com/unoplatform/uno/issues). To help us understand and prioritize your idea, please provide as much detail about your scenario and why the feature or enhancement would be useful.

### Ask questions

If you have a question, be sure first to check out our [documentation](https://github.com/unoplatform/uno/tree/master/doc/index.md). But if you are still stuck, you'll have a better chance of getting help on [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform) and we'll do our best to answer it. Questions asked there should be tagged with `uno-platform.`

For a more direct conversation, [our Gitter is also a good place to visit](https://gitter.im/uno-platform/Lobby).

## Contributing code and content

The Uno Platform is an open-source project, and we welcome code and content contributions from the community.

**Identifying the scale**

If you would like to contribute to one of our repositories, first identify the scale of what you would like to contribute. If it is small (grammar/spelling or a bug fix) feel free to start working on a fix.

If you are submitting a feature or substantial code contribution, please discuss it with the team. You might also read these two blog posts on contributing code: [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) by Miguel de Icaza and [Don't "Push" Your Pull Requests](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/) by Ilya Grigorik. Note that all code submissions will be rigorously reviewed and tested by the Uno Platform team. Only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.

**Obtaining the source code**

If you are an outside contributor, please fork the Uno Platform repository you would like to contribute to your account. See the GitHub documentation for [forking a repo](https://help.github.com/articles/fork-a-repo/) if you have any questions about this.

**Submitting a pull request**

If you don't know what a pull request is read this article: https://help.github.com/articles/using-pull-requests. Make sure the repository can build and all tests pass, as well as follow the current coding guidelines.

Pull requests should all be made to the **master** branch.

**Commit/Pull Request Format**

```
Summary of the changes (Less than 80 chars)
 - Detail 1
 - Detail 2

Addresses #bugnumber (in this specific format)
```

**Tests**

-  Tests need to be provided for every bug/feature that is completed.
-  Tests only need to be present for issues that need to be verified by QA (e.g., not tasks)
-  If there is a scenario that is far too hard to test, there does not need to be a test for it.
  - "Too hard" is determined by the team as a whole.
