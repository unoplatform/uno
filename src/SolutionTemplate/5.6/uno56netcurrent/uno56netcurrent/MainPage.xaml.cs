namespace uno56netcurrent;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        DataContext = new MainPageViewModel();
    }
}

[Microsoft.UI.Xaml.Data.Bindable]
public class MainPageViewModel
{
    public HelloModel HelloModel { get; } = new HelloModel(new HelloEntity("Hello Uno Platform!"));
}

public record HelloModel(HelloEntity HelloEntity);

public record HelloEntity(string Text);
