namespace uno53AppWithLib;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        _ = new uno53lib.Class1();
    }
}
