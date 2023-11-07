// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference InteractionTests/ColorPickerTests.cpp, tag winui3/release/1.4.2

// using System;
// using System.Collections.Generic;
// using System.Threading;
//
// using Windows.Foundation.Metadata;
// using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Infra;
// using Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Common;
// using Common;
//
// using WEX.TestExecution;
// using WEX.TestExecution.Markup;
// using WEX.Logging.Interop;
//
// using Microsoft.Windows.Apps.Test.Automation;
// using Microsoft.Windows.Apps.Test.Foundation;
// using Microsoft.Windows.Apps.Test.Foundation.Controls;
// using Microsoft.Windows.Apps.Test.Foundation.Patterns;
// using Microsoft.Windows.Apps.Test.Foundation.Waiters;
//
// namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests
// {
//     // ColorSpectrum doesn't fit any of the primitives provided in MITA,
//     // so we'll define our own MITA object for it here.
//     public class ColorSpectrum : UIObject, IValue
//     {
//         public ColorSpectrum(UIObject uiObject)
//             : base(uiObject)
//         {
//             this.Initialize();
//         }
//
//         private void Initialize()
//         {
//             _valuePattern = new ValueImplementation(this);
//         }
//
//         public ColorChangedEventWaiter GetColorChangedWaiter()
//         {
//             return new ColorChangedEventWaiter();
//         }
//
//         public virtual void SetValue(string value)
//         {
//             _valuePattern.SetValue(value);
//         }
//
//         public virtual string Value
//         {
//             get
//             {
//                 return _valuePattern.Value;
//             }
//         }
//
//         public virtual bool IsReadOnly
//         {
//             get
//             {
//                 return _valuePattern.IsReadOnly;
//             }
//         }
//
//         new public static IFactory<ColorSpectrum> Factory
//         {
//             get
//             {
//                 if (null == ColorSpectrum._factory)
//                 {
//                     ColorSpectrum._factory = new ColorSpectrumFactory();
//                 }
//                 return ColorSpectrum._factory;
//             }
//         }
//
//         private IValue _valuePattern;
//         private static IFactory<ColorSpectrum> _factory = null;
//
//         private class ColorSpectrumFactory : IFactory<ColorSpectrum>
//         {
//             public ColorSpectrum Create(UIObject element)
//             {
//                 return new ColorSpectrum(element);
//             }
//         }
//     }
//
//     // Similarly, ColorPickerSlider also doesn't fit any of the primitives
//     // provided in MITA, so we'll define our own MITA object for it here, too.
//     public class ColorPickerSlider : UIObject, IValue
//     {
//         public ColorPickerSlider(UIObject uiObject)
//             : base(uiObject)
//         {
//             this.Initialize();
//         }
//
//         private void Initialize()
//         {
//             _valuePattern = new ValueImplementation(this);
//         }
//
//         public ColorChangedEventWaiter GetColorChangedWaiter()
//         {
//             return new ColorChangedEventWaiter();
//         }
//
//         public virtual void SetValue(string value)
//         {
//             _valuePattern.SetValue(value);
//         }
//
//         public virtual string Value
//         {
//             get
//             {
//                 return _valuePattern.Value;
//             }
//         }
//
//         public virtual bool IsReadOnly
//         {
//             get
//             {
//                 return _valuePattern.IsReadOnly;
//             }
//         }
//
//         new public static IFactory<ColorPickerSlider> Factory
//         {
//             get
//             {
//                 if (null == ColorPickerSlider._factory)
//                 {
//                     ColorPickerSlider._factory = new ColorPickerSliderFactory();
//                 }
//                 return ColorPickerSlider._factory;
//             }
//         }
//
//         private IValue _valuePattern;
//         private static IFactory<ColorPickerSlider> _factory = null;
//
//         private class ColorPickerSliderFactory : IFactory<ColorPickerSlider>
//         {
//             public ColorPickerSlider Create(UIObject element)
//             {
//                 return new ColorPickerSlider(element);
//             }
//         }
//     }
//
//     // Depending on the current state of the ColorPicker, either the RGB text boxes will be visible
//     // or the HSV text boxes will be visible.  Waiting on either triplet works for the purposes of
//     // waiting for the color to change, hence the odd nomenclature in this class.
//     public class ColorChangedEventWaiter : Waiter
//     {
//         private ValueChangedEventWaiter redOrHueChangedWaiter;
//         private ValueChangedEventWaiter greenOrSaturationChangedWaiter;
//         private ValueChangedEventWaiter blueOrValueChangedWaiter;
//
//         public ColorChangedEventWaiter()
//         {
//             string redOrHueTextBoxAutomationId = null;
//             string greenOrSaturationTextBoxAutomationId = null;
//             string blueOrValueTextBoxAutomationId = null;
//
//             ColorChannels currentSelectedChannels = ColorPickerTests.GetCurrentlySelectedChannels();
//
//             if (currentSelectedChannels == ColorChannels.RGB)
//             {
//                 redOrHueTextBoxAutomationId = ColorPickerTests.RedTextBoxAutomationId;
//                 greenOrSaturationTextBoxAutomationId = ColorPickerTests.GreenTextBoxAutomationId;
//                 blueOrValueTextBoxAutomationId = ColorPickerTests.BlueTextBoxAutomationId;
//             }
//             else
//             {
//                 redOrHueTextBoxAutomationId = ColorPickerTests.HueTextBoxAutomationId;
//                 greenOrSaturationTextBoxAutomationId = ColorPickerTests.SaturationTextBoxAutomationId;
//                 blueOrValueTextBoxAutomationId = ColorPickerTests.ValueTextBoxAutomationId;
//             }
//
//             redOrHueChangedWaiter = new ValueChangedEventWaiter(new Edit(FindElement.ById(redOrHueTextBoxAutomationId)));
//             greenOrSaturationChangedWaiter = new ValueChangedEventWaiter(new Edit(FindElement.ById(greenOrSaturationTextBoxAutomationId)));
//             blueOrValueChangedWaiter = new ValueChangedEventWaiter(new Edit(FindElement.ById(blueOrValueTextBoxAutomationId)));
//         }
//
//         public override void Dispose()
//         {
//             redOrHueChangedWaiter.Dispose();
//             greenOrSaturationChangedWaiter.Dispose();
//             blueOrValueChangedWaiter.Dispose();
//         }
//
//         public override bool TryWait(TimeSpan timeout)
//         {
//             return CompositableWaiter.TryWaitAny(timeout, redOrHueChangedWaiter, greenOrSaturationChangedWaiter, blueOrValueChangedWaiter) != null;
//         }
//     }
//
//     public enum ColorChannels
//     {
//         RGB,
//         HSV,
//     }
//
//     [TestClass]
//     public class ColorPickerTests
//     {
//         public const string ColorSpectrumAutomationId = "ColorSpectrum";
//         public const string ThirdDimensionAutomationId = "ThirdDimensionSlider";
//         public const string AlphaSliderAutomationId = "AlphaSlider";
//         public const string MoreButtonAutomationId = "MoreButton";
//         public const string MoreButtonLabelAutomationId = "MoreButtonLabel";
//         public const string ColorRepresentationComboBoxAutomationId = "ColorRepresentationComboBox";
//         public const string RGBComboBoxItemAutomationId = "RGBComboBoxItem";
//         public const string HSVComboBoxItemAutomationId = "HSVComboBoxItem";
//         public const string RedTextBoxAutomationId = "RedTextBox";
//         public const string RedLabelAutomationId = "RedLabel";
//         public const string GreenTextBoxAutomationId = "GreenTextBox";
//         public const string GreenLabelAutomationId = "GreenLabel";
//         public const string BlueTextBoxAutomationId = "BlueTextBox";
//         public const string BlueLabelAutomationId = "BlueLabel";
//         public const string HueTextBoxAutomationId = "HueTextBox";
//         public const string HueLabelAutomationId = "HueLabel";
//         public const string SaturationTextBoxAutomationId = "SaturationTextBox";
//         public const string SaturationLabelAutomationId = "SaturationLabel";
//         public const string ValueTextBoxAutomationId = "ValueTextBox";
//         public const string ValueLabelAutomationId = "ValueLabel";
//         public const string AlphaTextBoxAutomationId = "AlphaTextBox";
//         public const string AlphaLabelAutomationId = "AlphaLabel";
//         public const string HexTextBoxAutomationId = "HexTextBox";
//
//         [Flags]
//         public enum TestOptions
//         {
//             None = 0,
//             EnableAlpha = 1,
//             EnableMoreButton = 2,
//             EnableNonEnglishLanguage = 4,
//             DisableColorSpectrumLoadWait = 8
//         }
//
//         public class Color
//         {
//             public byte A { get; set; }
//             public byte R { get; set; }
//             public byte G { get; set; }
//             public byte B { get; set; }
//
//             public static Color FromArgb(byte a, byte r, byte g, byte b)
//             {
//                 return new Color() { A = a, R = r, G = g, B = b };
//             }
//         }
//
//         public sealed class ColorPickerTestSetupHelper : TestSetupHelper, IEventSink
//         {
//             private PropertyChangedEventSource redEventSource;
//             private PropertyChangedEventSource greenEventSource;
//             private PropertyChangedEventSource blueEventSource;
//             private PropertyChangedEventSource alphaEventSource;
//
//             private object lockObject;
//             private ManualResetEvent colorChangedEvent;
//             private bool waitingForColorChanged;
//
//             public static ColorPickerTestSetupHelper Current
//             {
//                 get;
//                 private set;
//             }
//
//             public Color Color
//             {
//                 get;
//                 private set;
//             }
//
//             public ColorPickerTestSetupHelper(string testName, string languageOverride = "")
//                 : base(testName, new TestSetupHelperOptions{ LanguageOverride = languageOverride})
//             {
//                 Current = this;
//
//                 redEventSource = null;
//                 greenEventSource = null;
//                 blueEventSource = null;
//                 alphaEventSource = null;
//
//                 lockObject = new object();
//                 colorChangedEvent = new ManualResetEvent(false);
//                 waitingForColorChanged = false;
//             }
//
//             public void StartTrackingColorChanges()
//             {
//                 Edit redTextBox = new Edit(FindElement.ById(RedTextBoxAutomationId));
//                 Edit greenTextBox = new Edit(FindElement.ById(GreenTextBoxAutomationId));
//                 Edit blueTextBox = new Edit(FindElement.ById(BlueTextBoxAutomationId));
//                 Edit alphaTextBox = new Edit(FindElement.ById(AlphaTextBoxAutomationId));
//
//                 Color = Color.FromArgb(ParseAlphaTextBoxValue(alphaTextBox.Value), byte.Parse(redTextBox.Value), byte.Parse(greenTextBox.Value), byte.Parse(blueTextBox.Value));
//
//                 redEventSource = new PropertyChangedEventSource(redTextBox, Scope.Element, UIProperty.Get("Value.Value"));
//                 greenEventSource = new PropertyChangedEventSource(greenTextBox, Scope.Element, UIProperty.Get("Value.Value"));
//                 blueEventSource = new PropertyChangedEventSource(blueTextBox, Scope.Element, UIProperty.Get("Value.Value"));
//                 alphaEventSource = new PropertyChangedEventSource(alphaTextBox, Scope.Element, UIProperty.Get("Value.Value"));
//
//                 redEventSource.Start(this);
//                 greenEventSource.Start(this);
//                 blueEventSource.Start(this);
//                 alphaEventSource.Start(this);
//
//                 Log.Comment("Color-change tracking started. Initial color is RGBA = ({0}, {1}, {2}, {3}).", Color.R, Color.G, Color.B, Color.A);
//             }
//
//             public void ExecuteAndWaitForColorChange(Action action)
//             {
//                 waitingForColorChanged = true;
//                 colorChangedEvent.Reset();
//                 action();
//
//                 Log.Comment("Waiting for color to change...");
//                 colorChangedEvent.WaitOne();
//                 Log.Comment("Color changed.");
//
//                 waitingForColorChanged = false;
//             }
//
//             protected override void Dispose(bool isDisposing)
//             {
//                 base.Dispose(isDisposing);
//
//                 lock (lockObject)
//                 {
//                     if (redEventSource == null || greenEventSource == null || blueEventSource == null || alphaEventSource == null)
//                     {
//                         throw new Exception("Color change tracking was never started. Make sure to call StartTrackingColorChanges() on ColorPickerTestSetupHelper.");
//                     }
//
//                     redEventSource.Stop();
//                     redEventSource.Dispose();
//                     redEventSource = null;
//
//                     greenEventSource.Stop();
//                     greenEventSource.Dispose();
//                     greenEventSource = null;
//
//                     blueEventSource.Stop();
//                     blueEventSource.Dispose();
//                     blueEventSource = null;
//
//                     alphaEventSource.Stop();
//                     alphaEventSource.Dispose();
//                     alphaEventSource = null;
//
//                     colorChangedEvent.Dispose();
//                     colorChangedEvent = null;
//                 }
//
//                 Current = null;
//             }
//
//             public void SinkEvent(WaiterEventArgs eventArgs)
//             {
//                 lock (lockObject)
//                 {
//                     AutomationPropertyChangedEventArgs args = eventArgs.EventArgs as AutomationPropertyChangedEventArgs;
//                     string channelChanged = string.Empty;
//                     byte oldValue = 0;
//                     byte newValue = 0;
//
//                     string senderAutomationId = eventArgs.Sender.AutomationId;
//
//                     switch (senderAutomationId)
//                     {
//                         case RedTextBoxAutomationId:
//                             channelChanged = "Red";
//                             oldValue = Color.R;
//                             Color = Color.FromArgb(Color.A, byte.Parse(args.NewValue as string), Color.G, Color.B);
//                             newValue = Color.R;
//                             break;
//                         case GreenTextBoxAutomationId:
//                             channelChanged = "Green";
//                             oldValue = Color.G;
//                             Color = Color.FromArgb(Color.A, Color.R, byte.Parse(args.NewValue as string), Color.B);
//                             newValue = Color.G;
//                             break;
//                         case BlueTextBoxAutomationId:
//                             channelChanged = "Blue";
//                             oldValue = Color.B;
//                             Color = Color.FromArgb(Color.A, Color.R, Color.G, byte.Parse(args.NewValue as string));
//                             newValue = Color.B;
//                             break;
//                         case AlphaTextBoxAutomationId:
//                             channelChanged = "Alpha";
//                             oldValue = Color.A;
//                             // The alpha text box contains a percentage rather than a number between 0 and 255,
//                             // so we need to convert it to that before giving its value to ColorHelper.FromArgb().
//                             Color = Color.FromArgb(ParseAlphaTextBoxValue(args.NewValue), Color.R, Color.G, Color.B);
//                             newValue = Color.A;
//                             break;
//                         default:
//                             throw new Exception(string.Format("Unknown sender '{0}'.", senderAutomationId));
//                     }
//
//                     Log.Comment("{0} changed from {1} to {2}. Color is now RGBA = ({3}, {4}, {5}, {6}).", channelChanged, oldValue, newValue, Color.R, Color.G, Color.B, Color.A);
//
//                     if (waitingForColorChanged && colorChangedEvent != null)
//                     {
//                         colorChangedEvent.Set();
//                     }
//                 }
//             }
//
//             private byte ParseAlphaTextBoxValue(object value)
//             {
//                 return (byte)(byte.Parse((value.ToString()).TrimEnd('%')) * 255 / 100);
//             }
//         }
//
//         public static ColorChannels GetCurrentlySelectedChannels()
//         {
//             ColorChannels currentlySelectedChannels = ColorChannels.RGB;
//
//             ComboBox colorRepresentationComboBox = new ComboBox(FindElement.ById(ColorRepresentationComboBoxAutomationId));
//             ComboBoxItem selectedComboBoxItem = colorRepresentationComboBox.Selection[0];
//
//             if (selectedComboBoxItem.AutomationId == RGBComboBoxItemAutomationId)
//             {
//                 currentlySelectedChannels = ColorChannels.RGB;
//             }
//             else if (selectedComboBoxItem.AutomationId == HSVComboBoxItemAutomationId)
//             {
//                 currentlySelectedChannels = ColorChannels.HSV;
//             }
//             else
//             {
//                 Verify.Fail("We should never have a combo box item that isn't either RGB or HSV.");
//             }
//
//             return currentlySelectedChannels;
//         }
//
//         [ClassInitialize]
//         [TestProperty("RunAs", "User")]
//         [TestProperty("Classification", "Integration")]
//         [TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
//         public static void ClassInitialize(TestContext testContext)
//         {
//             TestEnvironment.Initialize(testContext);
//         }
//
//         [TestCleanup]
//         public void TestCleanup()
//         {
//             TestCleanupHelper.Cleanup();
//         }
//
//         [TestMethod]
//         public void CanSelectColorFromSpectrumLTR()
//         {
//             using (var setup = SetupColorPickerTest())
//             {
//                 CanSelectColorFromSpectrum(setup, false /* isRTL */);
//             }
//         }
//
//         [TestMethod]
//         public void CanSelectColorFromSpectrumRTL()
//         {
//             using (var setup = SetupColorPickerTest())
//             {
//                 CanSelectColorFromSpectrum(setup, true /* isRTL */);
//             }
//         }
//
//         private void CanSelectColorFromSpectrum(ColorPickerTestSetupHelper setup, bool isRTL)
//         {
//             SetIsRTL(isRTL);
//
//             // We snap to the nearest HSV value and then derive the RGB value and ellipse position from that,
//             // so we're bound to run into rounding errors along the way, which is what accounts for the slightly
//             // different values we get in RTL where our x-axis is flipped.
//             setup.ExecuteAndWaitForColorChange(() => TapOnColorSpectrumAndWaitForColorChange(0.5, 0.5));
//             VerifySelectedColorIsNear(127, 255, 252);
//             VerifySelectionEllipseIsNear(127, 127);
//             setup.ExecuteAndWaitForColorChange(() => TapOnColorSpectrumAndWaitForColorChange(0.25, 0.25));
//             VerifySelectedColorIsNear(163, isRTL ? 63 : 255, isRTL ? 255 : 63);
//             VerifySelectionEllipseIsNear(isRTL ? 194 : 63, 63);
//             setup.ExecuteAndWaitForColorChange(() => TapOnColorSpectrumAndWaitForColorChange(0.75, 0.25));
//             VerifySelectedColorIsNear(151, isRTL ? 255 : 63, isRTL ? 63 : 255);
//             VerifySelectionEllipseIsNear(isRTL ? 66 : 191, 63);
//             setup.ExecuteAndWaitForColorChange(() => TapOnColorSpectrumAndWaitForColorChange(0.25, 0.75));
//             VerifySelectedColorIsNear(224, isRTL ? 191 : 255, isRTL ? 255 : 191);
//             VerifySelectionEllipseIsNear(isRTL ? 194 : 63, 192);
//             setup.ExecuteAndWaitForColorChange(() => TapOnColorSpectrumAndWaitForColorChange(0.75, 0.75));
//             VerifySelectedColorIsNear(220, isRTL ? 255 : 191, isRTL ? 191 : 255);
//             VerifySelectionEllipseIsNear(isRTL ? 66 : 191, 192);
//         }
//
//         [TestMethod]
//         public void VerifyHsvUpdatesOnSpectrumSelection()
//         {
//             using (var setup = SetupColorPickerTest())
//             {
//                 TapOnColorSpectrumAndWaitForColorChange(0.5, 0.5);
//                 VerifySelectedColorIsNear(127, 255, 252);
//                 VerifySelectionEllipseIsNear(127, 127);
//
//                 WriteInTextBox(ValueTextBoxAutomationId, "0");
//                 VerifySelectedColorIsNear(0, 0, 0);
//
//                 // Since Value is 0, the RGB value stays black and does not trigger the colorChangedEvent,
//                 // so this helper function is used instead.
//                 TapOnColorSpectrum(0.25, 0.25);
//                 Wait.ForIdle();
//
//                 VerifySelectedColorIsNear(0, 0, 0);
//
//                 // Allowing +/-1 devation
//                 const double errorMargin = 1;
//
//                 Verify.IsLessThanOrEqual(Math.Abs(90 - Double.Parse((new Edit(FindElement.ById(HueTextBoxAutomationId))).Value)), errorMargin);
//                 Verify.IsLessThanOrEqual(Math.Abs(75 - Double.Parse((new Edit(FindElement.ById(SaturationTextBoxAutomationId))).Value)), errorMargin);
//                 Verify.IsLessThanOrEqual(Math.Abs(0 - Double.Parse((new Edit(FindElement.ById(ValueTextBoxAutomationId))).Value)), errorMargin);
//
//                 TapOnColorSpectrum(0.75, 0.75);
//                 Wait.ForIdle();
//
//                 VerifySelectedColorIsNear(0, 0, 0);
//
//                 Verify.IsLessThanOrEqual(270 - Double.Parse((new Edit(FindElement.ById(HueTextBoxAutomationId))).Value), errorMargin);
//                 Verify.IsLessThanOrEqual(25 - Double.Parse((new Edit(FindElement.ById(SaturationTextBoxAutomationId))).Value), errorMargin);
//                 Verify.IsLessThanOrEqual(0 - Double.Parse((new Edit(FindElement.ById(ValueTextBoxAutomationId))).Value), errorMargin);
//             }
//         }
//
//         [TestMethod]
//         public void CanSelectPreviousColor()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 VerifyPreviousColorIsNull();
//
//                 Button previousColorRedButton = new Button(FindElement.ById("PreviousColorRedButton"));
//                 previousColorRedButton.Invoke();
//                 VerifyPreviousColorIsEqualTo(255, 0, 0);
//
//                 SetColorSpectrumColor(128, 255, 255);
//                 Button previousColorCurrentColorButton = new Button(FindElement.ById("PreviousColorCurrentColorButton"));
//                 previousColorCurrentColorButton.Invoke();
//                 VerifyPreviousColorIsEqualTo(128, 255, 255);
//             }
//         }
//
//         [TestMethod]
//         public void CanChangeColorThroughRgbTextEntry()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 WriteInTextBox(RedTextBoxAutomationId, "128");
//                 VerifyColorIsSelected(128, 0, 0);
//                 WriteInTextBox(GreenTextBoxAutomationId, "128");
//                 VerifyColorIsSelected(128, 128, 0);
//                 WriteInTextBox(BlueTextBoxAutomationId, "128");
//                 VerifyColorIsSelected(128, 128, 128);
//             }
//         }
//
//         [TestMethod]
//         public void CanChangeColorThroughHsvTextEntry()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 WriteInTextBox(HueTextBoxAutomationId, "120");
//                 VerifyColorIsSelected(0, 255, 0);
//                 WriteInTextBox(HueTextBoxAutomationId, "240");
//                 VerifyColorIsSelected(0, 0, 255);
//                 WriteInTextBox(SaturationTextBoxAutomationId, "50");
//                 VerifyColorIsSelected(128, 128, 255);
//                 WriteInTextBox(SaturationTextBoxAutomationId, "100");
//                 WriteInTextBox(ValueTextBoxAutomationId, "50");
//                 VerifyColorIsSelected(0, 0, 128);
//             }
//         }
//
//         [TestMethod]
//         public void CanChangeColorThroughOpacityTextEntry()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 WriteInTextBox(AlphaTextBoxAutomationId, "50%");
//                 VerifyColorIsSelected(255, 0, 0, 128);
//             }
//         }
//
//         [TestMethod]
//         public void CanChangeColorThroughHexTextEntry()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 WriteInTextBox(HexTextBoxAutomationId, "#7F7F7F7F");
//                 VerifyColorIsSelected(127, 127, 127, 127);
//             }
//         }
//
//         [TestMethod]
//         public void SelectionEllipseChangesColorAccordingToSelectedColor()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha))
//             {
//                 ComboBox colorSpectrumComponentsComboBox = new ComboBox(FindElement.ById("ColorSpectrumComponentsComboBox"));
//                 colorSpectrumComponentsComboBox.SelectItemById("ColorSpectrumComponentsHueValue");
//
//                 VerifySelectionEllipseColorIsEqualTo(255, 255, 255);
//                 WriteInTextBox(AlphaTextBoxAutomationId, "0%");
//                 VerifySelectionEllipseColorIsEqualTo(255, 255, 255);
//                 WriteInTextBox(HueTextBoxAutomationId, "120");
//                 VerifySelectionEllipseColorIsEqualTo(0, 0, 0);
//                 WriteInTextBox(ValueTextBoxAutomationId, "85");
//                 VerifySelectionEllipseColorIsEqualTo(255, 255, 255);
//                 WriteInTextBox(SaturationTextBoxAutomationId, "50");
//                 VerifySelectionEllipseColorIsEqualTo(0, 0, 0);
//             }
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithColorSpectrumLTR()
//         {
//             CanUseKeyboardToInteractWithColorSpectrum(isRTL: false);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractColorSpectrumRTL()
//         {
//             CanUseKeyboardToInteractWithColorSpectrum(isRTL: true);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithSlidersLTR()
//         {
//             CanUseKeyboardToInteractWithSliders(isRTL: false);
//             CanUseKeyboardToInteractWithSliders(isRTL: false, shouldTryUpAndDown: true);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithSlidersLTRHorizontalOrientation()
//         {
//             CanUseKeyboardToInteractWithSliders(isRTL: false, shouldOrientationBeHorizontal: true);
//             CanUseKeyboardToInteractWithSliders(isRTL: false, shouldTryUpAndDown: true, shouldOrientationBeHorizontal: true);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractSlidersRTL()
//         {
//             CanUseKeyboardToInteractWithSliders(isRTL: true);
//             CanUseKeyboardToInteractWithSliders(isRTL: false, shouldTryUpAndDown: true);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithSlidersRTLHorizontalOrientation()
//         {
//             CanUseKeyboardToInteractWithSliders(isRTL: true, shouldOrientationBeHorizontal: true);
//             CanUseKeyboardToInteractWithSliders(isRTL: true, shouldTryUpAndDown: true, shouldOrientationBeHorizontal: true);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithTextBoxesForRGBLTR()
//         {
//             CanUseKeyboardToInteractWithTextBoxesForRGB(isRTL: false);
//         }
//
//         [TestMethod]
//         [TestProperty("Hosting:Mode", "WPF")]
//         public void CanUseKeyboardToInteractWithTextBoxesForRGBRTL()
//         {
//             CanUseKeyboardToInteractWithTextBoxesForRGB(isRTL: true);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithTextBoxesForAlphaLTR()
//         {
//             CanUseKeyboardToInteractWithTextBoxesForAlpha(isRTL: false);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithTextBoxesForAlphaRTL()
//         {
//             CanUseKeyboardToInteractWithTextBoxesForAlpha(isRTL: true);
//         }
//
//         [TestMethod]
//         [TestProperty("Hosting:Mode", "WPF")]
//         public void CanUseKeyboardToInteractWithTextBoxesForHSVLTR()
//         {
//             CanUseKeyboardToInteractWithTextBoxesForHSV(isRTL: false);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithTextBoxesForHSVRTL()
//         {
//             CanUseKeyboardToInteractWithTextBoxesForHSV(isRTL: true);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithTextBoxesForHexLTR()
//         {
//             CanUseKeyboardToInteractWithTextBoxesForHex(isRTL: false);
//         }
//
//         [TestMethod]
//         public void CanUseKeyboardToInteractWithTextBoxesForHexRTL()
//         {
//             CanUseKeyboardToInteractWithTextBoxesForHex(isRTL: true);
//         }
//
//         public void CanUseKeyboardToInteractWithColorSpectrum(bool isRTL)
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha))
//             {
//                 SetIsRTL(isRTL);
//
//                 Log.Comment("Give initial keyboard focus to the color spectrum.");
//                 FindElement.ById(ColorSpectrumAutomationId).SetFocus();
//                 Wait.ForIdle();
//
//                 VerifyElementIsFocused(ColorSpectrumAutomationId);
//                 VerifySelectionEllipsePosition(0, 0);
//
//                 Log.Comment("Keyboard to the right and left first.  We expect this to change the hue by 1 each time (+/- 4 to one RGB channel), applying wrapping.");
//                 PressKeyAndWaitForColorChange(isRTL ? Key.Left : Key.Right);
//                 VerifyColorIsSelected(255, 4, 0);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(1, 0);
//                 PressKeyAndWaitForColorChange(isRTL ? Key.Right : Key.Left);
//                 VerifyColorIsSelected(255, 0, 0);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(0, 0);
//                 PressKeyAndWaitForColorChange(isRTL ? Key.Right : Key.Left);
//                 VerifyColorIsSelected(255, 0, 4);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(256, 0);
//                 PressKeyAndWaitForColorChange(isRTL ? Key.Left : Key.Right);
//                 VerifyColorIsSelected(255, 0, 0);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(0, 0);
//
//                 Log.Comment("Now hold control and keyboard to the right and left again.  We expect this to jump to the next named color each time, applying wrapping.");
//                 PressKeyAndWaitForColorChange(isRTL ? Key.Left : Key.Right, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 119, 0);
//                 VerifyColorNameIsSelected("Orange");
//                 VerifySelectionEllipsePosition(20, 0);
//                 PressKeyAndWaitForColorChange(isRTL ? Key.Right : Key.Left, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 0, 13);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(255, 0);
//                 PressKeyAndWaitForColorChange(isRTL ? Key.Right : Key.Left, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 0, 170);
//                 VerifyColorNameIsSelected("Pink");
//                 VerifySelectionEllipsePosition(228, 0);
//                 PressKeyAndWaitForColorChange(isRTL ? Key.Left : Key.Right, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 0, 13);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(255, 0);
//
//                 Log.Comment("Next, keyboard up and down.  We expect this to change the saturation by 1 each time (+/- 2.5 to both non-max RGB channels), applying wrapping.");
//                 PressKeyAndWaitForColorChange(Key.Down);
//                 VerifyColorIsSelected(255, 3, 15);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(255, 3);
//                 PressKeyAndWaitForColorChange(Key.Up);
//                 VerifyColorIsSelected(255, 0, 13);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(255, 0);
//                 PressKeyAndWaitForColorChange(Key.Up);
//                 VerifyColorIsSelected(255, 255, 255);
//                 VerifyColorNameIsSelected("White");
//                 VerifySelectionEllipsePosition(255, 256);
//                 PressKeyAndWaitForColorChange(Key.Down);
//                 VerifyColorIsSelected(255, 0, 13);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(255, 0);
//
//                 Log.Comment("Now hold control keyboard up and down again.  We expect this to jump to the next named color each time, applying wrapping.");
//                 PressKeyAndWaitForColorChange(Key.Down, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 153, 158);
//                 VerifyColorNameIsSelected("Rose");
//                 VerifySelectionEllipsePosition(255, 154);
//                 PressKeyAndWaitForColorChange(Key.Up, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 38, 49);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(255, 38);
//                 PressKeyAndWaitForColorChange(Key.Up, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 242, 243);
//                 VerifyColorNameIsSelected("White");
//                 VerifySelectionEllipsePosition(255, 243);
//                 PressKeyAndWaitForColorChange(Key.Down, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 41, 52);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(255, 41);
//                 PressKeyAndWaitForColorChange(Key.Down, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 153, 158);
//                 VerifyColorNameIsSelected("Rose");
//                 VerifySelectionEllipsePosition(255, 154);
//                 PressKeyAndWaitForColorChange(Key.Up, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 38, 49);
//                 VerifyColorNameIsSelected("Red");
//                 VerifySelectionEllipsePosition(255, 38);
//             }
//         }
//
//         public void CanUseKeyboardToInteractWithSliders(bool isRTL, bool shouldTryUpAndDown = false, bool shouldOrientationBeHorizontal = false)
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 SetIsRTL(isRTL);
//                 SetIsHorizontalOrientation(shouldOrientationBeHorizontal);
//                 Key increaseKey = shouldTryUpAndDown ? Key.Up : isRTL ? Key.Left : Key.Right;
//                 Key decreaseKey = shouldTryUpAndDown ? Key.Down : isRTL ? Key.Right : Key.Left;
//
//                 Log.Comment("Initialize the color to what it was at the end of CanUseKeyboardToInteractWithColorSpectrum.");
//                 SetColorSpectrumColor(255, 38, 49);
//
//                 Log.Comment("Give initial keyboard focus to the color spectrum.");
//                 FindElement.ById(ColorSpectrumAutomationId).SetFocus();
//                 Wait.ForIdle();
//
//                 Log.Comment("Give keyboard focus to the value slider using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(ThirdDimensionAutomationId);
//
//                 Log.Comment("Keyboard to the right and left first.  We expect this to change the value by 1 each time (+/- 2.5 to the max RGB channel), but not wrap.");
//                 KeyboardHelper.PressKey(decreaseKey);
//                 VerifyColorIsSelected(252, 38, 49);
//                 VerifyColorNameIsSelected("Red");
//                 KeyboardHelper.PressKey(increaseKey);
//                 VerifyColorIsSelected(255, 38, 49);
//                 VerifyColorNameIsSelected("Red");
//                 KeyboardHelper.PressKey(increaseKey);
//                 VerifyColorIsSelected(255, 38, 49);
//                 VerifyColorNameIsSelected("Red");
//
//                 Log.Comment("Now hold control and keyboard to the right and left again.  We expect this to jump to the next named color each time, but not wrap.");
//                 KeyboardHelper.PressKey(decreaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(102, 15, 20);
//                 VerifyColorNameIsSelected("Dark red");
//                 KeyboardHelper.PressKey(decreaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(15, 2, 3);
//                 VerifyColorNameIsSelected("Black");
//                 KeyboardHelper.PressKey(decreaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(0, 0, 0);
//                 VerifyColorNameIsSelected("Black");
//                 KeyboardHelper.PressKey(increaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(105, 16, 20);
//                 VerifyColorNameIsSelected("Dark red");
//                 KeyboardHelper.PressKey(increaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(214, 32, 41);
//                 VerifyColorNameIsSelected("Red");
//                 KeyboardHelper.PressKey(increaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 38, 49);
//                 VerifyColorNameIsSelected("Red");
//
//                 Log.Comment("Give keyboard focus to the alpha slider using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(AlphaSliderAutomationId);
//
//                 Log.Comment("Keyboard to the right and left first.  We expect this to change the alpha by 2.5 each time, but not wrap.");
//                 KeyboardHelper.PressKey(decreaseKey);
//                 VerifyColorIsSelected(255, 38, 49, 252);
//                 KeyboardHelper.PressKey(increaseKey);
//                 VerifyColorIsSelected(255, 38, 49, 255);
//                 KeyboardHelper.PressKey(increaseKey);
//                 VerifyColorIsSelected(255, 38, 49, 255);
//
//                 Log.Comment("Now hold control and keyboard to the right and left again.  We expect this to change the alpha by 25 each time, but not wrap.  We also expect us to snap to multiples of 10 if we're between them.");
//                 KeyboardHelper.PressKey(decreaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 38, 49, 230);
//                 KeyboardHelper.PressKey(increaseKey);
//                 VerifyColorIsSelected(255, 38, 49, 232);
//                 KeyboardHelper.PressKey(decreaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 38, 49, 230);
//                 KeyboardHelper.PressKey(increaseKey, ModifierKey.Control);
//                 VerifyColorIsSelected(255, 38, 49, 255);
//             }
//         }
//
//         public void CanUseKeyboardToInteractWithTextBoxesForRGB(bool isRTL)
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 SetIsRTL(isRTL);
//
//                 Log.Comment("Initialize the color to a known value.");
//                 SetColorSpectrumColor(255, 0, 0);
//
//                 Log.Comment("Give initial keyboard focus to red text box.");
//                 FindElement.ById(RedTextBoxAutomationId).SetFocus();
//                 Wait.ForIdle();
//
//                 Log.Comment("Select the current text, and then type in a new number.");
//                 Edit redTextBox = new Edit(FindElement.ById(RedTextBoxAutomationId));
//                 redTextBox.DocumentRange.Select();
//                 KeyboardHelper.EnterText(redTextBox, "128");
//                 VerifyColorIsSelected(128, 0, 0);
//
//                 Log.Comment("Give keyboard focus to the green text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(GreenTextBoxAutomationId);
//
//                 Log.Comment("Select the current text, and then type in a new number.");
//                 Edit greenTextBox = new Edit(FindElement.ById(GreenTextBoxAutomationId));
//                 greenTextBox.DocumentRange.Select();
//                 KeyboardHelper.EnterText(greenTextBox, "128");
//                 VerifyColorIsSelected(128, 128, 0);
//
//                 Log.Comment("Give keyboard focus to the blue text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(BlueTextBoxAutomationId);
//
//                 Log.Comment("Select the current text, and then type in a new number.");
//                 Edit blueTextBox = new Edit(FindElement.ById(BlueTextBoxAutomationId));
//                 blueTextBox.DocumentRange.Select();
//                 KeyboardHelper.EnterText(blueTextBox, "128");
//                 VerifyColorIsSelected(128, 128, 128);
//             }
//         }
//
//         public void CanUseKeyboardToInteractWithTextBoxesForAlpha(bool isRTL)
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 SetIsRTL(isRTL);
//
//                 Log.Comment("Initialize the color to a known value.");
//                 SetColorSpectrumColor(255, 0, 0);
//
//                 Log.Comment("Give initial keyboard focus to alpha text box.");
//                 FindElement.ById(AlphaTextBoxAutomationId).SetFocus();
//                 Wait.ForIdle();
//
//                 Log.Comment("Select the current text, and then type in a new number.");
//                 Edit alphaTextBox = new Edit(FindElement.ById(AlphaTextBoxAutomationId));
//                 alphaTextBox.DocumentRange.Select();
//                 KeyboardHelper.EnterText(alphaTextBox, "50%");
//                 VerifyColorIsSelected(255, 0, 0, 128);
//             }
//         }
//
//         public void CanUseKeyboardToInteractWithTextBoxesForHSV(bool isRTL)
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 SetIsRTL(isRTL);
//
//                 Log.Comment("Give initial keyboard focus to the color representation combobox.");
//                 FindElement.ById(ColorRepresentationComboBoxAutomationId).SetFocus();
//
//                 Log.Comment("Switch to HSV by pressing down.");
//                 KeyboardHelper.PressKey(Key.Down);
//
//                 Log.Comment("Give keyboard focus to the hue text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(HueTextBoxAutomationId);
//
//                 Log.Comment("Select the current text, and then type in a new number.");
//                 Edit hueTextBox = new Edit(FindElement.ById(HueTextBoxAutomationId));
//                 hueTextBox.DocumentRange.Select();
//                 KeyboardHelper.EnterText(hueTextBox, "120");
//                 VerifyColorIsSelected(0, 255, 0);
//
//                 Log.Comment("Give keyboard focus to the saturation text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(SaturationTextBoxAutomationId);
//
//                 Log.Comment("Select the current text, and then type in a new number.");
//                 Edit saturationTextBox = new Edit(FindElement.ById(SaturationTextBoxAutomationId));
//                 saturationTextBox.DocumentRange.Select();
//                 KeyboardHelper.EnterText(saturationTextBox, "50");
//                 VerifyColorIsSelected(128, 255, 128);
//
//                 Log.Comment("Give keyboard focus to the value text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(ValueTextBoxAutomationId);
//
//                 Log.Comment("Select the current text, and then type in a new number.");
//                 Edit valueTextBox = new Edit(FindElement.ById(ValueTextBoxAutomationId));
//                 valueTextBox.DocumentRange.Select();
//                 KeyboardHelper.EnterText(valueTextBox, "50");
//                 VerifyColorIsSelected(64, 128, 64);
//             }
//         }
//
//         public void CanUseKeyboardToInteractWithTextBoxesForHex(bool isRTL)
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 SetIsRTL(isRTL);
//
//                 Log.Comment("Give keyboard focus to the hex text box.");
//                 FindElement.ById(HexTextBoxAutomationId).SetFocus();
//                 Wait.ForIdle();
//
//                 Log.Comment("Select the current text, and then type in a new number.");
//                 Edit hexTextBox = new Edit(FindElement.ById(HexTextBoxAutomationId));
//                 hexTextBox.DocumentRange.Select();
//                 KeyboardHelper.EnterText(hexTextBox, "#FFFF0000");
//                 VerifyColorIsSelected(255, 0, 0);
//             }
//         }
//
//         [TestMethod]
//         public void VerifySpectrumTakesFocusOnPointerPress()
//         {
//             using (var setup = SetupColorPickerTest())
//             {
//                 TapOnColorSpectrumAndWaitForColorChange(0.5, 0.5);
//                 VerifyElementIsFocused(ColorSpectrumAutomationId);
//             }
//         }
//
//         [TestMethod]
//         public void VerifyMoreButtonShowsAndHidesTextInputFields()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.EnableMoreButton | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 ToggleButton moreButton = new ToggleButton(FindElement.ById(MoreButtonAutomationId));
//
//                 Verify.IsNotNull(TryFindElement.ById(ColorSpectrumAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(ThirdDimensionAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(AlphaSliderAutomationId));
//                 Verify.IsNull(TryFindElement.ById(ColorRepresentationComboBoxAutomationId));
//                 Verify.IsNull(TryFindElement.ById(RedTextBoxAutomationId));
//                 Verify.IsNull(TryFindElement.ById(GreenTextBoxAutomationId));
//                 Verify.IsNull(TryFindElement.ById(BlueTextBoxAutomationId));
//                 Verify.IsNull(TryFindElement.ById(AlphaTextBoxAutomationId));
//                 Verify.IsNull(TryFindElement.ById(HexTextBoxAutomationId));
//
//                 moreButton.Toggle();
//                 Wait.ForIdle();
//
//                 Verify.IsNotNull(TryFindElement.ById(ColorSpectrumAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(ThirdDimensionAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(AlphaSliderAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(ColorRepresentationComboBoxAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(RedTextBoxAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(GreenTextBoxAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(BlueTextBoxAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(AlphaTextBoxAutomationId));
//                 Verify.IsNotNull(TryFindElement.ById(HexTextBoxAutomationId));
//             }
//         }
//
//         [TestMethod]
//         public void VerifyInvalidTextEntryIsReverted()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 SetMinSaturation(50);
//                 SetMinValue(50);
//
//                 Verify.AreEqual("255", (new Edit(FindElement.ById(RedTextBoxAutomationId))).Value);
//
//                 Log.Comment("First, write a completely invalid string, and make sure that it's reverted after focus is lost.");
//                 WriteInTextBox(RedTextBoxAutomationId, "foo");
//                 KeyboardHelper.PressKey(Key.Tab);
//
//                 Verify.AreEqual("255", (new Edit(FindElement.ById(RedTextBoxAutomationId))).Value);
//
//                 Log.Comment("Next, write a string that represents a valid color, but one that is out of range, and then tab away.  We should snap to the closest color in range in this case.");
//                 WriteInTextBox(RedTextBoxAutomationId, "0");
//                 KeyboardHelper.PressKey(Key.Tab);
//
//                 Verify.AreEqual("128", (new Edit(FindElement.ById(RedTextBoxAutomationId))).Value);
//                 Verify.AreEqual("64", (new Edit(FindElement.ById(GreenTextBoxAutomationId))).Value);
//                 Verify.AreEqual("64", (new Edit(FindElement.ById(BlueTextBoxAutomationId))).Value);
//
//                 Log.Comment("Now we'll do the same with the HSV text boxes.");
//                 WriteInTextBox(HueTextBoxAutomationId, "foo");
//                 KeyboardHelper.PressKey(Key.Tab);
//
//                 Verify.AreEqual("0", (new Edit(FindElement.ById(HueTextBoxAutomationId))).Value);
//
//                 WriteInTextBox(SaturationTextBoxAutomationId, "0");
//                 KeyboardHelper.PressKey(Key.Tab);
//
//                 Verify.AreEqual("50", (new Edit(FindElement.ById(SaturationTextBoxAutomationId))).Value);
//
//                 WriteInTextBox(ValueTextBoxAutomationId, "0");
//                 KeyboardHelper.PressKey(Key.Tab);
//
//                 Verify.AreEqual("50", (new Edit(FindElement.ById(ValueTextBoxAutomationId))).Value);
//
//                 Log.Comment("And similarly for the hex text box.");
//
//                 WriteInTextBox(HexTextBoxAutomationId, "#foo");
//                 KeyboardHelper.PressKey(Key.Tab);
//
//                 Verify.AreEqual("#804040", (new Edit(FindElement.ById(HexTextBoxAutomationId))).Value);
//
//                 WriteInTextBox(HexTextBoxAutomationId, "#000000");
//                 KeyboardHelper.PressKey(Key.Tab);
//
//                 Verify.AreEqual("#804040", (new Edit(FindElement.ById(HexTextBoxAutomationId))).Value);
//             }
//         }
//
//         [TestMethod]
//         [TestProperty("Hosting:Mode", "WPF")]
//         public void VerifyHexStringAndColorAreInSync()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 SetColorSpectrumColor(128, 255, 255);
//                 Verify.AreEqual("#80FFFF", (new Edit(FindElement.ById(HexTextBoxAutomationId))).Value);
//             }
//         }
//
//         [TestMethod]
//         public void VerifyEllipsePositionIsClampedToSpectrumBounds()
//         {
//             using (var setup = SetupColorPickerTest())
//             {
//                 SetMinSaturation(50);
//                 InputHelper.Tap(FindElement.ById("ColorWhiteButton"));
//
//                 VerifySelectionEllipsePosition(0, 256);
//             }
//         }
//
//         [TestMethod]
//         public void VerifyCanChangeColorWithTouchWithoutPanning()
//         {
//             using (var setup = SetupColorPickerTest())
//             {
//                 ScrollImplementation scrollViewer = new ScrollImplementation(FindElement.ById("ColorPickerScrollViewer"));
//                 double initialVerticalScroll = scrollViewer.VerticalScrollPercent;
//
//                 ColorSpectrum colorSpectrum = new ColorSpectrum(FindElement.ById(ColorSpectrumAutomationId));
//                 InputHelper.DragDistance(colorSpectrum, 50, Direction.North);
//
//                 Verify.AreEqual(initialVerticalScroll, scrollViewer.VerticalScrollPercent);
//             }
//         }
//
//         [TestMethod]
//         public void VerifyMoreButtonRespondsToThemeChanges()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableMoreButton | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 Button themeLightButton = new Button(FindElement.ById("ThemeLightButton"));
//                 Button themeDarkButton = new Button(FindElement.ById("ThemeDarkButton"));
//
//                 TextBlock moreButtonForegroundTextBlock = new TextBlock(FindElement.ById("MoreButtonForegroundTextBlock"));
//                 TextBlock moreButtonBackgroundTextBlock = new TextBlock(FindElement.ById("MoreButtonBackgroundTextBlock"));
//                 TextBlock moreButtonBorderBrushTextBlock = new TextBlock(FindElement.ById("MoreButtonBorderBrushTextBlock"));
//
//                 themeLightButton.Invoke();
//                 Wait.ForIdle();
//
//                 Verify.AreEqual<string>("#FF000000", moreButtonForegroundTextBlock.DocumentText);
//                 Verify.AreEqual<string>("#00FFFFFF", moreButtonBackgroundTextBlock.DocumentText);
//                 Verify.AreEqual<string>("#00FFFFFF", moreButtonBorderBrushTextBlock.DocumentText);
//
//                 themeDarkButton.Invoke();
//                 Wait.ForIdle();
//
//                 Verify.AreEqual<string>("#FFFFFFFF", moreButtonForegroundTextBlock.DocumentText);
//                 Verify.AreEqual<string>("#00FFFFFF", moreButtonBackgroundTextBlock.DocumentText);
//                 Verify.AreEqual<string>("#00FFFFFF", moreButtonBorderBrushTextBlock.DocumentText);
//             }
//         }
//
//         [TestMethod]
//         public void VerifyAutomationNamesAreCorrect()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 Verify.AreEqual("Color picker", FindElement.ById(ColorSpectrumAutomationId).Name);
//                 Verify.AreEqual("Value (Brightness)", FindElement.ById(ThirdDimensionAutomationId).Name);
//                 Verify.AreEqual("Opacity", FindElement.ById(AlphaSliderAutomationId).Name);
//                 Verify.AreEqual("Color model", FindElement.ById(ColorRepresentationComboBoxAutomationId).Name);
//                 Verify.AreEqual("Red", FindElement.ById(RedTextBoxAutomationId).Name);
//                 Verify.AreEqual("Green", FindElement.ById(GreenTextBoxAutomationId).Name);
//                 Verify.AreEqual("Blue", FindElement.ById(BlueTextBoxAutomationId).Name);
//                 Verify.AreEqual("RGB hex", FindElement.ById(HexTextBoxAutomationId).Name);
//                 SelectTextBoxes(ColorChannels.HSV);
//                 Verify.AreEqual("Hue", FindElement.ById(HueTextBoxAutomationId).Name);
//                 Verify.AreEqual("Saturation", FindElement.ById(SaturationTextBoxAutomationId).Name);
//                 Verify.AreEqual("Value (Brightness)", FindElement.ById(ValueTextBoxAutomationId).Name);
//                 Verify.AreEqual("HSV hex", FindElement.ById(HexTextBoxAutomationId).Name);
//             }
//         }
//
//         // Color change for ColorPicker trigs CColorSpectrum::RaiseColorChanged
//         // Verify it's not crashed when ColorSpectrum is invisiable
//         [TestMethod]
//         public void ValidateNoCrashWhenWhenColorSpectrumInvisibleAndColorChanged()
//         {
//             using (var setup = SetupColorPickerTest())
//             {
//                 Log.Comment("Hide ColorSpectrum");
//                 CheckBox isColorSpectrumVisible = new CheckBox(FindElement.ById("ColorSpectrumVisibleCheckBox"));
//                 isColorSpectrumVisible.Toggle();
//                 Wait.ForIdle();
//
//                 Log.Comment("Change Color");
//                 InputHelper.Tap(FindElement.ById("ColorWhiteButton"));
//             }
//         }
//
//         [TestMethod]
//         public void VerifyColorSpectrumAutomationValuesAreCorrect()
//         {
//             using (var setup = SetupColorPickerTest())
//             {
//                 ColorSpectrum colorSpectrum = new ColorSpectrum(FindElement.ById(ColorSpectrumAutomationId));
//                 colorSpectrum.SetFocus();
//
//                 Verify.AreEqual("2D slider", colorSpectrum.LocalizedControlType);
//                 Verify.AreEqual("2D navigation with arrow keys", colorSpectrum.HelpText);
//
//                 Verify.AreEqual("Red, Hue 0, Saturation 100, Value 100", colorSpectrum.Value);
//                 KeyboardHelper.PressKey(Key.Right);
//                 Verify.AreEqual("Red, Hue 1, Saturation 100, Value 100", colorSpectrum.Value);
//                 KeyboardHelper.PressKey(Key.Right, ModifierKey.Control);
//                 Verify.AreEqual("Orange, Hue 28, Saturation 100, Value 100", colorSpectrum.Value);
//             }
//         }
//
//         [TestMethod]
//         public void VerifyThirdDimensionSliderAutomationValuesAreCorrect()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 ColorPickerSlider thirdDimensionSlider = new ColorPickerSlider(FindElement.ById(ThirdDimensionAutomationId));
//                 thirdDimensionSlider.SetFocus();
//
//                 Verify.AreEqual("100, Red", thirdDimensionSlider.Value);
//                 KeyboardHelper.PressKey(Key.Left);
//                 Verify.AreEqual("99, Red", thirdDimensionSlider.Value);
//                 KeyboardHelper.PressKey(Key.Left, ModifierKey.Control);
//                 Verify.AreEqual("46, Dark red", thirdDimensionSlider.Value);
//             }
//         }
//
//         // In the OS repo, we run tests against rs_onecore_dep_uxp_dev, and localization resources are not created for OS builds from that repo.
//         // As a result, we can't test localization there.  The functionality that this is testing is just whether ColorPicker is properly
//         // loading resources instead of having any hard-coded strings, so running this test only in the MUXControls repo should be OK -
//         // we don't have any OS repo-specific code in what this is testing, so nothing should change between the two repos.
//         //[TestMethod] Bug 24048408: DCPP: Multiple MUXC interaction tests fail in CatGates due to taking too long to execute
//         public void VerifyStringsAreLocalizedCorrectly()
//         {
//             // First gather all of the text from the English version of the test page.
//             List<string> englishStrings = new List<string>();
//
//             Log.Comment("Collecting en-US strings...");
//
//             using (var setup = SetupColorPickerTest(
//                 TestOptions.EnableAlpha |
//                 TestOptions.EnableMoreButton |
//                 TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 GetLocalizedText(englishStrings);
//             }
//
//             // Now switch to Japanese and gather all of the new text from the test page.
//             List<string> otherLanguageStrings = new List<string>();
//
//             Log.Comment("Collecting ja-JP strings...");
//
//             using (var setup = SetupColorPickerTest(
//                 TestOptions.EnableAlpha |
//                 TestOptions.EnableMoreButton |
//                 TestOptions.EnableNonEnglishLanguage |
//                 TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 GetLocalizedText(otherLanguageStrings);
//             }
//
//             LocalizationHelper.CompareStringSets(englishStrings, otherLanguageStrings);
//         }
//
//         [TestMethod]
//         public void VerifyTabFocusOrder()
//         {
//             using (var setup = SetupColorPickerTest(TestOptions.EnableAlpha | TestOptions.DisableColorSpectrumLoadWait))
//             {
//                 Log.Comment("Give initial keyboard focus to the color spectrum.");
//                 FindElement.ById(ColorSpectrumAutomationId).SetFocus();
//                 Wait.ForIdle();
//
//                 Log.Comment("Give keyboard focus to the value slider using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(ThirdDimensionAutomationId);
//
//                 Log.Comment("Give keyboard focus to the alpha slider using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(AlphaSliderAutomationId);
//
//                 Log.Comment("Give keyboard focus to the Color Representation ComboBox using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(ColorRepresentationComboBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the red text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(RedTextBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the green text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(GreenTextBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the blue text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(BlueTextBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the alpha text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(AlphaTextBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the hex text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(HexTextBoxAutomationId);
//
//                 SetIsHorizontalOrientation(true);
//
//                 Log.Comment("Give initial keyboard focus to the color spectrum.");
//                 FindElement.ById(ColorSpectrumAutomationId).SetFocus();
//                 Wait.ForIdle();
//
//                 Log.Comment("Give keyboard focus to the value slider using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(ThirdDimensionAutomationId);
//
//                 Log.Comment("Give keyboard focus to the alpha slider using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(AlphaSliderAutomationId);
//
//                 Log.Comment("Give keyboard focus to the hex text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(HexTextBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the Color Representation ComboBox using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(ColorRepresentationComboBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the red text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(RedTextBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the green text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(GreenTextBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the blue text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(BlueTextBoxAutomationId);
//
//                 Log.Comment("Give keyboard focus to the alpha text box using tab.");
//                 KeyboardHelper.PressKey(Key.Tab);
//                 VerifyElementIsFocused(AlphaTextBoxAutomationId);
//             }
//         }
//
//         private void GetLocalizedText(IList<string> stringList)
//         {
//             ColorSpectrum colorSpectrum = new ColorSpectrum(FindElement.ById(ColorSpectrumAutomationId));
//             stringList.Add(colorSpectrum.Value);
//             stringList.Add(colorSpectrum.LocalizedControlType);
//             stringList.Add(colorSpectrum.HelpText);
//
//             ColorPickerSlider thirdDimensionSlider = new ColorPickerSlider(FindElement.ById(ThirdDimensionAutomationId));
//             stringList.Add(thirdDimensionSlider.Name);
//             stringList.Add(thirdDimensionSlider.Value);
//
//             ColorPickerSlider alphaSlider = new ColorPickerSlider(FindElement.ById(AlphaSliderAutomationId));
//             stringList.Add(alphaSlider.Name);
//
//             ToggleButton moreButton = new ToggleButton(FindElement.ById(MoreButtonAutomationId));
//             stringList.Add(moreButton.Name);
//             stringList.Add(FindElement.ById(MoreButtonLabelAutomationId).Name);
//
//             moreButton.Toggle();
//             Wait.ForIdle();
//
//             stringList.Add(moreButton.Name);
//             stringList.Add(FindElement.ById(MoreButtonLabelAutomationId).Name);
//
//             ComboBox colorRepresentationComboBox = new ComboBox(FindElement.ById(ColorRepresentationComboBoxAutomationId));
//             stringList.Add(colorRepresentationComboBox.Name);
//
//             stringList.Add(FindElement.ById(RedLabelAutomationId).Name);
//             stringList.Add(FindElement.ById(GreenLabelAutomationId).Name);
//             stringList.Add(FindElement.ById(BlueLabelAutomationId).Name);
//
//             SelectTextBoxes(ColorChannels.HSV);
//
//             stringList.Add(FindElement.ById(HueLabelAutomationId).Name);
//             stringList.Add(FindElement.ById(SaturationLabelAutomationId).Name);
//             stringList.Add(FindElement.ById(ValueLabelAutomationId).Name);
//
//             stringList.Add(FindElement.ById(AlphaLabelAutomationId).Name);
//         }
//
//         private ColorPickerTestSetupHelper SetupColorPickerTest(TestOptions options = TestOptions.None)
//         {
//             var setup = new ColorPickerTestSetupHelper(
//                 "ColorPicker Tests",
//                 languageOverride : (options & TestOptions.EnableNonEnglishLanguage) == TestOptions.EnableNonEnglishLanguage ? "ja-JP" : "");
//
//             // On CHK builds, waiting for the color spectrum to load can take a while, which contributes
//             // several seconds towards CatGates' 30-second timeout. As a result, we should only wait
//             // for it to load in tests where we're actually going to be interacting with it.
//             if ((options & TestOptions.DisableColorSpectrumLoadWait) != TestOptions.DisableColorSpectrumLoadWait)
//             {
//                 // This is a bit of a hack - the ColorSpectrum can't be interacted with until it loads asynchronously,
//                 // so we need to wait until that has occurred.  There's no publicly accessible event saying that it's loaded,
//                 // so to get around that, we toggle the value of a check box in the test app to indicate that loading has completed.
//                 Log.Comment("Wait until the color spectrum image has loaded.");
//                 CheckBox cb = new CheckBox(FindElement.ById("ColorSpectrumLoadedCheckBox"));
//
//                 using (var waiter = cb.GetToggledWaiter())
//                 {
//                     if (cb.ToggleState != ToggleState.On)
//                     {
//                         waiter.Wait();
//                     }
//                 }
//
//                 Log.Comment("Color spectrum image loaded.");
//             }
//
//             SetIsAlphaEnabled((options & TestOptions.EnableAlpha) == TestOptions.EnableAlpha);
//             SetIsMoreButtonEnabled((options & TestOptions.EnableMoreButton) == TestOptions.EnableMoreButton);
//
//             setup.StartTrackingColorChanges();
//
//             Wait.ForIdle();
//
//             return setup;
//         }
//
//         private void SetColorSpectrumColor(int red, int green, int blue, int alpha = 255)
//         {
//             ColorSpectrum colorSpectrum = new ColorSpectrum(FindElement.ById(ColorSpectrumAutomationId));
//
//             string colorString = "#";
//
//             // "X2" means "output as hex and pad with zeroes if needed to make the string two characters long".
//             colorString += alpha.ToString("X2");
//             colorString += red.ToString("X2");
//             colorString += green.ToString("X2");
//             colorString += blue.ToString("X2");
//
//             using (var waiter = colorSpectrum.GetColorChangedWaiter())
//             {
//                 Log.Comment("Setting ColorSpectrum color to {0}.", colorString);
//                 colorSpectrum.SetValue(colorString);
//             }
//
//             Wait.ForIdle();
//         }
//
//         private void TapOnColorSpectrum(double xPercent, double yPercent)
//         {
//             ColorSpectrum colorSpectrum = new ColorSpectrum(FindElement.ById(ColorSpectrumAutomationId));
//
//             int colorSpectrumWidth = colorSpectrum.BoundingRectangle.Width;
//             int colorSpectrumHeight = colorSpectrum.BoundingRectangle.Height;
//
//             double xPosition = xPercent * (colorSpectrumWidth - 1);
//             double yPosition = yPercent * (colorSpectrumHeight - 1);
//
//             InputHelper.Tap(colorSpectrum, xPosition, yPosition);
//         }
//
//         private void TapOnColorSpectrumAndWaitForColorChange(double xPercent, double yPercent)
//         {
//             ColorSpectrum colorSpectrum = new ColorSpectrum(FindElement.ById(ColorSpectrumAutomationId));
//
//             int colorSpectrumWidth = colorSpectrum.BoundingRectangle.Width;
//             int colorSpectrumHeight = colorSpectrum.BoundingRectangle.Height;
//
//             double xPosition = xPercent * (colorSpectrumWidth - 1);
//             double yPosition = yPercent * (colorSpectrumHeight - 1);
//
//             ColorPickerTestSetupHelper.Current.ExecuteAndWaitForColorChange(
//                 () => InputHelper.Tap(colorSpectrum, xPosition, yPosition)
//             );
//         }
//
//         private void PressKeyAndWaitForColorChange(Key key, ModifierKey modifierKey = ModifierKey.None, uint numPresses = 1)
//         {
//             ColorPickerTestSetupHelper.Current.ExecuteAndWaitForColorChange(() => KeyboardHelper.PressKey(key, modifierKey, numPresses));
//         }
//
//         private void WriteInTextBox(string textBoxName, string s)
//         {
//             Log.Comment("Retrieve text box with name '{0}'.", textBoxName);
//
//             if (textBoxName == RedTextBoxAutomationId ||
//                 textBoxName == GreenTextBoxAutomationId ||
//                 textBoxName == BlueTextBoxAutomationId)
//             {
//                 SelectTextBoxes(ColorChannels.RGB);
//             }
//             else if (textBoxName == HueTextBoxAutomationId ||
//                 textBoxName == SaturationTextBoxAutomationId ||
//                 textBoxName == ValueTextBoxAutomationId)
//             {
//                 SelectTextBoxes(ColorChannels.HSV);
//             }
//             else if (textBoxName == AlphaTextBoxAutomationId)
//             {
//                 SetIsAlphaEnabled(true);
//             }
//
//             KeyboardHelper.EnterText(new Edit(FindElement.ById(textBoxName)), s);
//         }
//
//         private void SelectTextBoxes(ColorChannels channels)
//         {
//             ColorChannels currentlySelectedChannels = GetCurrentlySelectedChannels();
//             ComboBox colorRepresentationComboBox = new ComboBox(FindElement.ById(ColorRepresentationComboBoxAutomationId));
//
//             Log.Comment("{0} text boxes requested, {1} text boxes currently selected.", channels == ColorChannels.RGB ? "RGB" : "HSV", currentlySelectedChannels == ColorChannels.RGB ? "RGB" : "HSV");
//
//             if (currentlySelectedChannels != channels)
//             {
//                 currentlySelectedChannels = channels;
//
//                 Log.Comment("Change the combo box to select {0} text boxes.", channels == ColorChannels.RGB ? "RGB" : "HSV");
//                 colorRepresentationComboBox.SelectItemById(channels == ColorChannels.RGB ? RGBComboBoxItemAutomationId : HSVComboBoxItemAutomationId);
//             }
//             else
//             {
//                 Log.Comment("No need to change the combo box - we already have the right text box set selected.");
//             }
//         }
//
//         private void SetIsAlphaEnabled(bool isAlphaEnabled)
//         {
//             CheckBox isAlphaEnabledCheckBox = new CheckBox(FindElement.ById("AlphaEnabledCheckBox"));
//
//             if (isAlphaEnabled && isAlphaEnabledCheckBox.ToggleState != ToggleState.On ||
//                 !isAlphaEnabled && isAlphaEnabledCheckBox.ToggleState != ToggleState.Off)
//             {
//                 isAlphaEnabledCheckBox.Toggle();
//                 Wait.ForIdle();
//             }
//         }
//
//         private void SetIsMoreButtonEnabled(bool isMoreButtonEnabled)
//         {
//             CheckBox isMoreButtonVisibleCheckBox = new CheckBox(FindElement.ById("MoreButtonVisibleCheckBox"));
//
//             if (isMoreButtonEnabled && isMoreButtonVisibleCheckBox.ToggleState != ToggleState.On ||
//                 !isMoreButtonEnabled && isMoreButtonVisibleCheckBox.ToggleState != ToggleState.Off)
//             {
//                 isMoreButtonVisibleCheckBox.Toggle();
//                 Wait.ForIdle();
//             }
//         }
//
//         private bool GetIsRTL()
//         {
//             CheckBox isRTLCheckBox = new CheckBox(FindElement.ById("IsRtlCheckBox"));
//             return isRTLCheckBox.ToggleState == ToggleState.On;
//         }
//
//         private void SetIsRTL(bool isRTL)
//         {
//             CheckBox isRTLCheckBox = new CheckBox(FindElement.ById("IsRtlCheckBox"));
//
//             if (isRTL && isRTLCheckBox.ToggleState != ToggleState.On ||
//                 !isRTL && isRTLCheckBox.ToggleState != ToggleState.Off)
//             {
//                 isRTLCheckBox.Toggle();
//                 Wait.ForIdle();
//             }
//         }
//
//         private void SetIsHorizontalOrientation(bool shouldBeHorizontal)
//         {
//             ComboBox isRTLCheckBox = new ComboBox(FindElement.ById("OrientationComboBox"));
//             isRTLCheckBox.SelectItemById(shouldBeHorizontal ? "OrientationHorizontal" : "OrientationVertical");
//             Wait.ForIdle();
//         }
//
//         private void SetMinSaturation(double minSaturation)
//         {
//             Log.Comment("Setting MinSaturation to {0}.", minSaturation);
//             RangeValueSlider minSaturationSlider = new RangeValueSlider(FindElement.ById("MinimumSaturationSlider"));
//             SetSaturation(minSaturationSlider, minSaturation);
//         }
//
//         private void SetMaxSaturation(double maxSaturation)
//         {
//             Log.Comment("Setting MaxSaturation to {0}.", maxSaturation);
//             RangeValueSlider maxSaturationSlider = new RangeValueSlider(FindElement.ById("MaximumSaturationSlider"));
//             SetSaturation(maxSaturationSlider, maxSaturation);
//         }
//
//         private void SetSaturation(RangeValueSlider saturationSlider, double saturation)
//         {
//             if (saturationSlider.Value != saturation)
//             {
//                 CheckBox cb = new CheckBox(FindElement.ById("ColorSpectrumLoadedCheckBox"));
//
//                 using (var waiter = cb.GetToggledWaiter())
//                 {
//                     if (cb.ToggleState != ToggleState.Off)
//                     {
//                         cb.Toggle();
//                         Wait.ForIdle();
//                     }
//
//                     saturationSlider.SetValue(saturation);
//                     Log.Comment("Waiting for the color spectrum image to be reloaded...");
//                     waiter.Wait();
//                 }
//
//                 Log.Comment("Color spectrum image loaded.");
//             }
//         }
//
//         private void SetMinValue(double minValue)
//         {
//             Log.Comment("Setting MinValue to {0}.", minValue);
//             RangeValueSlider minValueSlider = new RangeValueSlider(FindElement.ById("MinimumValueSlider"));
//             SetValue(minValueSlider, minValue);
//         }
//
//         private void SetMaxValue(double maxValue)
//         {
//             Log.Comment("Setting MaxValue to {0}.", maxValue);
//             RangeValueSlider maxValueSlider = new RangeValueSlider(FindElement.ById("MaximumValueSlider"));
//             SetValue(maxValueSlider, maxValue);
//         }
//
//         private void SetValue(RangeValueSlider valueSlider, double value)
//         {
//             RangeValueSlider minValueSlider = new RangeValueSlider(FindElement.ById("MinimumValueSlider"));
//
//             if (minValueSlider.Value != value)
//             {
//                 minValueSlider.SetValue(value);
//                 // The value is tracked by the slider, meaning that we don't have to wait for
//                 // the color spectrum image to be reloaded in this circumstance.
//             }
//         }
//
//         private void VerifyColorIsSelected(int expectedRed, int expectedGreen, int expectedBlue, int expectedAlpha = 255)
//         {
//             VerifyColorIsSelectedCore(expectedRed, expectedGreen, expectedBlue, expectedAlpha, 1 /* allowedMargin */);
//         }
//
//         private void VerifySelectedColorIsNear(int expectedRed, int expectedGreen, int expectedBlue, int expectedAlpha = 255)
//         {
//             VerifyColorIsSelectedCore(expectedRed, expectedGreen, expectedBlue, expectedAlpha, 10 /* allowedMargin */);
//         }
//
//         private void VerifyColorIsSelectedCore(int expectedRed, int expectedGreen, int expectedBlue, int expectedAlpha, int allowedMargin)
//         {
//             Color currentColor = ColorPickerTestSetupHelper.Current.Color;
//
//             int actualRed = currentColor.R;
//             int actualGreen = currentColor.G;
//             int actualBlue = currentColor.B;
//             int actualAlpha = currentColor.A;
//
//             Log.Comment("Expecting the ColorPicker's selected color to be {8}RGBA = ({0}, {1}, {2}, {3}). Actual color is RGBA = ({4}, {5}, {6}, {7}).",
//                 expectedRed, expectedGreen, expectedBlue, expectedAlpha,
//                 actualRed, actualGreen, actualBlue, actualAlpha,
//                 allowedMargin > 0 ? string.Format("within {0} unit{1} of ", allowedMargin, allowedMargin != 1 ? "s" : "") : "");
//
//             Verify.IsLessThanOrEqual(expectedRed - allowedMargin, actualRed);
//             Verify.IsGreaterThanOrEqual(expectedRed + allowedMargin, actualRed);
//             Verify.IsLessThanOrEqual(expectedGreen - allowedMargin, actualGreen);
//             Verify.IsGreaterThanOrEqual(expectedGreen + allowedMargin, actualGreen);
//             Verify.IsLessThanOrEqual(expectedBlue - allowedMargin, actualBlue);
//             Verify.IsGreaterThanOrEqual(expectedBlue + allowedMargin, actualBlue);
//             Verify.IsLessThanOrEqual(expectedAlpha - allowedMargin, actualAlpha);
//             Verify.IsGreaterThanOrEqual(expectedAlpha + allowedMargin, actualAlpha);
//         }
//
//         private void VerifyColorNameIsSelected(string expectedName)
//         {
//             TextBlock selectedColorNameTextBlock = new TextBlock(FindElement.ById("SelectedColorNameTextBlock"));
//
//             string actualName = selectedColorNameTextBlock.DocumentText;
//
//             Log.Comment("Expecting the ColorPicker's selected color name to be '{0}'. Actual color name is '{1}'.",
//                 expectedName, actualName);
//
//             Verify.AreEqual(expectedName, actualName);
//         }
//
//         private void VerifyPreviousColorIsEqualTo(int expectedRed, int expectedGreen, int expectedBlue, int expectedAlpha = 255)
//         {
//             TextBlock previousRedTextBlock = new TextBlock(FindElement.ById("PreviousRedTextBlock"));
//             TextBlock previousGreenTextBlock = new TextBlock(FindElement.ById("PreviousGreenTextBlock"));
//             TextBlock previousBlueTextBlock = new TextBlock(FindElement.ById("PreviousBlueTextBlock"));
//             TextBlock previousAlphaTextBlock = new TextBlock(FindElement.ById("PreviousAlphaTextBlock"));
//
//             int actualRed = int.Parse(previousRedTextBlock.DocumentText);
//             int actualGreen = int.Parse(previousGreenTextBlock.DocumentText);
//             int actualBlue = int.Parse(previousBlueTextBlock.DocumentText);
//             int actualAlpha = int.Parse(previousAlphaTextBlock.DocumentText);
//
//             Log.Comment("Expecting the ColorPicker's previous color to be RGBA = ({0}, {1}, {2}, {3}). Actual color is RGBA = ({4}, {5}, {6}, {7}).",
//                 expectedRed, expectedGreen, expectedBlue, expectedAlpha,
//                 actualRed, actualGreen, actualBlue, actualAlpha);
//
//             Verify.AreEqual<int>(expectedRed, actualRed);
//             Verify.AreEqual<int>(expectedGreen, actualGreen);
//             Verify.AreEqual<int>(expectedBlue, actualBlue);
//             Verify.AreEqual<int>(expectedAlpha, actualAlpha);
//         }
//
//         private void VerifyPreviousColorIsNull()
//         {
//             TextBlock previousRedTextBlock = new TextBlock(FindElement.ById("PreviousRedTextBlock"));
//             TextBlock previousGreenTextBlock = new TextBlock(FindElement.ById("PreviousGreenTextBlock"));
//             TextBlock previousBlueTextBlock = new TextBlock(FindElement.ById("PreviousBlueTextBlock"));
//             TextBlock previousAlphaTextBlock = new TextBlock(FindElement.ById("PreviousAlphaTextBlock"));
//
//             Verify.AreEqual<string>("", previousRedTextBlock.DocumentText);
//             Verify.AreEqual<string>("", previousGreenTextBlock.DocumentText);
//             Verify.AreEqual<string>("", previousBlueTextBlock.DocumentText);
//             Verify.AreEqual<string>("", previousAlphaTextBlock.DocumentText);
//         }
//
//         private void VerifySelectionEllipsePosition(double expectedX, double expectedY)
//         {
//             VerifySelectionEllipsePositionCore(expectedX, expectedY, 0 /* allowedMargin */);
//         }
//
//         private void VerifySelectionEllipseIsNear(double expectedX, double expectedY)
//         {
//             VerifySelectionEllipsePositionCore(expectedX, expectedY, 10 /* allowedMargin */);
//         }
//
//         private void VerifySelectionEllipsePositionCore(double expectedX, double expectedY, double allowedMargin)
//         {
//             TextBlock ellipseXTextBlock = new TextBlock(FindElement.ById("EllipseXTextBlock"));
//             TextBlock ellipseYTextBlock = new TextBlock(FindElement.ById("EllipseYTextBlock"));
//
//             double actualX = double.Parse(ellipseXTextBlock.DocumentText);
//             double actualY = double.Parse(ellipseYTextBlock.DocumentText);
//
//             Log.Comment("Expecting the selection ellipse's position to be {4}({0}, {1}). Actual position is ({2}, {3}).",
//                 expectedX, expectedY,
//                 actualX, actualY,
//                 allowedMargin > 0 ? string.Format("within {0} units of ", allowedMargin) : "");
//
//             Verify.IsLessThanOrEqual(expectedX - allowedMargin, actualX);
//             Verify.IsGreaterThanOrEqual(expectedX + allowedMargin, actualX);
//             Verify.IsLessThanOrEqual(expectedY - allowedMargin, actualY);
//             Verify.IsGreaterThanOrEqual(expectedY + allowedMargin, actualY);
//         }
//
//         private void VerifySelectionEllipseColorIsEqualTo(int expectedRed, int expectedGreen, int expectedBlue, int expectedAlpha = 255)
//         {
//             TextBlock ellipseRedTextBlock = new TextBlock(FindElement.ById("EllipseRedTextBlock"));
//             TextBlock ellipseGreenTextBlock = new TextBlock(FindElement.ById("EllipseGreenTextBlock"));
//             TextBlock ellipseBlueTextBlock = new TextBlock(FindElement.ById("EllipseBlueTextBlock"));
//             TextBlock ellipseAlphaTextBlock = new TextBlock(FindElement.ById("EllipseAlphaTextBlock"));
//
//             int actualRed = int.Parse(ellipseRedTextBlock.DocumentText);
//             int actualGreen = int.Parse(ellipseGreenTextBlock.DocumentText);
//             int actualBlue = int.Parse(ellipseBlueTextBlock.DocumentText);
//             int actualAlpha = int.Parse(ellipseAlphaTextBlock.DocumentText);
//
//             Log.Comment("Expecting the selection ellipse's color to be RGBA = ({0}, {1}, {2}, {3}). Actual color is RGBA = ({4}, {5}, {6}, {7}).",
//                 expectedRed, expectedGreen, expectedBlue, expectedAlpha,
//                 actualRed, actualGreen, actualBlue, actualAlpha);
//
//             Verify.AreEqual<int>(expectedRed, actualRed);
//             Verify.AreEqual<int>(expectedGreen, actualGreen);
//             Verify.AreEqual<int>(expectedBlue, actualBlue);
//             Verify.AreEqual<int>(expectedAlpha, actualAlpha);
//         }
//
//         private void VerifyElementIsFocused(string expectedFocusedElement)
//         {
//             TextBlock currentlyFocusedElementTextBlock = new TextBlock(FindElement.ById("CurrentlyFocusedElementTextBlock"));
//
//             Log.Comment("Expecting the focused element to be '{0}'. Actual focused element is '{1}'.", expectedFocusedElement, currentlyFocusedElementTextBlock.DocumentText);
//             Verify.AreEqual(expectedFocusedElement, currentlyFocusedElementTextBlock.DocumentText);
//         }
//     }
// }
