# How to build & debug apps
Sometimes you need to debug an app with a _Debug_ version of Uno.
This is useful if you need to understand a certain behavior from Uno
and pin-point de source using the debugger and eventually test some changes
on a real application.

To do this, you need to recompile your app using a _debug_ version of Uno­
(or a _Release_ version if you prefer).

> Instead of tweating your app solution to reference the your custom Uno
> output (because there's many steps to follow and you will have to avoid commiting this
> to source control), we found a simpler way: override the nuget output folder.

## HOW-TO (Steps)
\*\* **THIS PROCEDURE WILL ONLY WORK WITH PROJECTS USING `project.json` NUGET SYSTEM.** \*\* 

1. In your application project (the one referencing Uno):
   1. Make sure to build at least once (to ensure the nuget packages are downloaded)
   1. Check in the `project.json` file to get the version of the `Uno.UI` package.
      The file should look-like this:
      ``` json
      ...
      "dependencies": {
        "CommonServiceLocator": "1.3",
        "Uno.UI": "1.3.15142-dev",
        "Uno.UI.Tasks": "2.10.13115",
        ...
      },
      ...
      ```
      -> In this example, the version is `1.3.15142-dev`.
1. Create a file named `nuget_version_override.txt` and in the solution folder
  (where `Uno.UI.sln` is) and put the version number in it - **on the first line and nothing
  else is the file**.
1. Build the project for the platform you need (or build all the solution to get all of them).
   See below for list of project to compile for each platform...
1. Never commit the `nuget_version_override.txt` into source control.  It should be ignored
   by default anyway.

## Projects per platform
| Platform | _Solution Folder_ + Project |
| --- | --- |
| Android (Xamarin) | _`Uno.UI`_ / `Uno.UI.Android`
| iOS (Xamarin) | _`Uno.UI`_ / `Uno.UI.iOS`

---
## Clean-up Warning

