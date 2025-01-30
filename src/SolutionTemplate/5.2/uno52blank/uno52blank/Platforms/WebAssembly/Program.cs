namespace uno52blank;

public class Program
{
    private static App? _app;

    public static int Main(string[] args)
    {
        Microsoft.UI.Xaml.Application.Start(_ => _app = new App());

        return 0;
    }
}
