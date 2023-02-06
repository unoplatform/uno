using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Uno.Wasm.Bootstrap.Server;

namespace UnoQuickStart
{
	public sealed class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers();

			var app = builder.Build();

			// Configure the HTTP request pipeline.

			app.UseAuthorization();

#if (UseWebAssembly)
			app.UseUnoFrameworkFiles();
			app.MapFallbackToFile("index.html");
#endif

			app.MapControllers();
			app.UseStaticFiles();

			app.Run();
		}
	}
}
