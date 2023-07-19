---
uid: Uno.Development.NotImplemented
---

## The member/type is not implemented

If you're visiting this page after having clicked on a link in your application's debug log or exception view, this means you're using directly or indirectly an API that is not implemented.

Uno Platform is built around providing the full APIs provided by WinRT and WinUI, by replicating the available surface to be used in Uno Platform apps.

This WinUI and WinRT surface is very large. We covered a great portion of it since 2013, to the level of even publicly-listed enterprises deploying apps with it. To prioritize the remainder of unimplemented WinUI & WinRT API surface, we need the help of the community to add support for APIs that are marked as "Not Implemented".

Multiple directions are available:

- Make sure that you are using the latest available stable packages for Uno Platform (Uno.WinUI or Uno.UI)
- Find the API (Type, method or property) in the [Issues list](https://github.com/unoplatform/uno/issues) or [Pull Requests](https://github.com/unoplatform/uno/pulls) to vote on it with GitHub reactions, and if it's not already present, create one so the team knows you're trying to use it. Make sure to let us know your use case as it may drive priorities.
- If you're up for it, you can contribute an implementation for this API

The Uno Platform team [offers professional support](https://platform.uno/support/) to get issues sorted out faster.
