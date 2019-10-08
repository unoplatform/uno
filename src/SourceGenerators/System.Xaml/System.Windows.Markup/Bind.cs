using Uno.Xaml;

namespace System.Windows.Markup
{
	[ContentProperty ("Path")]
	public class Bind : MarkupExtension
	{
		public Bind()
		{
		}

		public Bind(string path)
		{
			Path = path;
		}

		[ConstructorArgument ("path")]
		public string Path { get; set; }

		public override object ProvideValue (IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException (nameof(serviceProvider));
			}

			if (Path == null)
			{
				throw new InvalidOperationException ("Name property is not set");
			}

			if (!(serviceProvider.GetService (typeof (IXamlNameResolver)) is IXamlNameResolver r))
			{
				throw new InvalidOperationException ("serviceProvider does not implement IXamlNameResolver");
			}

			var ret = r.Resolve (Path) ?? r.GetFixupToken (new[] { Path }, true);

			return ret;
		}
	}
}
