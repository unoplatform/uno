using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Uno.Wasm.Bootstrap.Server;


namespace UnoAppWinUI.Server
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

			app.UseUnoFrameworkFiles();
			app.MapFallbackToFile("index.html");


			app.MapControllers();
			app.UseStaticFiles();

			app.Run();
		}
	}
}
