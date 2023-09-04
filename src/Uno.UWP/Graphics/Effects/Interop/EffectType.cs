namespace Windows.Graphics.Effects.Interop
{
	/// <summary>
	/// This enum specifies effects supported by Composition APIs<br/><br/>
	/// <remarks>
	/// References:<br/>
	///		1- <see href="https://microsoft.github.io/Win2D/WinUI2/html/N_Microsoft_Graphics_Canvas_Effects.htm"/><br/>
	///		2- wuceffects.dll
	///	</remarks>
	/// </summary>
	internal enum EffectType
	{
		// D2D1 Effects
		AlphaMaskEffect,
		ArithmeticCompositeEffect,
		BlendEffect,
		BorderEffect,
		ColorMatrixEffect,
		ColorSourceEffect,
		CompositeEffect,
		ContrastEffect,
		CrossFadeEffect,
		//DisplacementMapEffect, // Note: DisplacementMapEffect is implemented by Composition but it's not enabled yet (as of 10.0.25941.1000), uncomment this member when it gets enabled
		DistantDiffuseEffect,
		DistantSpecularEffect,
		ExposureEffect,
		GammaTransferEffect,
		GaussianBlurEffect,
		GrayscaleEffect,
		HueRotationEffect,
		InvertEffect,
		LuminanceToAlphaEffect,
		LinearTransferEffect, // Note: This is supported by Composition, docs are outdated
		OpacityEffect,
		PointDiffuseEffect,
		PointSpecularEffect,
		SaturationEffect,
		SepiaEffect,
		SpotDiffuseEffect,
		SpotSpecularEffect,
		TemperatureAndTintEffect,
		TintEffect,
		Transform2DEffect,

		// Composition-only Effects
		SceneLightingEffect,
		WhiteNoiseEffect,

		Unsupported
	}
}