> ![](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAAGzElEQVR4Xu2dO4gkVRSGv0VFFAUVIw00cEXwFZu5oKCrBoYriOsjWMEHKogoZguLgQauqKDCKmpgqOATRBNjBV9gopGCr2gXA1G5OrXO9FT3uVX3dc6p28kEdaum/v98U91fVw+9h+U9LgAOAvuAi7bifw98DBwDflpSJXsWFPYU4AngceD0Nbn/AA4DR4C/ltDNUgA4FXgLuDVyqGHtgSVAsBQAngIejRz+sCxcCZ6cuI+55UsA4ArgcyA8BUx5/AlcCXw7ZSdra5cAwEfAdTMH8x6wf+a+JnbzDsAtwNuJk7gReD/xGGp39wzAacBXwN7E9r8BrgLCU4K7h2cAHgaezjSxB4CjmY6l6jBeATgf+A44J1Pbv21dScJPVw+vALwAHMo8qXAFCFcCVw+PAMzVPmmwLrXQIwAp2idB4E4LvQGQQ/skCFxpoScAcmmfBIArLfQEQE7tkyBwo4VeAMitfRIAQQcvAX6XFmrf7gWA54F7K5f9LPBg5d+Z/dd5AKCU9kllu9BCDwCU1D4JAvNaaB2AGtonQWBaCy0DUEv7JABMa6FlAGpqnwSBWS20CkBt7ZMAMKuFVgFooX0SBCa10CIArbRPAsCkFloEoKX2SRCY00JrAGjQPgkCU1poCQAt2icBYEoLLQGgSfskCMxooRUAtGmfBIAZLbQCgEbtkyAwoYUWANCqfRIAJrTQAgCatU+CQL0WagfAgvZJEKjWQs0ABO37ErhUalj5dtVaqBkAS9onMahWC7UCYE37JADUaqFWACxqnwSBSi3UCIBV7ZMAUKmFGgHIrX2pGf+WJjthuzotTC1nQvaopSW0LzVjTgBCCaq0MLWcqKlGLiqlfakZcwOgSgtTy4mcbdSyUtqXmjE3AKEMNVqYWk7UZCMWldS+1IwlAFCjhanlRMw2aklJ7UvNWAKAUIoKLUwtJ2q6wqLS2peasRQAKrQwtZwcAOTWvtVzSs1YCoBwns21MLWcVABKaJ8lAJprYUsASmmfNQCaamFLAEppnzUAmmphKwBKap9FAJppYSsASmqfRQCaaWELAEprn1UAmmhhCwBKa59VAJpoYW0AamifZQCqa2FNAGppn3UAvgaurvUFFTUBeAh4JvWdoxn7p2Ys+U7gujj3A8/NyDp5l9RyYn9hTe2zfgUI519NC2sBUFP7PABQTQtrAFBb+7wAUEULawBQW/u8AFBFC0sD0EL7PAFQXAtLAtBK+7wBUFQLSwLQSvu8ARDyFNPCUgC01D6PABTTwlIAtNQ+jwAU08ISAFwOfDHj69pj31Saui41Y4t3AscyFtHC1HLGTvRD4PqpUyq4PjWjFgBCRe8CN+XsKrWc1XPRoH1enwKGXFn/tzAnAFq0zzsAWbUwJwBatM87AFm1MBcAmrQv51Ok1mNl08JcAGjSPq1Dy31eWf63MAcA2rQvd9Faj5dFC3MAoE37tA6sxHkla2EqABq1r0TRmo+ZpIUpAGjVPs3DKnFuSVqYAoBW7StRsvZjzr5bOBeArn26kPgV2Dvn6+znAmBJ++ZmHEas6V7AJuxmaeGccqxp35yM24u2AsAsLZxTjjXtm5PRIgCz7hZOLcei9k3NuHqZtXIFmHW3cEo5VrVvSsax51hrAEzSwinlWNW+KRk9ADDpbmFsOZa1LzbjulfY1q4AIUe0FsaWY0n7lvB5gJh3IaK0MAYAa9rXAfivgSgtjAHAmvZ1AP5vQLxbKAFwM/BOzPVG8Ropo3TqFl8DbM90A/DBupCbyrGqff0KsLOBjVq4CQCr2tcB2P3nvvZu4ToALGtfB2A3AGu1cB0AlrWvAzD+hD+qhWMAWNe+DsA4AKNaOAaAde2TXtUvefsuLVwFwIP2LXnAMdl3aOF2ALxoX0wJS16zQwu3A+BF+5Y83NjsJ7VwAOBs4Afg3Ngj9HWmG/gFuBg4PgBwN/Cy6Uj95Kc2cAfw2gDAm8CBqUfo60038CpwcADgM+Aa03H6yU9t4FPg2g7A1Nr8rP8E2DcA8AZwm59sPUlEA8eAOwcA7gJeidipL/HTwO3A6wMAZ21p4Hl+8vUkGxr4eUsDT2x/I+ge4KVe2yIa+FcBQ9LVewFHgfsWUcFyQ4bvbXpkiD92NzB8p+9h4IzlduQy+QngMSD8kZ98rPtAyIXAIWA/cBlwpstK/Ic6DoRvJw+3gV8EflyNnPqJWf8VOk/YAXA+YCleB0BqyPn2DoDzAUvxOgBSQ863dwCcD1iK1wGQGnK+vQPgfMBSvA6A1JDz7R0A5wOW4nUApIacb+8AOB+wFK8DIDXkfHsHwPmApXgdAKkh59s7AM4HLMX7B1/HRJBSeXWDAAAAAElFTkSuQmCC)
>
> **WARNING**
> You may need to clean your `%USERPROFILE%\.nuget\Packages` folder after you finished,
> just to make sure other apps referencing the same version of Uno won't get weird
> results.

# How to debug generators of Uno.UI.Tasks
There is a known issue for this project: Visual Studio is locking the dll file,
so you cannot easily debug it.

The easiest way to debug it is :
1. Configure the `nuget_version_override.txt` as described above 
   (If you try to debug using the Uno SampleApp you don't have to do anything as it's already configured)
1. Close/kill all your devenv.exe, msbuild.exe, and Uno.SourceGeneration.Host.exe
1. Open a new Visual studio an open only the project src\SourceGenerators\Uno.UI.Tasks\Uno.UI.Tasks.csproj
1. In the _Debug_ tab of the project settings configure those:
   * **Start external program:** `C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe`
   * **Command line argmuents:** `[PATH_TO_YOUR_PROJECT/SOLUTION_FILE] /p:Configuration=Debug`

_[PATH_TO_YOUR_PROJECT/SOLUTION_FILE] can be relative to the output folder of the Uno.UI.Tasks project, so `..\..\..\..\SamplesApp\SamplesApp.Droid\SamplesApp.Droid.csproj`
will build the Android SampleApp_
