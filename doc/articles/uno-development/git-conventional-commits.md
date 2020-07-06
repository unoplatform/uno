# Conventional Commits

Uno uses the [Conventional Commits format](https://www.conventionalcommits.org/en/v1.0.0/#summary) for all changes to the source repository. If you submit a PR, the CI build won't pass until all commits satisfy the format. This format embeds semantic information in each commit that's used to automatically generate release notes.

The Conventional Commits format is as follows:

```
<type>([optional scope]): <description>

[optional body]

[optional footer(s)]
```

### Type

Only certain _types_ are permitted, with the most common being:

 - `fix:` a commit that fixes a bug in Uno.
 - `feat:` a commit that adds new functionality to Uno.
 - `docs:` a commit that adds or improves Uno's documentation.
 - `test:` a commit that adds unit tests.
 - `perf:` a commit that improves performance, without functional changes.
 - `chore:` a catch-all type for any other commits. For instance, if you're implementing a single feature and it makes sense to divide the work into multiple commits, you should mark one commit as `feat` and the rest as `chore`.

### Scope

 The _scope_ optionally provides extra context. If you're fixing a `ListView` bug, for example, you might use `fix(listview)`. 

 One scope with special meaning is `reg`, short for regression. This is used for fixes for bugs that weren't present in the most recent stable release.

### Breaking changes

Breaking changes are indicated by putting `BREAKING CHANGE:` at the start of the message body, for any commit type. Optionally they may be emphasised by appending a `!` after the type and scope. The message body should provide appropriate guidance for developers affected by the breaking change.

## Fixing up commits

If you already made commits and they don't meet the Conventional Commits specification, you have a couple of options:
 
 - if there's only one commit to redo, the easiest option is to use `git commit --amend` with no staged changes, which will allow you to edit the commit message.
 - if you have multiple commits to reformat, you'll probably need to do an [interactive rebase](https://git-scm.com/book/en/v2/Git-Tools-Rewriting-History) and use the `reword` option.

## Examples

### A commit fixing a bug

```text
fix(webview): Fixed video display in WebView on Android: the control was forced to use software rendering.

This feature is now opt-in instead.
```

### A commit adding a new feature

```text
feat(imageBrush): [iOS][macOS] Add support of WriteableBitmap
```

### A commit introducing a breaking change

```text
fix(resourcedictionary)!: Make ResourceDictionary.Lookup() internal, use correct lookup

BREAKING CHANGE: This method isn't part of the public .NET contract on WinUI. Use item indexing or TryGetValue() instead.
```

### A commit with no special meaning

```text
chore: Fix XAML parsing sample
```
