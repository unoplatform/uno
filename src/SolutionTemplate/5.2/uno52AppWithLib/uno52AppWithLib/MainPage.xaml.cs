namespace uno52AppWithLib;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        _ = new uno52lib.Class1();
    }
}
