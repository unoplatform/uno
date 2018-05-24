#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PrintMediaSize 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PrinterCustom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BusinessCard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CreditCard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA0,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA3Extra,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA3Rotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA4Extra,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA4Rotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA5,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA5Extra,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA5Rotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA6,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA6Rotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA7,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoA9,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB0,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB4Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB5Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB5Extra,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB7,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoB9,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC0,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC3Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC4Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC5,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC5Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC6,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC6C5Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC6Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC7,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoC9,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoDLEnvelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoDLEnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IsoSRA3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Japan2LPhoto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanChou3Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanChou3EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanChou4Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanChou4EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanDoubleHagakiPostcard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanDoubleHagakiPostcardRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanHagakiPostcard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanHagakiPostcardRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanKaku2Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanKaku2EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanKaku3Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanKaku3EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanLPhoto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanQuadrupleHagakiPostcard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanYou1Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanYou2Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanYou3Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanYou4Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanYou4EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanYou6Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JapanYou6EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB0,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB4Rotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB5,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB5Rotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB6,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB6Rotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB7,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JisB9,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica10x11,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica10x12,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica10x14,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica11x17,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica14x17,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica4x6,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica4x8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica5x7,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica8x10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmerica9x11,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaArchitectureASheet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaArchitectureBSheet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaArchitectureCSheet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaArchitectureDSheet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaArchitectureESheet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaCSheet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaDSheet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaESheet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaExecutive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaGermanLegalFanfold,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaGermanStandardFanfold,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaLegal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaLegalExtra,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaLetter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaLetterExtra,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaLetterPlus,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaLetterRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaMonarchEnvelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaNote,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaNumber10Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaNumber10EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaNumber11Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaNumber12Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaNumber14Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaNumber9Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaPersonalEnvelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaQuarto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaStatement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaSuperA,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaSuperB,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaTabloid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NorthAmericaTabloidExtra,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherMetricA3Plus,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherMetricA4Plus,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherMetricFolio,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherMetricInviteEnvelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherMetricItalianEnvelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc10Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc10EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc16K,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc16KRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc1Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc1EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc2Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc2EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc32K,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc32KBig,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc32KRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc3Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc3EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc4Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc4EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc5Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc5EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc6Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc6EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc7Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc7EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc8Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc8EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc9Envelope,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Prc9EnvelopeRotated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll04Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll06Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll08Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll12Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll15Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll18Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll22Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll24Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll30Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll36Inch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Roll54Inch,
		#endif
	}
	#endif
}
