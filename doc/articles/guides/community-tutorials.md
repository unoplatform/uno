---
uid: Uno.Tutorials.CommunityTutorials
---

# Community Tutorials

This page lists links to our community provided tutorials with detailed information to help you assess their relevance and currency.

> [!NOTE]
> Please use this page to submit links to your blogs, articles, and other resources as community contribution to Uno Platform Advocacy.

> [!IMPORTANT]
> **About External Resources:**
> Community tutorials are external resources that may not reflect the latest Uno Platform features or patterns. Always check:
> - **Publication date** - Recent content is more likely to be current
> - **Project structure** - Look for Single Project templates (introduced in Uno Platform 5.0+)
> - **.NET version** - Prefer .NET 8.0+ content
> 
> For the most up-to-date tutorials, see the [official Uno Platform workshops and samples](xref:Uno.Samples.Catalog).

## Articles and Blog Posts

### Getting Started Guides

#### A Quick Look at Uno Platform Development

**Author:** James D. McCaffrey  
**Link:** [jamesmccaffrey.wordpress.com](https://jamesmccaffrey.wordpress.com/2021/05/31/a-quick-look-at-uno-platform-development/)  
**Published:** May 2021  
**Type:** Overview/Introduction  
**Status:** ⚠️ May contain outdated patterns (3+ years old)

#### Uno Platform Getting Started Series

**Author:** Skye Hoefling (Microsoft MVP)  
**Link:** [andrewhoefling.com](https://www.andrewhoefling.com/Blog/Post/uno-platform-getting-started-series)  
**Type:** Multi-part tutorial series  
**Topics Covered:**
- New project creation
- View Stack Navigation
- Tabbed Pages with Command Bar
- Platform-specific XAML
- Platform-specific C#
- MVVM and Dependency Injection
- ViewModelLocator patterns
- WASM JavaScript interop

**Key Articles:**
1. [Getting Started with New Projects](https://www.andrewhoefling.com/Blog/Post/uno-platform-getting-started-with-new-projects)
2. [View Stack Navigation](https://www.andrewhoefling.com/Blog/Post/uno-platform-view-stack-navigation-uwp-android-ios-wasm)
3. [Tabbed Pages with Command Bar](https://www.andrewhoefling.com/Blog/Post/uno-platform-tabbed-pages-with-the-command-bar-uwp-ios-android-wasm)
4. [Platform Specific XAML](https://www.andrewhoefling.com/Blog/Post/platform-specific-xaml-in-uno-platform-ios-android-wasm-uwp)
5. [Platform Specific C#](https://www.andrewhoefling.com/Blog/Post/platform-specific-c-sharp-in-uno-platform-ios-android-wasm-uwp)
6. [MVVM and Dependency Injection](https://www.andrewhoefling.com/Blog/Post/mvvm-and-dependency-injection-in-uno-platform-ios-android-wasm-uwp)
7. [ViewModelLocator for MVVM](https://www.andrewhoefling.com/Blog/Post/view-model-locator-for-mvvm-applications-in-uno-platform)
8. [WASM Invoke JavaScript](https://www.andrewhoefling.com/Blog/Post/uno-platform-wasm-invoke-javascript)

**Status:** ⚠️ May contain outdated patterns - Check for newer alternatives

#### Introduction to Uno Platform Series

**Author:** Ronica Singh  
**Link:** [ronicasingh.hashnode.dev](https://ronicasingh.hashnode.dev/)  
**Type:** Multi-part beginner series  

**Articles:**
- [Introduction to Uno Platform](https://ronicasingh.hashnode.dev/introduction-to-uno-platform)
- [Creating your First Uno Platform Application](https://ronicasingh.hashnode.dev/creating-your-first-uno-platform-application)
- [Uno Platform and its Fluent app design](https://ronicasingh.hashnode.dev/uno-platform-and-its-fluent-app-design)

**Status:** Check publication dates for currency

### Application Development Tutorials

#### Building a Todo App with Uno Platform

**Author:** Steven Giesel ([linkdotnet](https://github.com/linkdotnet))  
**Link:** [steven-giesel.com](https://steven-giesel.com/)  
**Type:** Complete application tutorial (5-part series)  
**Project:** Multi-platform Todo application

**Articles:**
- [Part 1 - Introduction and Environment Setup](https://steven-giesel.com/blogPost/b2234ada-0978-4c7b-841e-ca6a255247b0)
- [Part 2 - Requirements & First Features](https://steven-giesel.com/blogPost/85814db0-3495-492c-8ce1-5c83d708590b)
- [Part 3 - Dialog Component & ViewModel](https://steven-giesel.com/blogPost/a3179d55-d5be-48ba-b570-ee7d494a8b21)
- [Part 4 - Adding Elements to the Swimlane](https://steven-giesel.com/blogPost/2d96d970-ef11-48f4-a102-9339fc362a75)
- [Part 5 - Implementing Drag and Drop Behavior](https://steven-giesel.com/blogPost/2c025ac6-d67f-45ec-a616-009e0285c999)

> [!NOTE]
> This tutorial series covers comprehensive app development including UI components, MVVM patterns, and drag-and-drop functionality.

**Status:** ⚠️ External resource - verify against current Uno Platform patterns

### Platform-Specific Tutorials

#### Uno Platform on Surface Duo

**Author:** Craig Dunn (Microsoft)  
**Link:** [devblogs.microsoft.com/surface-duo](https://devblogs.microsoft.com/surface-duo/tag/uno-platform/)  
**Type:** Platform-specific guide  
**Platform:** Surface Duo (dual-screen Android device)

**Status:** ⚠️ Check for current Surface Duo SDK compatibility

#### Raspberry Pi Applications with Uno Platform

**Author:** Peter Gallagher  
**Link:** [petecodes.co.uk](https://www.petecodes.co.uk/developing-uwp-apps-for-the-raspberry-pi-with-uno-platform/)  
**Type:** Platform-specific deployment guide  
**Platform:** Raspberry Pi (Linux/Embedded)

**See also:** [Official Raspberry Pi Documentation](xref:Uno.RaspberryPi.Intro)

### Books and Courses

For information about books and paid courses, see the [Next Steps in Learning](xref:Uno.GetStarted.NextSteps) page.

## Tips for Using External Tutorials

### Assessing Tutorial Currency ⏰

**Check these indicators:**
- **Publication date** - Content older than 1 year may need adaptation
- **Uno Platform version mentioned** - Look for version 5.0+ (Single Project templates)
- **.NET version** - Prefer .NET 8.0+ tutorials
- **Project structure** - Multi-head projects indicate older patterns

### Adapting Older Content 🔄

If you're following an older tutorial:

1. **Use current templates** - Start with `dotnet new unoapp` for Single Project structure
2. **Check for package updates** - Older tutorials may use deprecated packages
3. **Refer to migration guides** - See [Migration Guidance](xref:Uno.MigratingGuidance) for pattern updates
4. **Ask the community** - Join [Uno Platform Discord](https://platform.uno/discord) for help adapting older content

### Reporting Issues 🐛

If you find issues with external tutorials:
- **Contact the author** - Most authors appreciate feedback
- **Share updated approaches** - Help the community by posting solutions
- **Update this page** - Submit a PR with status notes

## Contributing Your Tutorial

Have you written a tutorial about Uno Platform? We'd love to include it!

### Submission Guidelines

When adding your tutorial to this page, please include:

- **Direct link** to your content
- **Your name** and any relevant credentials
- **Publication date** (or "Updated: [date]" for maintained content)
- **Brief description** of what the tutorial covers
- **Prerequisites** if any (e.g., "Requires paid Pluralsight subscription")
- **Uno Platform version** if specified in your content
- **.NET version** if specified

### Example Format

```markdown
#### [Your Tutorial Title]

**Author:** Your Name  
**Link:** [your-site.com](https://your-site.com/tutorial)  
**Published:** Month Year  
**Type:** Tutorial/Guide/Course  
**Topics:** Brief description of topics covered  
**Uno Platform Version:** 5.0+ (if specified)  
**.NET Version:** .NET 8.0+ (if specified)  
```

### How to Submit

1. Fork the [Uno repository](https://github.com/unoplatform/uno)
2. Edit this file: `doc/articles/guides/community-tutorials.md`
3. Add your entry in the appropriate section
4. Follow [Conventional Commits](xref:Uno.Contributing.ConventionalCommits) guidelines
5. Submit a pull request

_Create a PR to add yours here!_

## See Also

- [Official Samples Catalog](xref:Uno.Samples.Catalog) - Maintained samples with detailed metadata
- [Samples & Tutorials Overview](xref:Uno.SamplesTutorials.Overview) - Complete learning resources
- [Next Steps in Learning](xref:Uno.GetStarted.NextSteps) - Books, courses, and official resources
- [Contributing Guidelines](xref:Uno.Contributing.Intro) - How to contribute to Uno Platform
