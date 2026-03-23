namespace Uno.UI.DevServer.Cli.Helpers;

internal static class AssemblyVersionHelper
{
	public static string GetAssemblyVersion(System.Reflection.Assembly assembly)
	{
		var attr = assembly
			.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
			.OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
			.FirstOrDefault();

		if (attr is not null)
		{
			var parts = attr.InformationalVersion.Split('+', StringSplitOptions.RemoveEmptyEntries);
			return parts[0];
		}

		return assembly.GetName().Version?.ToString() ?? "0.0.0";
	}
}
