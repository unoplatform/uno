#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.Foundation.Metadata;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationApiContract
{
	[TestMethod]
	public void When_Public_Types_Are_Compared_To_Windows_App_Sdk_2_3_They_Match()
	{
		var expected = new[]
		{
			"Microsoft.Windows.AppNotifications.AppNotification",
			"Microsoft.Windows.AppNotifications.AppNotificationActivatedEventArgs",
			"Microsoft.Windows.AppNotifications.AppNotificationManager",
			"Microsoft.Windows.AppNotifications.AppNotificationPriority",
			"Microsoft.Windows.AppNotifications.AppNotificationProgressData",
			"Microsoft.Windows.AppNotifications.AppNotificationProgressResult",
			"Microsoft.Windows.AppNotifications.AppNotificationsContract",
			"Microsoft.Windows.AppNotifications.AppNotificationSetting",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationAudioLooping",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilderContract",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationButton",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationButtonStyle",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationComboBox",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationDuration",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationImageCrop",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationScenario",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationSoundEvent",
			"Microsoft.Windows.AppNotifications.Builder.AppNotificationTextProperties",
		};
		var actual = typeof(AppNotification).Assembly
			.GetExportedTypes()
			.Where(type => type.Namespace is not null && type.Namespace.StartsWith("Microsoft.Windows.AppNotifications", StringComparison.Ordinal))
			.Select(type => type.FullName!)
			.ToArray();

		AssertSetsAreEqual(expected, actual);
	}

	[TestMethod]
	public void When_Core_Surface_Is_Compared_To_Windows_App_Sdk_2_3_It_Matches()
	{
		AssertSurface<AppNotification>("""
			C:(System.String payload)
			P:System.DateTimeOffset Expiration {get;set;}
			P:System.Boolean ExpiresOnReboot {get;set;}
			P:System.String Group {get;set;}
			P:System.UInt32 Id {get;}
			P:System.String Payload {get;}
			P:Microsoft.Windows.AppNotifications.AppNotificationPriority Priority {get;set;}
			P:Microsoft.Windows.AppNotifications.AppNotificationProgressData Progress {get;set;}
			P:System.Boolean SuppressDisplay {get;set;}
			P:System.String Tag {get;set;}
			""");
		AssertSurface<AppNotificationActivatedEventArgs>("""
			P:System.String Argument {get;}
			P:System.Collections.Generic.IDictionary<System.String,System.String> Arguments {get;}
			P:System.Collections.Generic.IDictionary<System.String,System.String> UserInput {get;}
			""");
		AssertSurface<AppNotificationManager>("""
			P:static Microsoft.Windows.AppNotifications.AppNotificationManager Default {get;}
			P:Microsoft.Windows.AppNotifications.AppNotificationSetting Setting {get;}
			E:Windows.Foundation.TypedEventHandler<Microsoft.Windows.AppNotifications.AppNotificationManager,Microsoft.Windows.AppNotifications.AppNotificationActivatedEventArgs> NotificationInvoked
			M:Windows.Foundation.IAsyncOperation<System.Collections.Generic.IList<Microsoft.Windows.AppNotifications.AppNotification>> GetAllAsync()
			M:static System.Boolean IsSupported()
			M:System.Void Register()
			M:System.Void Register(System.String displayName,System.Uri iconUri)
			M:Windows.Foundation.IAsyncAction RemoveAllAsync()
			M:Windows.Foundation.IAsyncAction RemoveByGroupAsync(System.String group)
			M:Windows.Foundation.IAsyncAction RemoveByIdAsync(System.UInt32 notificationId)
			M:Windows.Foundation.IAsyncAction RemoveByTagAndGroupAsync(System.String tag,System.String group)
			M:Windows.Foundation.IAsyncAction RemoveByTagAsync(System.String tag)
			M:System.Void Show(Microsoft.Windows.AppNotifications.AppNotification notification)
			M:System.Void Unregister()
			M:System.Void UnregisterAll()
			M:Windows.Foundation.IAsyncOperation<Microsoft.Windows.AppNotifications.AppNotificationProgressResult> UpdateAsync(Microsoft.Windows.AppNotifications.AppNotificationProgressData data,System.String tag,System.String group)
			M:Windows.Foundation.IAsyncOperation<Microsoft.Windows.AppNotifications.AppNotificationProgressResult> UpdateAsync(Microsoft.Windows.AppNotifications.AppNotificationProgressData data,System.String tag)
			""");
		AssertSurface<AppNotificationProgressData>("""
			C:(System.UInt32 sequenceNumber)
			P:System.UInt32 SequenceNumber {get;set;}
			P:System.String Status {get;set;}
			P:System.String Title {get;set;}
			P:System.Double Value {get;set;}
			P:System.String ValueStringOverride {get;set;}
			""");
	}

	[TestMethod]
	public void When_Builder_Surface_Is_Compared_To_Windows_App_Sdk_2_3_It_Matches()
	{
		AssertSurface<AppNotificationBuilder>("""
			C:()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder AddArgument(System.String key,System.String value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder AddButton(Microsoft.Windows.AppNotifications.Builder.AppNotificationButton value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder AddComboBox(Microsoft.Windows.AppNotifications.Builder.AppNotificationComboBox value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder AddProgressBar(Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder AddText(System.String text)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder AddText(System.String text,Microsoft.Windows.AppNotifications.Builder.AppNotificationTextProperties properties)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder AddTextBox(System.String id)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder AddTextBox(System.String id,System.String placeHolderText,System.String title)
			M:Microsoft.Windows.AppNotifications.AppNotification BuildNotification()
			M:static System.Boolean IsUrgentScenarioSupported()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder MuteAudio()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAppLogoOverride(System.Uri imageUri)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAppLogoOverride(System.Uri imageUri,Microsoft.Windows.AppNotifications.Builder.AppNotificationImageCrop imageCrop)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAppLogoOverride(System.Uri imageUri,Microsoft.Windows.AppNotifications.Builder.AppNotificationImageCrop imageCrop,System.String alternateText)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAttributionText(System.String text)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAttributionText(System.String text,System.String language)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAudioEvent(Microsoft.Windows.AppNotifications.Builder.AppNotificationSoundEvent appNotificationSoundEvent)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAudioEvent(Microsoft.Windows.AppNotifications.Builder.AppNotificationSoundEvent appNotificationSoundEvent,Microsoft.Windows.AppNotifications.Builder.AppNotificationAudioLooping loop)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAudioUri(System.Uri audioUri)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetAudioUri(System.Uri audioUri,Microsoft.Windows.AppNotifications.Builder.AppNotificationAudioLooping loop)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetDuration(Microsoft.Windows.AppNotifications.Builder.AppNotificationDuration duration)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetGroup(System.String group)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetHeroImage(System.Uri imageUri)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetHeroImage(System.Uri imageUri,System.String alternateText)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetInlineImage(System.Uri imageUri)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetInlineImage(System.Uri imageUri,Microsoft.Windows.AppNotifications.Builder.AppNotificationImageCrop imageCrop)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetInlineImage(System.Uri imageUri,Microsoft.Windows.AppNotifications.Builder.AppNotificationImageCrop imagecrop,System.String alternateText)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetScenario(Microsoft.Windows.AppNotifications.Builder.AppNotificationScenario value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetTag(System.String value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder SetTimeStamp(System.DateTimeOffset value)
			""");
		AssertSurface<AppNotificationButton>("""
			C:()
			C:(System.String content)
			P:System.Collections.Generic.IDictionary<System.String,System.String> Arguments {get;set;}
			P:Microsoft.Windows.AppNotifications.Builder.AppNotificationButtonStyle ButtonStyle {get;set;}
			P:System.String Content {get;set;}
			P:System.Boolean ContextMenuPlacement {get;set;}
			P:System.Uri Icon {get;set;}
			P:System.String InputId {get;set;}
			P:System.Uri InvokeUri {get;set;}
			P:System.String TargetAppId {get;set;}
			P:System.String ToolTip {get;set;}
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationButton AddArgument(System.String key,System.String value)
			M:static System.Boolean IsButtonStyleSupported()
			M:static System.Boolean IsToolTipSupported()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationButton SetButtonStyle(Microsoft.Windows.AppNotifications.Builder.AppNotificationButtonStyle value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationButton SetContextMenuPlacement()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationButton SetIcon(System.Uri value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationButton SetInputId(System.String value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationButton SetInvokeUri(System.Uri protocolUri)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationButton SetInvokeUri(System.Uri protocolUri,System.String targetAppId)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationButton SetToolTip(System.String value)
			""");
		AssertSurface<AppNotificationComboBox>("""
			C:(System.String id)
			P:System.Collections.Generic.IDictionary<System.String,System.String> Items {get;set;}
			P:System.String SelectedItem {get;set;}
			P:System.String Title {get;set;}
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationComboBox AddItem(System.String id,System.String content)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationComboBox SetSelectedItem(System.String id)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationComboBox SetTitle(System.String value)
			""");
		AssertSurface<AppNotificationProgressBar>("""
			C:()
			P:System.String Status {get;set;}
			P:System.String Title {get;set;}
			P:System.Double Value {get;set;}
			P:System.String ValueStringOverride {get;set;}
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar BindStatus()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar BindTitle()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar BindValue()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar BindValueStringOverride()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar SetStatus(System.String value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar SetTitle(System.String value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar SetValue(System.Double value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationProgressBar SetValueStringOverride(System.String value)
			""");
		AssertSurface<AppNotificationTextProperties>("""
			C:()
			P:System.Boolean IncomingCallAlignment {get;set;}
			P:System.String Language {get;set;}
			P:System.Int32 MaxLines {get;set;}
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationTextProperties SetIncomingCallAlignment()
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationTextProperties SetLanguage(System.String value)
			M:Microsoft.Windows.AppNotifications.Builder.AppNotificationTextProperties SetMaxLines(System.Int32 value)
			""");
	}

	[TestMethod]
	public void When_Enum_Values_Are_Compared_To_Windows_App_Sdk_2_3_They_Match()
	{
		AssertEnum<AppNotificationPriority>("Default=0", "High=1");
		AssertEnum<AppNotificationProgressResult>("Succeeded=0", "AppNotificationNotFound=1", "Unsupported=2");
		AssertEnum<AppNotificationSetting>("Enabled=0", "DisabledForApplication=1", "DisabledForUser=2", "DisabledByGroupPolicy=3", "DisabledByManifest=4", "Unsupported=5");
		AssertEnum<AppNotificationsContract>();
		AssertEnum<AppNotificationAudioLooping>("None=0", "Loop=1");
		AssertEnum<AppNotificationButtonStyle>("Default=0", "Success=1", "Critical=2");
		AssertEnum<AppNotificationDuration>("Default=0", "Long=1");
		AssertEnum<AppNotificationImageCrop>("Default=0", "Circle=1");
		AssertEnum<AppNotificationScenario>("Default=0", "Reminder=1", "Alarm=2", "IncomingCall=3", "Urgent=4");
		AssertEnum<AppNotificationSoundEvent>("Default=0", "IM=1", "Mail=2", "Reminder=3", "SMS=4", "Alarm=5", "Alarm2=6", "Alarm3=7", "Alarm4=8", "Alarm5=9", "Alarm6=10", "Alarm7=11", "Alarm8=12", "Alarm9=13", "Alarm10=14", "Call=15", "Call2=16", "Call3=17", "Call4=18", "Call5=19", "Call6=20", "Call7=21", "Call8=22", "Call9=23", "Call10=24");
		AssertEnum<AppNotificationBuilderContract>();
	}

	[TestMethod]
	public void When_AppNotifications_Contract_Is_Queried_Stable_Implemented_Versions_Are_Reported()
	{
		const string contractName = "Microsoft.Windows.AppNotifications.AppNotificationsContract";

		Assert.IsTrue(ApiInformation.IsApiContractPresent(contractName, 1));
		Assert.IsTrue(ApiInformation.IsApiContractPresent(contractName, 3));
		Assert.IsFalse(ApiInformation.IsApiContractPresent(contractName, 4));
	}

	[TestMethod]
	public void When_Core_Metadata_Is_Inspected_Stable_Contract_Annotations_Are_Present()
	{
		AssertContractVersion(typeof(AppNotificationsContract), 3);
		AssertContractVersion(typeof(AppNotificationManager), 1);
		AssertContractVersion(typeof(AppNotification), 1);
		AssertContractVersion(typeof(AppNotificationProgressData), 1);
		AssertContractVersion(typeof(AppNotificationActivatedEventArgs).GetProperty(nameof(AppNotificationActivatedEventArgs.Arguments))!, 3);

		var overloads = typeof(AppNotificationManager)
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Where(method => method.Name == nameof(AppNotificationManager.UpdateAsync))
			.Select(method => method.CustomAttributes.Single(attribute => attribute.AttributeType == typeof(OverloadAttribute)))
			.Select(attribute => (string)attribute.ConstructorArguments.Single().Value!)
			.ToArray();
		CollectionAssert.AreEquivalent(new[] { "UpdateAsync", "UpdateAsync2" }, overloads);
	}

	private static void AssertSurface<T>(string expectedSurface)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
		var type = typeof(T);
		var actual = new List<string>();
		actual.AddRange(type.GetConstructors(flags).Select(constructor => $"C:({FormatParameters(constructor.GetParameters())})"));
		actual.AddRange(type.GetProperties(flags).Select(property =>
		{
			var accessor = property.GetMethod ?? property.SetMethod!;
			var staticPrefix = accessor.IsStatic ? "static " : string.Empty;
			var setter = property.CanWrite ? "set;" : string.Empty;
			return $"P:{staticPrefix}{FormatType(property.PropertyType)} {property.Name} {{get;{setter}}}";
		}));
		actual.AddRange(type.GetEvents(flags).Select(@event => $"E:{FormatType(@event.EventHandlerType!)} {@event.Name}"));
		actual.AddRange(type.GetMethods(flags)
			.Where(method => !method.IsSpecialName)
			.Select(method => $"M:{(method.IsStatic ? "static " : string.Empty)}{FormatType(method.ReturnType)} {method.Name}({FormatParameters(method.GetParameters())})"));
		var expected = expectedSurface.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		AssertSetsAreEqual(expected, actual);
	}

	private static void AssertEnum<T>(params string[] expected)
		where T : struct, Enum
	{
		var actual = typeof(T)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Select(field => $"{field.Name}={Convert.ToInt32(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture)}")
			.ToArray();

		CollectionAssert.AreEqual(expected, actual);
	}

	private static void AssertContractVersion(MemberInfo member, uint expectedMajorVersion)
	{
		var attribute = member.CustomAttributes.Single(attribute => attribute.AttributeType == typeof(ContractVersionAttribute));
		var encodedVersion = (uint)attribute.ConstructorArguments[^1].Value!;
		Assert.AreEqual(expectedMajorVersion, encodedVersion >> 16);
	}

	private static string FormatParameters(IEnumerable<ParameterInfo> parameters)
		=> string.Join(',', parameters.Select(parameter => $"{FormatType(parameter.ParameterType)} {parameter.Name}"));

	private static string FormatType(Type type)
	{
		if (!type.IsGenericType)
		{
			return type.FullName!;
		}

		var genericName = type.GetGenericTypeDefinition().FullName!;
		genericName = genericName[..genericName.IndexOf('`')];
		return $"{genericName}<{string.Join(',', type.GetGenericArguments().Select(FormatType))}>";
	}

	private static void AssertSetsAreEqual(IEnumerable<string> expected, IEnumerable<string> actual)
	{
		var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
		var actualSet = actual.ToHashSet(StringComparer.Ordinal);
		var missing = expectedSet.Except(actualSet).OrderBy(value => value, StringComparer.Ordinal);
		var extra = actualSet.Except(expectedSet).OrderBy(value => value, StringComparer.Ordinal);
		Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, missing), "Missing public API members");
		Assert.AreEqual(string.Empty, string.Join(Environment.NewLine, extra), "Unexpected public API members");
		Assert.AreEqual(expectedSet.Count, actualSet.Count);
	}
}
