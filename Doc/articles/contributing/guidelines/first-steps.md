# How to begin contributing

  There are many forms of contributing to Uno. 

## simpler: create an Issue (bug report)
Before submitting new Issue, check if someone else doesn't submit such issue before: [Issues](https://github.com/unoplatform/uno/issues).
If not, use this: [new bug report](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fbug%2C+triage%2Funtriaged&template=bug-report.md).
Now, be patient and wait for someone to propose solution.

## harder: try to find/catch error in Uno source
 First step is to get ready Visual Studio - see [this article](uno-development\debugging-uno-ui.md).
 Do not miss "Faster dev loop with single target-framework builds" and "Debugging Uno.UI" sections.

Try to change Uno code, compile it. Then switch to your app solution, clean, build (beware: use Uno.UI nuget package in same version as in UnoNugetOverrideVersion node in crosstargeting_override.props file).
Test. If it works, you have solution, and you can treat this solution as your hidden secret. But you can also share this solution, by creating Pull Request, to be included in future versions of Uno.

To make something bigger, you have to use Git.

## Hardest: make Git your friend

### Git philosophy.
 You will use three different Uno source code trees:
 1. Uno.master (https://github.com/unoplatform/uno),
 2. Your fork of it (Uno.fork), and
 3. Your local copy/clone of it (Uno.clone)
 And many branches of these trees.

#### Forking Uno
* Go to https://github.com/unoplatform/uno,
* Find button "Fork",
* use it.

In effect, you would have own tree of Platform Uno code on Github.

#### Creating local (disk) copy/clone 
* From Visual Studio File menu, use `Clone or Check Out code`.

In effect, you would have next tree of Platform Uno code, but this time on your local drive, ready to use for compilations.

Recommended:
Write down current build number (Tools:Nuget Package Manager, check build number of Uno.UI) - you can use it in `crosstargeting_override.props` file.
 

#### Working with Branches
 Seems like best branch structure is this:
 1. your master, regularly synchronized with Uno.master (see below),
 2. new branch after every synchronization (Team Manager, Branches, checkout `master` branch, press `New Branch`), e.g. branch named as build number (e.g. `build-2.1.0-dev.945`)
 3. new branch for every PR you would like to share (push to Uno.master tree), based on master (or branch from point 2, above)
 4. branch with all your changes, not incorporated to Uno.master, based on branch mentioned in point 2; e.g. `myCompilation945`

 When you start on some change/addition to Uno, do this:
 * checkout your branch (4),
 * make all changes, switching between compiling Uno and compiling your app,
 * test it, be sure that your solution works.

 Now, create one commit from all these changes:
 * Team Explorer, Changes - review changes,
 * if you encounter some changes you want to exclude from commit, then use 'stage' for all changes you want to commit,
 * press Commit (it will include all changes, or - if some of them are staged - all staged)

 Send changes 
 * create new branch for this PR, naming it from functionality changed/added,
 * checkout this branch,
 * right click on your build branch, select `View History`,
 * right click on all commits you want to include in this branch (i.e. in your next PR), choose 'cherry pick',
 * add documentation (mainly, doc/ReleaseNotes/_ReleaseNotes.md),
 * add tests (see [this article](creating-tests.md),
 * commit all changes (doc/tests),
 * Team Explorer, Sync, Push - it will create new branch on your Uno.Fork.

 Create PR
 * Go to [Uno.master](https://github.com/unoplatform/uno),
 * you will see information about just pushed branch, with 'compare' button,
 * in effect, your PR will be created.
 

#### Synchronizing trees
 From time to time, sync all trees, getting changes from Uno.Master to your Uno.fork and Uno.clone.
* Open your Uno.fork repository, and find button `Compare`. When you click it, you will see comparison of Uno.master nad Uno.fork, and link would be something like:
 https://github.com/unoplatform/uno/compare/master...[user]:master
 Change it to:
 https://github.com/unoplatform/uno/compare/[user]:master...master (reversing master and [user]:master).
 It will be converted to:
 https://github.com/[user]/uno/compare/master...unoplatform:master .
 Now, you would see all commits to Uno.master since your last synchronization.
* Press `Create pull request` (button with green background), then `Merge`.
* Open Uno project (Uno.clone) in Visual Studio, go to Team Explorer window.
* Select your master branch (Branches, check out `master`).
* Sync with Uno.fork (Sync, Pull).
* Make new build branch (branch from point 2).
* change `crosstargeting_override.props` file (new build number).
* checkout this new build branch, and cherry-pick all required commits from previous build branch (hopely, not all of them, as some should be already included in Uno.master).
* build this branch, and use/test in one or two your apps. Sometimes, new build of Uno make something not working (e.g. once it was DoubleClick); in such a case, your previous build branches would help (and you can trace down problem in new build and corrects them).


#### Polluted master
If you pollute your master with unwanted changes (and these changes would appear in new PR), use this procedure:
* Team Explorer, Branches, checkout master branch
* view history of branch, check how many commits should be deleted,
* Team Explorer, Sync, Actions, Open command prompt
* Enter `git status`, to be sure what is current branch
* `git reset --hard HEAD~3` (replace 3 with number of commits to be removed)
* `git push origin +master --force` - it will 'propagate' this removing also to Uno.fork, and both of your master (Uno.clone and Uno.Fork) would be clean.
