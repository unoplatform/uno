---
uid: Uno.Samples.Catalog
---

# Samples Catalog

This page provides a comprehensive catalog of Uno Platform samples with detailed information to help you choose the right sample for your learning needs.

> [!TIP]
> Use the tables below to quickly identify samples that match your requirements. Look for samples with **Single Project** structure and recent **.NET versions** for the best experience with current Uno Platform templates.

> [!WARNING]
> Some samples may use deprecated packages or older project structures. Check the **Notes** column for important information before using a sample.

## Featured Sample Applications

These are fully-featured sample applications that demonstrate comprehensive Uno Platform capabilities.

### Uno Chefs

**Description:** A recipe app that combines recipe browsing with interactive features, allowing users to explore step-by-step instructions, video tutorials, nutritional information, user reviews, and personalized cookbooks.

| Property | Value |
|----------|-------|
| **Repository** | [unoplatform/Uno.Chefs](https://github.com/unoplatform/Uno.Chefs) |
| **Documentation** | [Uno Chefs Overview](xref:Uno.Chefs.Overview) |
| **.NET Version** | .NET 9.0+ |
| **Uno.Sdk Version** | 5.5+ |
| **Project Structure** | Single Project |
| **Target Frameworks** | iOS, Android, WebAssembly, Windows, macOS, Linux (Skia) |
| **Key UnoFeatures** | Extensions, Navigation, Http, Localization, Hosting, MVUX, Toolkit |
| **Tutorial Available** | Yes - Step-by-step recipe guides available |
| **Tutorial Location** | Maintained in Uno Docs |
| **Status** | ✅ Active & Up-to-date |

## Workshop Projects

These workshop projects provide step-by-step guided learning experiences.

### SimpleCalc Workshop

**Description:** A calculator application that teaches fundamental Uno Platform concepts including UI design, MVVM/MVUX patterns, and cross-platform development.

| Property | Value |
|----------|-------|
| **Repository** | [unoplatform/workshops](https://github.com/unoplatform/workshops) |
| **Documentation** | [SimpleCalc Workshop](xref:Workshop.SimpleCalc.Overview) |
| **.NET Version** | .NET 9.0+ |
| **Uno.Sdk Version** | 5.5+ |
| **Project Structure** | Single Project |
| **Target Frameworks** | iOS, Android, WebAssembly, Windows, macOS, Linux |
| **Key UnoFeatures** | Extensions, Hosting, MVUX, Navigation |
| **Tutorial Available** | Yes - Comprehensive step-by-step guide |
| **Tutorial Location** | Maintained in Uno Docs |
| **Markup Options** | XAML or C# Markup |
| **Pattern Options** | MVVM or MVUX |
| **Status** | ✅ Active & Up-to-date |

### TubePlayer Workshop

**Description:** A YouTube video search and streaming application demonstrating advanced Uno Platform features including Figma import, MVUX, and C# Markup.

| Property | Value |
|----------|-------|
| **Repository** | [unoplatform/workshops](https://github.com/unoplatform/workshops) |
| **Documentation** | [TubePlayer Workshop](xref:Workshop.TubePlayer.Overview) |
| **.NET Version** | .NET 9.0+ |
| **Uno.Sdk Version** | 5.5+ |
| **Project Structure** | Single Project |
| **Target Frameworks** | iOS, Android, WebAssembly, Windows, macOS, Linux |
| **Key UnoFeatures** | Extensions, Http, MVUX, Navigation, CSharpMarkup |
| **Third-party Packages** | None (uses platform APIs) |
| **Tutorial Available** | Yes - Step-by-step with Figma option |
| **Tutorial Location** | Maintained in Uno Docs |
| **Status** | ✅ Active & Up-to-date |

### Counter Tutorial

**Description:** The essential "Hello World" of Uno Platform - a simple counter app to learn the basics.

| Property | Value |
|----------|-------|
| **Repository** | [unoplatform/workshops](https://github.com/unoplatform/workshops) |
| **Documentation** | [Counter App Tutorial](xref:Uno.Workshop.Counter) |
| **.NET Version** | .NET 9.0+ |
| **Uno.Sdk Version** | 5.5+ |
| **Project Structure** | Single Project |
| **Target Frameworks** | iOS, Android, WebAssembly, Windows, macOS, Linux |
| **Key UnoFeatures** | Extensions, Hosting |
| **Tutorial Available** | Yes - Beginner-friendly guide |
| **Tutorial Location** | Maintained in Uno Docs |
| **Markup Options** | XAML or C# Markup |
| **Pattern Options** | MVVM or MVUX |
| **Status** | ✅ Active & Up-to-date |

## Uno.Samples Repository

The [Uno.Samples repository](https://github.com/unoplatform/Uno.Samples) contains a collection of samples demonstrating specific features and scenarios. Below is a catalog of key samples with detailed information.

> [!NOTE]
> The Uno.Samples repository is actively maintained. Check the [repository README](https://github.com/unoplatform/Uno.Samples) for the complete and most up-to-date list of samples.

> [!IMPORTANT]
> Some older samples in the Uno.Samples repository may use:
> - **Multi-head project structure** (older pattern, not recommended for new projects)
> - **Deprecated packages** (see notes below for specific samples)
> - **Older .NET versions** (migration may be needed)
>
> For new projects, prefer samples marked with **Single Project** structure and recent .NET versions.

### Reference Samples

These samples demonstrate complete application scenarios:

#### Commerce (Figma-to-Uno Reference)

**Description:** A complete e-commerce application showcasing Figma design import capabilities.

| Property | Value |
|----------|-------|
| **Path** | `reference/Commerce` |
| **Documentation** | Figma Plugin documentation only |
| **.NET Version** | To be verified in repository |
| **Project Structure** | To be verified |
| **Target Frameworks** | To be verified |
| **Tutorial Available** | ⚠️ Limited - Figma import guide only |
| **Notes** | Lacks comprehensive Hot Design/Hot Reload tutorial |

#### ToDo App

**Description:** A task management application demonstrating CRUD operations and data persistence.

| Property | Value |
|----------|-------|
| **Path** | `reference/ToDo` |
| **External Link** | [Community ToDo Tutorial](https://steven-giesel.com/blogPost/2c025ac6-d67f-45ec-a616-009e0285c999) |
| **.NET Version** | To be verified in repository |
| **Project Structure** | ⚠️ Multi-head (external), To be verified (repository version) |
| **Tutorial Available** | ⚠️ External only |
| **Tutorial Location** | External blog (may be outdated) |
| **Notes** | Repository version needs comprehensive tutorial |

#### Time Entry Sample

**Description:** Time tracking application with identity server authentication.

| Property | Value |
|----------|-------|
| **Path** | Related to Silverlight migration |
| **.NET Version** | To be verified |
| **Project Structure** | To be verified |
| **Third-party Packages** | ⚠️ IdentityServer4 (EOL since December 2022) |
| **Status** | ⚠️ **DEPRECATED PACKAGE** |
| **Migration Required** | Yes - Should migrate to Duende IdentityServer |
| **Notes** | **Critical:** IdentityServer4 is end-of-life. Not recommended for new projects. |

#### Authentication.OidcDemo

**Description:** OpenID Connect authentication demonstration.

| Property | Value |
|----------|-------|
| **Path** | `UI/Authentication.OidcDemo` |
| **Project Structure** | ⚠️ Multi-head (older pattern) |
| **Third-party Packages** | ⚠️ IdentityModel.OidcClient (deprecated) |
| **Status** | ⚠️ **USES DEPRECATED PACKAGES** |
| **Alternative** | Use [Authentication.Oidc Extension](xref:Uno.Extensions.Authentication.Oidc) instead |
| **Notes** | **Deprecated:** Use Uno.Extensions.Authentication.Oidc for new projects |

#### Food Delivery

**Description:** A food delivery application demonstrating UI/UX patterns.

| Property | Value |
|----------|-------|
| **Path** | To be verified in Uno.Samples |
| **Description Status** | ⚠️ Missing description in current docs |
| **.NET Version** | To be verified |
| **Project Structure** | To be verified |
| **Documentation** | Needs description added |

## Legacy/Migration Samples

> [!CAUTION]
> The samples in this section are provided for **migration purposes only** and may contain significantly outdated patterns, deprecated packages, and older project structures. These are NOT recommended for new projects.

### Silverlight Migration Samples

**Description:** Samples demonstrating migration from Silverlight to Uno Platform.

| Property | Value |
|----------|-------|
| **Documentation** | [Silverlight Migration Guide](xref:Uno.SilverlightMigration.Landing) |
| **.NET Version** | ⚠️ Outdated |
| **Project Structure** | ⚠️ Legacy multi-head |
| **Status** | ⚠️ **MIGRATION REFERENCE ONLY** |
| **Notes** | **Warning:** Significantly outdated. For historical reference and migration context only. |

> [!WARNING]
> **Silverlight Migration Documentation Status:**
> The Silverlight migration documentation is outdated and should NOT be used as a reference for current Uno Platform development. It is maintained only for teams actively migrating from legacy Silverlight applications.

## How to Use This Catalog

### For Beginners 🚀

Start with samples marked:
- ✅ **Active & Up-to-date** status
- **Single Project** structure
- **.NET 9.0+** version
- **Tutorial Available: Yes**

**Recommended starting points:**
1. [Counter Tutorial](xref:Uno.Workshop.Counter) - Learn the basics
2. [SimpleCalc Workshop](xref:Workshop.SimpleCalc.Overview) - Build skills
3. [Uno Chefs](xref:Uno.Chefs.Overview) - See real-world patterns

### For Experienced Developers 💼

Look for samples that:
- Match your specific **UnoFeatures** needs
- Use your preferred **markup** (XAML or C# Markup)
- Target your required **platforms**

### Understanding Project Structure

- **Single Project:** Modern template structure (recommended). All platform targets in one project file.
- **Multi-head:** Older structure with separate projects per platform. Still works but not recommended for new projects.

### Checking for Outdated Content ⚠️

Before using any sample, check:
1. **Status** column - Look for warnings
2. **Third-party Packages** - Check for deprecation warnings
3. **Project Structure** - Prefer Single Project
4. **.NET Version** - Use .NET 9.0 or later for new projects
5. **Tutorial Location** - Maintained in Uno Docs is more likely to be current

## Need More Information?

For detailed information about any sample:

1. **Check the repository** - Most samples have detailed READMEs
2. **Review the documentation** - Follow the documentation links in the tables
3. **Ask the community** - Visit [Uno Platform Discord](https://platform.uno/discord) or [GitHub Discussions](https://github.com/unoplatform/uno/discussions)

## Contributing Sample Information

Have information about a sample that's missing from this catalog? Please contribute!

1. Check the sample repository for accurate information
2. Submit a pull request updating this page
3. Follow the [Conventional Commits](xref:Uno.Contributing.ConventionalCommits) guidelines

---

**See also:**
- [Samples & Tutorials Overview](xref:Uno.SamplesTutorials.Overview)
- [Next Steps in Learning](xref:Uno.GetStarted.NextSteps)
- [Community Tutorials](xref:Uno.Tutorials.CommunityTutorials)
