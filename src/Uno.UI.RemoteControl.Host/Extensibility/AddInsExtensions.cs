using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Uno.Utils.DependencyInjection;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Host.Extensibility;

public static class AddInsExtensions
{
	public static IWebHostBuilder ConfigureAddIns(this IWebHostBuilder builder, string solutionFile)
	{
		AssemblyHelper.Load(AddIns.Discover(solutionFile), throwIfLoadFailed: false);

		return builder.ConfigureServices(svc => svc.AddFromAttributes());
	}
}
