using System;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2PrintSettings
{
	internal const double DefaultMargin = 1d / 2.54d;

	private int _copies = 1;
	private double _marginTop = DefaultMargin;
	private double _marginBottom = DefaultMargin;
	private double _marginLeft = DefaultMargin;
	private double _marginRight = DefaultMargin;
	private double _pageHeight = 11d;
	private double _pageWidth = 8.5d;
	private int _pagesPerSide = 1;
	private double _scaleFactor = 1d;

	internal CoreWebView2PrintSettings()
	{
	}

	public CoreWebView2PrintCollation Collation { get; set; }
	public CoreWebView2PrintColorMode ColorMode { get; set; }
	public int Copies
	{
		get => _copies;
		set => _copies = value is >= 1 and <= 999 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}
	public CoreWebView2PrintDuplex Duplex { get; set; }
	public string FooterUri { get; set; } = string.Empty;
	public string HeaderTitle { get; set; } = string.Empty;
	public double MarginTop
	{
		get => _marginTop;
		set => _marginTop = ValidateNonNegative(value, nameof(value));
	}
	public double MarginBottom
	{
		get => _marginBottom;
		set => _marginBottom = ValidateNonNegative(value, nameof(value));
	}
	public double MarginLeft
	{
		get => _marginLeft;
		set => _marginLeft = ValidateNonNegative(value, nameof(value));
	}
	public double MarginRight
	{
		get => _marginRight;
		set => _marginRight = ValidateNonNegative(value, nameof(value));
	}
	public CoreWebView2PrintMediaSize MediaSize { get; set; }
	public CoreWebView2PrintOrientation Orientation { get; set; }
	public double PageHeight
	{
		get => _pageHeight;
		set => _pageHeight = ValidatePositive(value, nameof(value));
	}
	public double PageWidth
	{
		get => _pageWidth;
		set => _pageWidth = ValidatePositive(value, nameof(value));
	}
	public string PageRanges { get; set; } = string.Empty;
	public int PagesPerSide
	{
		get => _pagesPerSide;
		set => _pagesPerSide = value is 1 or 2 or 4 or 6 or 9 or 16 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}
	public string PrinterName { get; set; } = string.Empty;
	public double ScaleFactor
	{
		get => _scaleFactor;
		set => _scaleFactor = value is >= 0.1d and <= 2d ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}
	public bool ShouldPrintBackgrounds { get; set; }
	public bool ShouldPrintHeaderAndFooter { get; set; }
	public bool ShouldPrintSelectionOnly { get; set; }

	internal bool HasNonDefaultPdfSettings =>
		Collation != CoreWebView2PrintCollation.Default
		|| ColorMode != CoreWebView2PrintColorMode.Default
		|| Copies != 1
		|| Duplex != CoreWebView2PrintDuplex.Default
		|| !string.IsNullOrEmpty(FooterUri)
		|| !string.IsNullOrEmpty(HeaderTitle)
		|| MediaSize != CoreWebView2PrintMediaSize.Default
		|| Orientation != CoreWebView2PrintOrientation.Portrait
		|| Math.Abs(ScaleFactor - 1d) > double.Epsilon
		|| Math.Abs(MarginTop - DefaultMargin) > double.Epsilon
		|| Math.Abs(MarginBottom - DefaultMargin) > double.Epsilon
		|| Math.Abs(MarginLeft - DefaultMargin) > double.Epsilon
		|| Math.Abs(MarginRight - DefaultMargin) > double.Epsilon
		|| Math.Abs(PageHeight - 11d) > double.Epsilon
		|| Math.Abs(PageWidth - 8.5d) > double.Epsilon
		|| !string.IsNullOrEmpty(PageRanges)
		|| PagesPerSide != 1
		|| !string.IsNullOrEmpty(PrinterName)
		|| ShouldPrintBackgrounds
		|| ShouldPrintHeaderAndFooter
		|| ShouldPrintSelectionOnly;

	private static double ValidateNonNegative(double value, string parameterName) =>
		double.IsFinite(value) && value >= 0d ? value : throw new ArgumentOutOfRangeException(parameterName);

	private static double ValidatePositive(double value, string parameterName) =>
		double.IsFinite(value) && value > 0d ? value : throw new ArgumentOutOfRangeException(parameterName);
}