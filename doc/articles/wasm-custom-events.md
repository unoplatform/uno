# (Wasm) Handling custom HTML events

There is a specification in HTML to create custom DOM events.

It could be useful to intercept those events in managed code.
To reach this, you need to register to those events.

## Creating an Event or a CustomEvent

In javascript (or Typescript), you can send a custom event
using the following code:

``` javascript
    // Generate a custom generic event from Javascript/Typescript
    htmlElement.dispatchEvent(new Event("simpleEvent"));

    // Generate a custom event with a string payload
    const payload = "this is the payload of the event";
    htmlElement.dispatchEvent(new CustomEvent("stringEvent", {detail: payload}));

    // Generate a custom event with a complex payload
    const payload = { property:"value", property2: 1234 };
    htmlElement.dispatchEvent(new CustomEvent("complexEvent", {detail: payload}));
```

## Registering an event handler in C# for those events

``` csharp
    protected override void OnLoaded()
    {
        // Note: following extensions are in the namespace "Uno.Extensions"
        this.RegisterHtmlEventHandler("simpleEvent", OnSimpleEvent);
        this.RegisterHtmlCustomEventHandler("stringEvent", OnStringEvent, isDetailJson: false);
        this.RegisterHtmlCustomEventHandler("complexEvent", OnComplexEvent, isDetailJson: true);
    }

    private void OnSimpleEvent(object sender, EventArgs e)
    {
        // You can react on "simpleEvent" here
    }

    private void OnStringEvent(object sender, HtmlCustomEventArgs e)
    {
        // You can react on "stringEvent" here
        // The payload is available on e.Detail
    }

    private void OnComplexEvent(object sender, HtmlCustomEventArgs e)
    {
        // You can react on "complexEvent" here
        // The payload is available on e.Detail as a JSON string
    }
```
