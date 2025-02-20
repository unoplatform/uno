---
uid: Uno.Features.Cookies
---

# Working with cookies

In the case of WebAssembly, you may need to work with the browser cookies to either read or set values with expiration support. For these purposes, Uno Platform provides the `Uno.Web.Http.CookieManager` class.

## Prerequisites

All types related to cookie management are WebAssembly-specific and reside in the `Uno.Web.Http` namespace. When writing code which uses these types, you must wrap it in `#if __WASM__` blocks and either fully qualify these types (e.g. `Uno.Web.Http.Cookie` or `Uno.Web.Http.SetCookieRequest`) or add a using statement to the top of the file, wrapped within `#if`, as follows:

```csharp
#if __WASM__
using Uno.Web.Http;
#endif
```

## Setting a cookie

To set a cookie, create an instance of `Uno.Web.Http.SetCookieRequest` and initialize the values you are interested in. The following cookie attributes are supported:

- Name
- Value
- Path
- Domain
- Max-age
- Expires
- Secure
- SameSite

Then set the cookie using the `SetCookie` method.

```csharp
#if __WASM__

var cookie = new Cookie("MyCookie", "somevalue");
var request = new SetCookieRequest(cookie)
{
    Path = "/",
    Expires = DateTimeOffset.UtcNow.AddDays(2),
    Secure = true,
};

CookieManager.GetDefault().SetCookie(request);

#endif

```

> [!NOTE]
> If your cookie contains special characters, you may use `Uri.EscapeDataString` to escape those characters when setting the cookie and later use `Uri.UnescapeDataString` to decode them.

## Getting active cookies

To retrieve the active cookies, use the `GetCookies` method:

```csharp
#if __WASM__

var cookies = CookieManager.GetDefault().GetCookies();
foreach (var cookie in cookies)
{
    Debug.WriteLine(cookie.Name);
    Debug.WriteLine(cookie.Value);
}

#endif

```

> [!NOTE]
> Browser only provides the `Name` and `Value` properties for reading. Other attributes can be only used as part of the `SetCookie` request.

## Deleting a cookie

To delete a cookie, provide its `Name` and optionally the `Domain` and `Path` to the `DeleteCookie` method:

```csharp
#if __WASM__

CookieManager.GetDefault().DeleteCookie("MyCookie", path: "/");

#endif
```
