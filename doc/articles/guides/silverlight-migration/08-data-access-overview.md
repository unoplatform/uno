---
uid: Uno.SilverlightMigration.DataAccessOverview
---

# Data access services

The Silverlight Business Application template utilizes a WCF RIA Services backend project to deliver the Silverlight application via a static or a dynamic page, and to provide access to services. These services use WCF (Windows Communication Foundation) and a custom binding due to restrictions in the network protocols that can be accessed from the browser at that time. There are two types of service that are supported:

* **WCF Data Service** - a standard WCF service (albeit with a custom binding). Although, typically used for simple services and results that didn't directly map to database entities, they required a lot of code to create CRUD operations, required duplication of code to keep server and client in sync, etc. WCF RIA Services and the **Domain Services** were developed to automate much of the  tedium.
* **Domain Service** - a domain service is a key part of a WCF RIA services project and is used to expose data from the server and consume it on a Silverlight client. Through a combination of code-generation and sharing source files, business and validation logic are shared between the server and client, substantially reducing the effort required to develop applications.

The following services related to authentication are included in the template by default:

* **AuthenticationService** - a service that validates a user name and password and returns the logged in user **IPrincipal** via the ASP.NET Forms Authentication implementation.

    > [!TIP]
    > This can be customized to use data defined in a custom database as demonstrated in the **TimeEntryRia** sample.

* **UserRegistrationService** - a service that allowed new users to register themselves.

    > [!NOTE]
    > This made more sense for consumer style applications - enterprise applications typically manage user accounts via administrators.

Unfortunately, WCF itself has been discontinued, although there are, of course, alternatives such as ASP.NET Core Web APIs, gRPC, an open source version of WCF, etc. There is no direct replacement for WCF RIA services, although some scaffolding solutions exist to reduce the amount of boilerplate code required for CRUD operations.

> [!TIP]
> In the sample migration of TimeEntryRIA to Uno, ASP.NET Core Web APIs were used. You can find a full article discussing how to consume a web service with Uno below:
>
> * [How to consume a web service](/articles/howto-consume-webservices.md)
>
> [!NOTE]
> You can learn more about alternate service technologies here:
>
> * [Create web APIs with ASP.NET Core](https://learn.microsoft.com/aspnet/core/web-api/?view=aspnetcore-5.0)
> * [Introduction to gRPC on .NET](https://learn.microsoft.com/aspnet/core/grpc/?view=aspnetcore-5.0)
> * [Core WCF](https://github.com/CoreWCF/CoreWCF)

In the sample application migration, REST-based services using ASP.NET Core Web APIs were chosen.

## Next unit: Client authentication service

[![button](assets/NextButton.png)](09-client-auth-service.md)
