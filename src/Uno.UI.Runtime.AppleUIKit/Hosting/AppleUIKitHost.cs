using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.UI.Hosting;
using Uno.UI.Runtime;
using Uno.UI.Runtime.AppleUIKit.Extensions;

namespace Uno.UI.Runtime.AppleUIKit;

internal class AppleUIKitHost : UnoPlatformHost, IApplicationHost
{
	private readonly Func<Application> _appBuilder;
	private readonly Type? _uiApplicationDelegateOverride;

	/// <summary>
	/// Creates a host for an Uno Skia iOS/tvOS application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// NSProcessInfo.Arguments is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public AppleUIKitHost(Func<Application> appBuilder, Type? uiApplicationDelegateOverride)
	{
		_appBuilder = appBuilder ?? throw new ArgumentNullException(nameof(appBuilder));
		_uiApplicationDelegateOverride = uiApplicationDelegateOverride;
	}

	internal static ApplicationInitializationCallback? CreateAppAction { get; private set; }

	protected override void Initialize()
	{
		AccessibilityRouter.EnsureInitialized();
		ExtensionsRegistrar.Register();
	}

	protected override Task RunLoop()
	{
		try
		{
			CreateAppAction = (ApplicationInitializationCallbackParams _) =>
			{
				var app = _appBuilder.Invoke();
				app.Host = this;
			};

			// The .NET iOS runtime does not forward argv to Environment.GetCommandLineArgs().
			if (NSProcessInfo.ProcessInfo.Arguments is { Length: > 1 } processArgs)
			{
				Application.SetArguments(PasteArguments(processArgs));
			}

			var delegateType = _uiApplicationDelegateOverride ?? typeof(UnoUIApplicationDelegate);

			UIApplication.Main(Environment.GetCommandLineArgs(), null, delegateType);

			return Task.CompletedTask;
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");

			throw;
		}
	}

	/// <summary>
	/// Joins argv (skipping the executable) using the same quoting rules .NET uses
	/// to build Environment.CommandLine on Unix, so the result matches desktop.
	/// </summary>
	internal static string PasteArguments(string[] args)
	{
		StringBuilder sb = new();
		for (var i = 1; i < args.Length; i++)
		{
			if (i > 1)
			{
				sb.Append(' ');
			}

			AppendArgument(sb, args[i]);
		}

		return sb.ToString();
	}

	private static void AppendArgument(StringBuilder sb, string argument)
	{
		if (argument.Length != 0 && !ContainsWhitespaceOrQuote(argument))
		{
			sb.Append(argument);
			return;
		}

		sb.Append('"');
		var idx = 0;
		while (idx < argument.Length)
		{
			var c = argument[idx++];
			if (c == '\\')
			{
				var backslashes = 1;
				while (idx < argument.Length && argument[idx] == '\\')
				{
					idx++;
					backslashes++;
				}

				if (idx == argument.Length)
				{
					// Double them so the closing quote is not escaped.
					sb.Append('\\', backslashes * 2);
				}
				else if (argument[idx] == '"')
				{
					sb.Append('\\', backslashes * 2 + 1).Append('"');
					idx++;
				}
				else
				{
					sb.Append('\\', backslashes);
				}

				continue;
			}

			if (c == '"')
			{
				sb.Append('\\').Append('"');
				continue;
			}

			sb.Append(c);
		}

		sb.Append('"');
	}

	private static bool ContainsWhitespaceOrQuote(string s)
	{
		foreach (var c in s)
		{
			if (char.IsWhiteSpace(c) || c == '"')
			{
				return true;
			}
		}

		return false;
	}
}
