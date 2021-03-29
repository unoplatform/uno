#if __WASM__
namespace Uno.Web.Http
{
	public enum CookieSameSite
    {
        Lax,
		Strict,
		None
    }
}
#endif
