#if __WASM__
#nullable enable

namespace Uno.Web.Http
{
	public class Cookie
	{
		public Cookie(string name, string value)
		{
			Name = name ?? throw new System.ArgumentNullException(nameof(name));
			Value = value ?? throw new System.ArgumentNullException(nameof(value));
		}

		public string Name { get; }

		public string Value { get; }
	}
}
#endif
