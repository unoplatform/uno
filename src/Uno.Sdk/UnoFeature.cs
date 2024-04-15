namespace Uno.Sdk;

public enum UnoFeature
{
	Invalid = -1,

	[UnoArea(UnoArea.Core)]
	Maps,

	[UnoArea(UnoArea.Core)]
	Foldable,

	[UnoArea(UnoArea.Core)]
	MediaElement,

	[UnoArea(UnoArea.CSharpMarkup)]
	CSharpMarkup,

	[UnoArea(UnoArea.Extensions)]
	Extensions,

	[UnoArea(UnoArea.Extensions)]
	Authentication,

	[UnoArea(UnoArea.Extensions)]
	AuthenticationMsal,

	[UnoArea(UnoArea.Extensions)]
	AuthenticationOidc,

	[UnoArea(UnoArea.Extensions)]
	Configuration,

	[UnoArea(UnoArea.Extensions)]
	ExtensionsCore,

	[UnoArea(UnoArea.Extensions)]
	Hosting,

	[UnoArea(UnoArea.Extensions)]
	Http,

	[UnoArea(UnoArea.Extensions)]
	Localization,

	[UnoArea(UnoArea.Extensions)]
	Logging,

	[UnoArea(UnoArea.Extensions)]
	MauiEmbedding,

	[UnoArea(UnoArea.Extensions)]
	Mvux,

	[UnoArea(UnoArea.Extensions)]
	Navigation,

	[UnoArea(UnoArea.Extensions)]
	Serilog,

	[UnoArea(UnoArea.Extensions)]
	Storage,

	[UnoArea(UnoArea.Extensions)]
	Serialization,

	[UnoArea(UnoArea.Toolkit)]
	Toolkit,

	[UnoArea(UnoArea.Theme)]
	Material,

	[UnoArea(UnoArea.Theme)]
	Cupertino,

	Dsp,

	Mvvm,

	Prism,

	[UnoArea(UnoArea.Core)]
	Skia,

	[UnoArea(UnoArea.Core)]
	Lottie,

	[UnoArea(UnoArea.Core)]
	Svg
}
