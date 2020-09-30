## Debugging Uno.UI

To debug Uno.UI inside of an existing project, the simplest way (until Microsoft provides a better way to avoid overriding the global cache) is to :
* Install a published `Uno.UI` package in a project you want to debug, taking note of the version number.
* Rename [crosstargeting_override.props.sample](https://github.com/unoplatform/uno/blob/master/src/crosstargeting_override.props.sample) to `crosstargeting_override.props`
* Uncomment the `UnoNugetOverrideVersion` node
* Change the version number to the package you installed at the first step
* Build your solution.

> Note: This overrides your local nuget cache, making the cache inconsistent with the binaries you just built. 
To ensure that the file you have in your cache a correct, either clear the cache, or observe the properties of the `Uno.UI.dll` file, where the
product version should contain a git CommitID.

Once Uno.UI built, open the files you want to debug inside the solution running the application you need to debug, and set breakpoints there.

You may improve your built time by selecting an active target framework, see the **Faster dev loop** section above.

## Running the samples applications

The Uno solution provides a set of sample applications that provide a way to test features, as
well as provide a way to write UI Tests. See [this document](working-with-the-samples-apps.md) for more information.

## Microsoft Source Link support
Uno.UI supports [SourceLink](https://github.com/dotnet/sourcelink/) and it now possible to
step into Uno.UI without downloading the repository.

Make sure **Enable source link support** check box is checked in **Tools** / **Options**
/ **Debugging** / **General** properties page.
