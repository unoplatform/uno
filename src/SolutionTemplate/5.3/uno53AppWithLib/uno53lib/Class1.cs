namespace uno53lib;

public class Class1
{
    public Class1()
    {
#if __WASM__
        // Libraries should always get a reference to Uno.WinUI.Runtime.WebAssembly
        typeof(global::Uno.UI.Runtime.WebAssembly.HtmlElementAttribute).ToString();
#endif
    }
}

