#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Media.Media3D;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf;

namespace Uno.UI.Runtime.Skia;

internal class WpfHostBuilder : IPlatformHostBuilder, IWindowsSkiaHostBuilder
{
	private Func<Application>? _wpfApplication;
	private string? _windowsDesktopFrameworkPath;

	public WpfHostBuilder()
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			RegisterAssemblyResolver();
		}
	}

	private static void AdjustPath(string dotnetSharedDirectory, string runtimeVersion, ref string windowsDesktopFrameworkPath)
	{
		// In at least preview SDKs, the .NET runtime version is different from the .NET Desktop runtime version for the same .NET SDK.
		// For example, .NET SDK 9.0.100-preview.4 has these runtimes:
		// .NET Runtime 9.0.0-preview.4.24266.19
		// ASP.NET Core Runtime 9.0.0-preview.4.24267.6
		//.NET Desktop Runtime 9.0.0-preview.4.24267.11
		// This is a quite hacky to get to the right path where we don't exactly know the version beforehand.
		var indexOfPreview = runtimeVersion.IndexOf("-preview", StringComparison.Ordinal);
		if (indexOfPreview > -1)
		{
			var mainVersion = runtimeVersion.Substring(0, indexOfPreview);
			var directories = Directory.GetDirectories(Path.GetFullPath(Path.Combine(dotnetSharedDirectory, "Microsoft.WindowsDesktop.App")));
			foreach (var directory in directories)
			{
				var possibleVersion = Path.GetFileName(directory);
				var possibleIndexOfPreview = possibleVersion?.IndexOf("-preview", StringComparison.Ordinal) ?? -1;
				if (possibleIndexOfPreview > -1)
				{
					var desktopRuntimeVersion = possibleVersion!.Substring(0, possibleIndexOfPreview);
					windowsDesktopFrameworkPath = Path.GetFullPath(Path.Combine(
						dotnetSharedDirectory,
						"Microsoft.WindowsDesktop.App",
						possibleVersion));
					break;
				}
			}
		}
	}

	private void RegisterAssemblyResolver()
	{
		AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;

		if (Uri.TryCreate(typeof(object).Assembly.Location, UriKind.RelativeOrAbsolute, out var uri))
		{
			// We expect that the path from System.Private.CoreLib.dll:
			//
			// file:///C:/Program Files/dotnet/shared/Microsoft.NETCore.App/8.0.2/System.Private.CoreLib.dll
			//
			// Will be the in a sibling folder from the Microsoft.WindowsDesktop.App using the same version.
			// If the structure of the .NET installation changes, this code will need to be updated.
			var version = Path.GetFileName(Path.GetDirectoryName(uri.LocalPath))!;
			var dotnetShared = Path.Combine(Path.GetDirectoryName(uri.LocalPath)!, "..", "..");

			_windowsDesktopFrameworkPath = Path.GetFullPath(Path.Combine(
				dotnetShared,
				"Microsoft.WindowsDesktop.App",
				version));

			if (!Directory.Exists(_windowsDesktopFrameworkPath))
			{
				AdjustPath(dotnetShared, version, ref _windowsDesktopFrameworkPath);
			}

			if (!Directory.Exists(_windowsDesktopFrameworkPath))
			{
				throw new InvalidOperationException($"Unable to find the Microsoft.WindowsDesktop.App framework. An Uno Platform update may be required (Expected path {_windowsDesktopFrameworkPath})");
			}

			// Explicitly load WindowsBase to avoid later incorrect loading issues with this message:
			//
			//	System.IO.FileNotFoundException: Could not load file or assembly 'WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'. The system cannot find the file specified.
			//  HRESULT 0x80070002
			//
			AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(_windowsDesktopFrameworkPath, "WindowsBase.dll"));

			// Setup native images lookup in order for NativeLibrary.TryLoad to succeed
			var registration = NativeMethods.AddDllDirectory(_windowsDesktopFrameworkPath);

			LoadNativeImage("vcruntime140_cor3.dll");
			LoadNativeImage("PresentationNative_cor3.dll");

			if (RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86)
			{
				// This image is not needed when running on arm64 on windows.
				LoadNativeImage("wpfgfx_cor3.dll");
			}
		}
	}

	private static void LoadNativeImage(string image)
	{
		if (!NativeLibrary.TryLoad(
					image,
					typeof(WpfHostBuilder).Assembly,
					DllImportSearchPath.UserDirectories,
					out _))
		{
			throw new InvalidOperationException($"Failed to load native image. An Uno Platform update may be required. (Tried loading {image})");
		}
	}

	private Assembly? OnAssemblyResolving(AssemblyLoadContext context, AssemblyName assemblyName)
	{
		if (_windowsDesktopFrameworkPath is not null)
		{
			var assemblyPath = Path.Combine(_windowsDesktopFrameworkPath, assemblyName.Name + ".dll");

			if (File.Exists(assemblyPath))
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Resolving {assemblyName} with {assemblyPath}");
				}

				try
				{
					return context.LoadFromAssemblyPath(assemblyPath);
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						// Only log loading errors in trace level, some are false positives.
						this.Log().Error($"Failed to load {assemblyPath} for {assemblyName}", e);
					}
				}
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Unable to find file {assemblyPath} for {assemblyName}");
				}
			}
		}

		return null;
	}

	public bool IsSupported
		=> OperatingSystem.IsWindows();

	Func<Application>? IWindowsSkiaHostBuilder.WpfApplication
	{
		get => _wpfApplication;
		set => _wpfApplication = value;
	}

	public SkiaHost Create(Func<Windows.UI.Xaml.Application> appBuilder)
		=> new WpfHost(appBuilder, _wpfApplication);

	private static class NativeMethods
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int AddDllDirectory(string NewDirectory);
	}
}
