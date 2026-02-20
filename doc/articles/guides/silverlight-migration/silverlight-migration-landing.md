---
uid: Uno.SilverlightMigration.Landing
---

# Migrating from Silverlight to WinUI and Uno Platform Documentation

> [!CAUTION]
> **This documentation is significantly outdated and maintained for historical reference only.**
> 
> This Silverlight migration guide was created in 2021 and reflects patterns, tools, and approaches from that time. **It should NOT be used as a reference for current Uno Platform development.**
> 
> **Key Outdated Elements:**
> - Uses **older project structures** (multi-head instead of Single Project)
> - References **deprecated packages** (e.g., IdentityServer4 which reached EOL in December 2022)
> - Based on **older .NET versions** and Uno Platform versions
> - Does not reflect **current best practices** or modern Uno Platform features
> 
> **If you are migrating from Silverlight:**
> 1. Use this guide only as a **high-level conceptual reference** for migration patterns
> 2. Consult [current Uno Platform documentation](xref:Uno.GetStarted) for up-to-date implementation approaches
> 3. Use [current project templates](xref:Uno.GettingStarted.CreateAnApp.VS2022) (Single Project structure)
> 4. Check the [samples catalog](xref:Uno.Samples.Catalog) for modern examples
> 5. Ask for help on [Uno Platform Discord](https://platform.uno/discord) or [GitHub Discussions](https://github.com/unoplatform/uno/discussions)
>
> **For new Uno Platform projects:** Start with the [Counter Tutorial](xref:Uno.Workshop.Counter) and [Getting Started guides](xref:Uno.GetStarted) instead.

Silverlight will reach the end of support  on October 12, 2021. As luck would have it, a new Windows UI framework is about to RTM in March 2021 – WinUI – the modern native UI platform of Windows. And with WinUI launch there is a renewed desire by C# and XAML developers to write single codebase applications for Windows and the Web.

Enter Uno Platform. Hello “Rich Internet Applications (RIA)” again!

There are a number of pages of Silverlight migration documentation to help you with migration – accompanying source code, techniques and considerations you need to make when migrating your Silverlight application.

## Table of contents

* [Silverlight to Uno Migration Introduction](00-overview.md)
* [Create the Uno solution for UWP and WASM](01-create-uno-solution.md)
* [Considering navigation – comparing Silverlight and UWP navigation](02-considering-navigation.md)
* [Reviewing the app startup – comparing Silverlight and UWP app lifecycle](03-review-app-startup.md)
* [Migrating the home page XAML and styles – comparing XAML](04-migrate-home-page-xaml-and-styles.md)
* [Switching to string resources – comparing the Silverlight and UWP approach to resources](05-string-resources.md)
* [Dialogs and errors – Silverlight ChildWindow to UWP ContentDialog and console logging](07-dialogs.md)
* [Data access services – high-level review of alternatives to WCF RIA Services, data and authentication](08-data-access-overview.md)
* [Client Authentication – securing access to data APIs](09-client-auth-service.md)
* [Implementing a singleton token service – pattern for implementing singletons and creating a token service to secure data APIs](10-implementing-singleton-token-service.md)
* [Implementing an identity service client – implementing an authentication service](11-implementing-identity-service-client.md)
* [Migrating the authentication UI – migrating the Silverlight login UI, alternative to Silverlight DataForm](12-migrate-auth-ui.md)
* [Integrating authentication and navigation – WIP – will migrate the role check before navigating](13-integrating-auth-and-navigation.md)
* [Implement the time entry service – WIP – will implement the Time Entry Service client](14-implement-timeentry-services.md)
* [Migrating the time entry UI – WIP – will look at alternatives to the Silverlight DataGrid](15-migrate-timeentry-ui.md)
* [Wrap-up – WIP – will summarize the key migration activities](20-wrap-up.md)
* [The TimeEntry Sample apps – overview of sample apps and build instructions](98-timeentry-samples.md)
* [Useful resources](99-useful-resources.md)

## Next unit: Silverlight to Uno Migration

[![button](assets/NextButton.png)](00-overview.md)
