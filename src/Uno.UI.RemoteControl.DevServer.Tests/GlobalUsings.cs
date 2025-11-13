global using AwesomeAssertions;
global using Microsoft.Extensions.Logging;
global using Uno.UI.RemoteControl.Messages;

// Ensure tests are executed sequentially
[assembly: Parallelize(Workers = 1)]
