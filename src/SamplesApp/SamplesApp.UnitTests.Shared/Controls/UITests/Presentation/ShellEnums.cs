namespace SampleControl.Presentation;

/// <summary>The shell area the user is looking at; drives the rail selection.</summary>
public enum ShellDestination
{
	Home,
	Samples,
	RuntimeTests,
	Benchmarks,
	Playground,
	Help,
	Settings,
}

/// <summary>The view shown inside the sample browser pane.</summary>
public enum BrowserView
{
	Samples,
	Settings,
}

/// <summary>What the shell opens when the app starts without a deep link.</summary>
public enum StartupPage
{
	Home,
	Playground,
	LastSample,
}
