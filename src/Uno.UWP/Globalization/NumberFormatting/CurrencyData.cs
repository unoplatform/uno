#nullable enable

namespace Uno.Globalization.NumberFormatting;

internal class CurrencyData
{
	public int DefaultFractionDigits { get; set; }
	public string CurrencyCode { get; set; } = "";
	public string Symbol { get; set; } = "";
	public bool IsSymbolAfterNumber { get; set; }

	#region Initialize
	public static readonly CurrencyData Empty = new CurrencyData();

	private static readonly CurrencyData _allCurrencyData = new CurrencyData { CurrencyCode = "ALL", Symbol = "Lek", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _afnCurrencyData = new CurrencyData { CurrencyCode = "AFN", Symbol = "؋", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _arsCurrencyData = new CurrencyData { CurrencyCode = "ARS", Symbol = "ARS ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _awgCurrencyData = new CurrencyData { CurrencyCode = "AWG", Symbol = "ƒ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _audCurrencyData = new CurrencyData { CurrencyCode = "AUD", Symbol = "AUD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _aznCurrencyData = new CurrencyData { CurrencyCode = "AZN", Symbol = "₼", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bsdCurrencyData = new CurrencyData { CurrencyCode = "BSD", Symbol = "BSD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bbdCurrencyData = new CurrencyData { CurrencyCode = "BBD", Symbol = "BBD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bynCurrencyData = new CurrencyData { CurrencyCode = "BYN", Symbol = "Br", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bzdCurrencyData = new CurrencyData { CurrencyCode = "BZD", Symbol = "BZ$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bmdCurrencyData = new CurrencyData { CurrencyCode = "BMD", Symbol = "BMD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bobCurrencyData = new CurrencyData { CurrencyCode = "BOB", Symbol = "Bs.", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bamCurrencyData = new CurrencyData { CurrencyCode = "BAM", Symbol = "KM", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bwpCurrencyData = new CurrencyData { CurrencyCode = "BWP", Symbol = "P", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bgnCurrencyData = new CurrencyData { CurrencyCode = "BGN", Symbol = "лв.", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _brlCurrencyData = new CurrencyData { CurrencyCode = "BRL", Symbol = "R$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _bndCurrencyData = new CurrencyData { CurrencyCode = "BND", Symbol = "BND ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _khrCurrencyData = new CurrencyData { CurrencyCode = "KHR", Symbol = "៛", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _cadCurrencyData = new CurrencyData { CurrencyCode = "CAD", Symbol = "CAD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _kydCurrencyData = new CurrencyData { CurrencyCode = "KYD", Symbol = "KYD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _clpCurrencyData = new CurrencyData { CurrencyCode = "CLP", Symbol = "CLP ", DefaultFractionDigits = 0 };
	private static readonly CurrencyData _cnyCurrencyData = new CurrencyData { CurrencyCode = "CNY", Symbol = "¥", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _copCurrencyData = new CurrencyData { CurrencyCode = "COP", Symbol = "COP ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _crcCurrencyData = new CurrencyData { CurrencyCode = "CRC", Symbol = "₡", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _hrkCurrencyData = new CurrencyData { CurrencyCode = "HRK", Symbol = "kn", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _cupCurrencyData = new CurrencyData { CurrencyCode = "CUP", Symbol = "CUP ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _czkCurrencyData = new CurrencyData { CurrencyCode = "CZK", Symbol = "Kč", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _dkkCurrencyData = new CurrencyData { CurrencyCode = "DKK", Symbol = "kr.", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _dopCurrencyData = new CurrencyData { CurrencyCode = "DOP", Symbol = "RD$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _xcdCurrencyData = new CurrencyData { CurrencyCode = "XCD", Symbol = "EC$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _egpCurrencyData = new CurrencyData { CurrencyCode = "EGP", Symbol = "ج.م.‏", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _svcCurrencyData = new CurrencyData { CurrencyCode = "SVC", Symbol = "₡", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _eurCurrencyData = new CurrencyData { CurrencyCode = "EUR", Symbol = "€", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _fkpCurrencyData = new CurrencyData { CurrencyCode = "FKP", Symbol = "£", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _fjdCurrencyData = new CurrencyData { CurrencyCode = "FJD", Symbol = "FJD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _ghsCurrencyData = new CurrencyData { CurrencyCode = "GHS", Symbol = "GH₵", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _gipCurrencyData = new CurrencyData { CurrencyCode = "GIP", Symbol = "£", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _gtqCurrencyData = new CurrencyData { CurrencyCode = "GTQ", Symbol = "Q", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _ggpCurrencyData = new CurrencyData { CurrencyCode = "GGP", Symbol = "GGP", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _gydCurrencyData = new CurrencyData { CurrencyCode = "GYD", Symbol = "GYD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _hnlCurrencyData = new CurrencyData { CurrencyCode = "HNL", Symbol = "L.", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _hkdCurrencyData = new CurrencyData { CurrencyCode = "HKD", Symbol = "HKD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _hufCurrencyData = new CurrencyData { CurrencyCode = "HUF", Symbol = "Ft", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _iskCurrencyData = new CurrencyData { CurrencyCode = "ISK", Symbol = "kr.", DefaultFractionDigits = 0 };
	private static readonly CurrencyData _inrCurrencyData = new CurrencyData { CurrencyCode = "INR", Symbol = "₹", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _idrCurrencyData = new CurrencyData { CurrencyCode = "IDR", Symbol = "Rp", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _irrCurrencyData = new CurrencyData { CurrencyCode = "IRR", Symbol = "ريال", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _impCurrencyData = new CurrencyData { CurrencyCode = "IMP", Symbol = "IMP", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _ilsCurrencyData = new CurrencyData { CurrencyCode = "ILS", Symbol = "₪", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _jmdCurrencyData = new CurrencyData { CurrencyCode = "JMD", Symbol = "J$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _jpyCurrencyData = new CurrencyData { CurrencyCode = "JPY", Symbol = "¥", DefaultFractionDigits = 0 };
	private static readonly CurrencyData _jepCurrencyData = new CurrencyData { CurrencyCode = "JEP", Symbol = "JEP", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _kztCurrencyData = new CurrencyData { CurrencyCode = "KZT", Symbol = "₸", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _kpwCurrencyData = new CurrencyData { CurrencyCode = "KPW", Symbol = "₩", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _krwCurrencyData = new CurrencyData { CurrencyCode = "KRW", Symbol = "₩", DefaultFractionDigits = 0 };
	private static readonly CurrencyData _kgsCurrencyData = new CurrencyData { CurrencyCode = "KGS", Symbol = "сом", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _lakCurrencyData = new CurrencyData { CurrencyCode = "LAK", Symbol = "₭", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _lbpCurrencyData = new CurrencyData { CurrencyCode = "LBP", Symbol = "ل.ل.‏", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _lrdCurrencyData = new CurrencyData { CurrencyCode = "LRD", Symbol = "LRD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _mkdCurrencyData = new CurrencyData { CurrencyCode = "MKD", Symbol = "ден.", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _myrCurrencyData = new CurrencyData { CurrencyCode = "MYR", Symbol = "RM", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _murCurrencyData = new CurrencyData { CurrencyCode = "MUR", Symbol = "₨", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _mxnCurrencyData = new CurrencyData { CurrencyCode = "MXN", Symbol = "MXN ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _mntCurrencyData = new CurrencyData { CurrencyCode = "MNT", Symbol = "₮", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _mznCurrencyData = new CurrencyData { CurrencyCode = "MZN", Symbol = "MT", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _nadCurrencyData = new CurrencyData { CurrencyCode = "NAD", Symbol = "NAD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _nprCurrencyData = new CurrencyData { CurrencyCode = "NPR", Symbol = "रु", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _angCurrencyData = new CurrencyData { CurrencyCode = "ANG", Symbol = "NAƒ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _nzdCurrencyData = new CurrencyData { CurrencyCode = "NZD", Symbol = "NZD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _nioCurrencyData = new CurrencyData { CurrencyCode = "NIO", Symbol = "C$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _ngnCurrencyData = new CurrencyData { CurrencyCode = "NGN", Symbol = "₦", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _nokCurrencyData = new CurrencyData { CurrencyCode = "NOK", Symbol = "kr", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _omrCurrencyData = new CurrencyData { CurrencyCode = "OMR", Symbol = "ر.ع.‏", DefaultFractionDigits = 3 };
	private static readonly CurrencyData _pkrCurrencyData = new CurrencyData { CurrencyCode = "PKR", Symbol = "Rs", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _pabCurrencyData = new CurrencyData { CurrencyCode = "PAB", Symbol = "B/.", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _pygCurrencyData = new CurrencyData { CurrencyCode = "PYG", Symbol = "₲", DefaultFractionDigits = 0 };
	private static readonly CurrencyData _penCurrencyData = new CurrencyData { CurrencyCode = "PEN", Symbol = "S/", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _phpCurrencyData = new CurrencyData { CurrencyCode = "PHP", Symbol = "₱", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _plnCurrencyData = new CurrencyData { CurrencyCode = "PLN", Symbol = "zł", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _qarCurrencyData = new CurrencyData { CurrencyCode = "QAR", Symbol = "ر.ق.‏", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _ronCurrencyData = new CurrencyData { CurrencyCode = "RON", Symbol = "lei", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _rubCurrencyData = new CurrencyData { CurrencyCode = "RUB", Symbol = "₽", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _shpCurrencyData = new CurrencyData { CurrencyCode = "SHP", Symbol = "£", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _sarCurrencyData = new CurrencyData { CurrencyCode = "SAR", Symbol = "ر.س.‏", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _rsdCurrencyData = new CurrencyData { CurrencyCode = "RSD", Symbol = "din.", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _scrCurrencyData = new CurrencyData { CurrencyCode = "SCR", Symbol = "SR", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _sgdCurrencyData = new CurrencyData { CurrencyCode = "SGD", Symbol = "SGD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _sbdCurrencyData = new CurrencyData { CurrencyCode = "SBD", Symbol = "SBD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _sosCurrencyData = new CurrencyData { CurrencyCode = "SOS", Symbol = "S", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _zarCurrencyData = new CurrencyData { CurrencyCode = "ZAR", Symbol = "R", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _lkrCurrencyData = new CurrencyData { CurrencyCode = "LKR", Symbol = "Rs", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _sekCurrencyData = new CurrencyData { CurrencyCode = "SEK", Symbol = "kr", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _chfCurrencyData = new CurrencyData { CurrencyCode = "CHF", Symbol = "CHF", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _srdCurrencyData = new CurrencyData { CurrencyCode = "SRD", Symbol = "SRD ", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _sypCurrencyData = new CurrencyData { CurrencyCode = "SYP", Symbol = "ل.س.‏", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _twdCurrencyData = new CurrencyData { CurrencyCode = "TWD", Symbol = "NT$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _thbCurrencyData = new CurrencyData { CurrencyCode = "THB", Symbol = "฿", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _ttdCurrencyData = new CurrencyData { CurrencyCode = "TTD", Symbol = "TT$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _tryCurrencyData = new CurrencyData { CurrencyCode = "TRY", Symbol = "₺", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _tvdCurrencyData = new CurrencyData { CurrencyCode = "TVD", Symbol = "TVD", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _uahCurrencyData = new CurrencyData { CurrencyCode = "UAH", Symbol = "₴", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _aedCurrencyData = new CurrencyData { CurrencyCode = "AED", Symbol = "د.إ.‏", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _gbpCurrencyData = new CurrencyData { CurrencyCode = "GBP", Symbol = "£", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _usdCurrencyData = new CurrencyData { CurrencyCode = "USD", Symbol = "$", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _uyuCurrencyData = new CurrencyData { CurrencyCode = "UYU", Symbol = "$U", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _uzsCurrencyData = new CurrencyData { CurrencyCode = "UZS", Symbol = "so'm", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _vefCurrencyData = new CurrencyData { CurrencyCode = "VEF", Symbol = "Bs.F.", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _vndCurrencyData = new CurrencyData { CurrencyCode = "VND", Symbol = "₫", DefaultFractionDigits = 0 };
	private static readonly CurrencyData _yerCurrencyData = new CurrencyData { CurrencyCode = "YER", Symbol = "ر.ي.‏", DefaultFractionDigits = 2 };
	private static readonly CurrencyData _zwdCurrencyData = new CurrencyData { CurrencyCode = "ZWD", Symbol = "ZWD", DefaultFractionDigits = 2 };

	#endregion

	public static CurrencyData GetCurrencyData(string currencyCode)
	{
		switch (currencyCode)
		{
			case "ALL":
				return _allCurrencyData;
			case "AFN":
				return _afnCurrencyData;
			case "ARS":
				return _arsCurrencyData;
			case "AWG":
				return _awgCurrencyData;
			case "AUD":
				return _audCurrencyData;
			case "AZN":
				return _aznCurrencyData;
			case "BSD":
				return _bsdCurrencyData;
			case "BBD":
				return _bbdCurrencyData;
			case "BYN":
				return _bynCurrencyData;
			case "BZD":
				return _bzdCurrencyData;
			case "BMD":
				return _bmdCurrencyData;
			case "BOB":
				return _bobCurrencyData;
			case "BAM":
				return _bamCurrencyData;
			case "BWP":
				return _bwpCurrencyData;
			case "BGN":
				return _bgnCurrencyData;
			case "BRL":
				return _brlCurrencyData;
			case "BND":
				return _bndCurrencyData;
			case "KHR":
				return _khrCurrencyData;
			case "CAD":
				return _cadCurrencyData;
			case "KYD":
				return _kydCurrencyData;
			case "CLP":
				return _clpCurrencyData;
			case "CNY":
				return _cnyCurrencyData;
			case "COP":
				return _copCurrencyData;
			case "CRC":
				return _crcCurrencyData;
			case "HRK":
				return _hrkCurrencyData;
			case "CUP":
				return _cupCurrencyData;
			case "CZK":
				return _czkCurrencyData;
			case "DKK":
				return _dkkCurrencyData;
			case "DOP":
				return _dopCurrencyData;
			case "XCD":
				return _xcdCurrencyData;
			case "EGP":
				return _egpCurrencyData;
			case "SVC":
				return _svcCurrencyData;
			case "EUR":
				return _eurCurrencyData;
			case "FKP":
				return _fkpCurrencyData;
			case "FJD":
				return _fjdCurrencyData;
			case "GHS":
				return _ghsCurrencyData;
			case "GIP":
				return _gipCurrencyData;
			case "GTQ":
				return _gtqCurrencyData;
			case "GGP":
				return _ggpCurrencyData;
			case "GYD":
				return _gydCurrencyData;
			case "HNL":
				return _hnlCurrencyData;
			case "HKD":
				return _hkdCurrencyData;
			case "HUF":
				return _hufCurrencyData;
			case "ISK":
				return _iskCurrencyData;
			case "INR":
				return _inrCurrencyData;
			case "IDR":
				return _idrCurrencyData;
			case "IRR":
				return _irrCurrencyData;
			case "IMP":
				return _impCurrencyData;
			case "ILS":
				return _ilsCurrencyData;
			case "JMD":
				return _jmdCurrencyData;
			case "JPY":
				return _jpyCurrencyData;
			case "JEP":
				return _jepCurrencyData;
			case "KZT":
				return _kztCurrencyData;
			case "KPW":
				return _kpwCurrencyData;
			case "KRW":
				return _krwCurrencyData;
			case "KGS":
				return _kgsCurrencyData;
			case "LAK":
				return _lakCurrencyData;
			case "LBP":
				return _lbpCurrencyData;
			case "LRD":
				return _lrdCurrencyData;
			case "MKD":
				return _mkdCurrencyData;
			case "MYR":
				return _myrCurrencyData;
			case "MUR":
				return _murCurrencyData;
			case "MXN":
				return _mxnCurrencyData;
			case "MNT":
				return _mntCurrencyData;
			case "MZN":
				return _mznCurrencyData;
			case "NAD":
				return _nadCurrencyData;
			case "NPR":
				return _nprCurrencyData;
			case "ANG":
				return _angCurrencyData;
			case "NZD":
				return _nzdCurrencyData;
			case "NIO":
				return _nioCurrencyData;
			case "NGN":
				return _ngnCurrencyData;
			case "NOK":
				return _nokCurrencyData;
			case "OMR":
				return _omrCurrencyData;
			case "PKR":
				return _pkrCurrencyData;
			case "PAB":
				return _pabCurrencyData;
			case "PYG":
				return _pygCurrencyData;
			case "PEN":
				return _penCurrencyData;
			case "PHP":
				return _phpCurrencyData;
			case "PLN":
				return _plnCurrencyData;
			case "QAR":
				return _qarCurrencyData;
			case "RON":
				return _ronCurrencyData;
			case "RUB":
				return _rubCurrencyData;
			case "SHP":
				return _shpCurrencyData;
			case "SAR":
				return _sarCurrencyData;
			case "RSD":
				return _rsdCurrencyData;
			case "SCR":
				return _scrCurrencyData;
			case "SGD":
				return _sgdCurrencyData;
			case "SBD":
				return _sbdCurrencyData;
			case "SOS":
				return _sosCurrencyData;
			case "ZAR":
				return _zarCurrencyData;
			case "LKR":
				return _lkrCurrencyData;
			case "SEK":
				return _sekCurrencyData;
			case "CHF":
				return _chfCurrencyData;
			case "SRD":
				return _srdCurrencyData;
			case "SYP":
				return _sypCurrencyData;
			case "TWD":
				return _twdCurrencyData;
			case "THB":
				return _thbCurrencyData;
			case "TTD":
				return _ttdCurrencyData;
			case "TRY":
				return _tryCurrencyData;
			case "TVD":
				return _tvdCurrencyData;
			case "UAH":
				return _uahCurrencyData;
			case "AED":
				return _aedCurrencyData;
			case "GBP":
				return _gbpCurrencyData;
			case "USD":
				return _usdCurrencyData;
			case "UYU":
				return _uyuCurrencyData;
			case "UZS":
				return _uzsCurrencyData;
			case "VEF":
				return _vefCurrencyData;
			case "VND":
				return _vndCurrencyData;
			case "YER":
				return _yerCurrencyData;
			case "ZWD":
				return _zwdCurrencyData;
			default:
				return Empty;
		}
	}
}
