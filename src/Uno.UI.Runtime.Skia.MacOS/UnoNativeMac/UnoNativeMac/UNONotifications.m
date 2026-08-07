//
//  UNONotifications.m
//

#import "UNONotifications.h"
#include <stdlib.h>
#include <string.h>

static NSString *const UNOImmediatePrefix = @"uno.appnotifications.";
static NSString *const UNOScheduledPrefix = @"uno.toastschedules.";
static NSString *const UNOActionsKey = @"uno.appnotifications.actions";
static NSString *const UNOLaunchArgumentKey = @"uno.appnotifications.launchArgument";
static NSString *const UNOProtocolUriKey = @"uno.appnotifications.protocolUri";
static NSString *const UNOMuteAudioKey = @"uno.appnotifications.muteAudio";
static NSString *const UNOSuppressDisplayKey = @"uno.appnotifications.suppressDisplay";
static uno_notification_activated_fn_ptr uno_notification_activated;
static uno_notification_delivered_fn_ptr uno_notification_delivered;
static UNAuthorizationStatus uno_authorization_status = UNAuthorizationStatusNotDetermined;
static NSMutableSet<NSString *> *uno_pending_identifiers;
static NSMutableSet<NSString *> *uno_delivered_identifiers;
static BOOL uno_identifiers_ready = NO;
static NSUInteger uno_identifier_generation = 0;
static NSUInteger uno_refresh_generation = 0;

static NSObject *UNOStateGate(void)
{
	static NSObject *gate;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		gate = [[NSObject alloc] init];
	});
	return gate;
}

static NSMutableDictionary<NSString *, UNNotificationCategory *> *UNORegisteredCategories(void)
{
	static NSMutableDictionary<NSString *, UNNotificationCategory *> *categories;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		categories = [NSMutableDictionary dictionary];
	});
	return categories;
}

static NSMutableDictionary<NSString *, NSString *> *UNORequestTokens(void)
{
	static NSMutableDictionary<NSString *, NSString *> *tokens;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		tokens = [NSMutableDictionary dictionary];
	});
	return tokens;
}

static NSString *UNOString(NSDictionary *dictionary, NSString *key)
{
	id value = dictionary[key];
	return [value isKindOfClass:[NSString class]] ? value : @"";
}

static BOOL UNOBool(NSDictionary *dictionary, NSString *key)
{
	id value = dictionary[key];
	return [value isKindOfClass:[NSNumber class]] && [value boolValue];
}

static BOOL UNOIsRequestIdentifier(NSString *identifier)
{
	return [identifier hasPrefix:UNOImmediatePrefix] || [identifier hasPrefix:UNOScheduledPrefix];
}

static void UNORefreshAuthorizationStatus(void)
{
	[[UNUserNotificationCenter currentNotificationCenter]
		getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings *settings) {
			@synchronized (UNOStateGate()) {
				uno_authorization_status = settings.authorizationStatus;
			}
		}];
}

static void UNORefreshIdentifiers(void)
{
	UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
	dispatch_group_t group = dispatch_group_create();
	__block NSArray<UNNotificationRequest *> *pending = @[];
	__block NSArray<UNNotification *> *delivered = @[];
	__block NSUInteger identifierGeneration;
	__block NSUInteger refreshGeneration;
	@synchronized (UNOStateGate()) {
		identifierGeneration = uno_identifier_generation;
		refreshGeneration = ++uno_refresh_generation;
	}

	dispatch_group_enter(group);
	[center getPendingNotificationRequestsWithCompletionHandler:^(NSArray<UNNotificationRequest *> *requests) {
		pending = requests ?: @[];
		dispatch_group_leave(group);
	}];
	dispatch_group_enter(group);
	[center getDeliveredNotificationsWithCompletionHandler:^(NSArray<UNNotification *> *notifications) {
		delivered = notifications ?: @[];
		dispatch_group_leave(group);
	}];
	dispatch_group_notify(group, dispatch_get_global_queue(QOS_CLASS_UTILITY, 0), ^{
		@synchronized (UNOStateGate()) {
			if (identifierGeneration != uno_identifier_generation || refreshGeneration != uno_refresh_generation) {
				return;
			}
			[uno_pending_identifiers removeAllObjects];
			for (UNNotificationRequest *request in pending) {
				if (UNOIsRequestIdentifier(request.identifier)) {
					[uno_pending_identifiers addObject:request.identifier];
				}
			}
			[uno_delivered_identifiers removeAllObjects];
			for (UNNotification *notification in delivered) {
				if (UNOIsRequestIdentifier(notification.request.identifier)) {
					[uno_delivered_identifiers addObject:notification.request.identifier];
				}
			}
			uno_identifiers_ready = YES;
		}
	});
}

@interface UNOUserNotificationCenterDelegate : NSObject <UNUserNotificationCenterDelegate>

@property(nonatomic, weak, nullable) id<UNUserNotificationCenterDelegate> previousDelegate;

@end

static UNOUserNotificationCenterDelegate *uno_notification_center_delegate;

@implementation UNOUserNotificationCenterDelegate

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
	willPresentNotification:(UNNotification *)notification
	withCompletionHandler:(void (^)(UNNotificationPresentationOptions options))completionHandler
{
	NSString *identifier = notification.request.identifier;
	if (!UNOIsRequestIdentifier(identifier)) {
		id<UNUserNotificationCenterDelegate> previous = self.previousDelegate;
		if ([previous respondsToSelector:_cmd]) {
			[previous userNotificationCenter:center
				willPresentNotification:notification
				withCompletionHandler:completionHandler];
		} else {
			completionHandler(UNNotificationPresentationOptionNone);
		}
		return;
	}

	if ([identifier hasPrefix:UNOScheduledPrefix] && uno_notification_delivered != NULL) {
		uno_notification_delivered(identifier.UTF8String);
	}
	@synchronized (UNOStateGate()) {
		[uno_pending_identifiers removeObject:identifier];
		[uno_delivered_identifiers addObject:identifier];
		uno_identifier_generation++;
	}

	NSDictionary *userInfo = notification.request.content.userInfo;
	if (UNOBool(userInfo, UNOSuppressDisplayKey)) {
		if (@available(macOS 11.0, *)) {
			completionHandler(UNNotificationPresentationOptionList);
		} else {
			completionHandler(UNNotificationPresentationOptionNone);
		}
		return;
	}

	UNNotificationPresentationOptions options;
	if (@available(macOS 11.0, *)) {
		options = UNNotificationPresentationOptionList | UNNotificationPresentationOptionBanner;
	} else {
		options = UNNotificationPresentationOptionAlert;
	}
	if (!UNOBool(userInfo, UNOMuteAudioKey)) {
		options |= UNNotificationPresentationOptionSound;
	}
	completionHandler(options);
}

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
	didReceiveNotificationResponse:(UNNotificationResponse *)response
	withCompletionHandler:(void (^)(void))completionHandler
{
	NSString *identifier = response.notification.request.identifier;
	if (!UNOIsRequestIdentifier(identifier)) {
		id<UNUserNotificationCenterDelegate> previous = self.previousDelegate;
		if ([previous respondsToSelector:_cmd]) {
			[previous userNotificationCenter:center
				didReceiveNotificationResponse:response
				withCompletionHandler:completionHandler];
		} else {
			completionHandler();
		}
		return;
	}

	if ([identifier hasPrefix:UNOScheduledPrefix] && uno_notification_delivered != NULL) {
		uno_notification_delivered(identifier.UTF8String);
	}
	@synchronized (UNOStateGate()) {
		[uno_pending_identifiers removeObject:identifier];
		[uno_delivered_identifiers addObject:identifier];
		uno_identifier_generation++;
	}

	if (![response.actionIdentifier isEqualToString:UNNotificationDismissActionIdentifier] &&
		uno_notification_activated != NULL) {
		NSDictionary *userInfo = response.notification.request.content.userInfo;
		NSString *argument = @"";
		NSString *protocolUri = @"";
		NSString *inputId = @"";
		NSString *userText = @"";

		if ([response.actionIdentifier isEqualToString:UNNotificationDefaultActionIdentifier]) {
			argument = UNOString(userInfo, UNOLaunchArgumentKey);
			protocolUri = UNOString(userInfo, UNOProtocolUriKey);
		} else {
			NSDictionary *actions = userInfo[UNOActionsKey];
			NSDictionary *action = [actions isKindOfClass:[NSDictionary class]]
				? actions[response.actionIdentifier]
				: nil;
			if ([action isKindOfClass:[NSDictionary class]]) {
				argument = UNOString(action, @"argument");
				protocolUri = UNOString(action, @"protocolUri");
				inputId = UNOString(action, @"inputId");
			}
			if ([response isKindOfClass:[UNTextInputNotificationResponse class]]) {
				userText = ((UNTextInputNotificationResponse *)response).userText ?: @"";
			}
		}

		uno_notification_activated(
			identifier.UTF8String,
			argument.UTF8String,
			protocolUri.UTF8String,
			inputId.UTF8String,
			userText.UTF8String);
	}

	completionHandler();
}

- (void)userNotificationCenter:(UNUserNotificationCenter *)center
	openSettingsForNotification:(UNNotification * _Nullable)notification API_AVAILABLE(macos(10.14))
{
	id<UNUserNotificationCenterDelegate> previous = self.previousDelegate;
	if ([previous respondsToSelector:_cmd]) {
		[previous userNotificationCenter:center openSettingsForNotification:notification];
	}
}

@end

static UNNotificationAction *UNOCreateAction(NSDictionary *action)
{
	NSString *identifier = UNOString(action, @"identifier");
	NSString *title = UNOString(action, @"title");
	UNNotificationActionOptions options = UNNotificationActionOptionNone;
	if (UNOBool(action, @"destructive")) {
		options |= UNNotificationActionOptionDestructive;
	}
	if (UNOBool(action, @"foreground") || UNOString(action, @"protocolUri").length > 0) {
		options |= UNNotificationActionOptionForeground;
	}

	NSString *inputId = UNOString(action, @"inputId");
	if (inputId.length > 0) {
		return [UNTextInputNotificationAction
			actionWithIdentifier:identifier
			title:title
			options:options
			textInputButtonTitle:UNOString(action, @"inputButtonTitle")
			textInputPlaceholder:UNOString(action, @"inputPlaceholder")];
	}
	return [UNNotificationAction actionWithIdentifier:identifier title:title options:options];
}

static UNMutableNotificationContent *UNOCreateContent(NSDictionary *command);
static void UNOAddRequest(NSDictionary *command, double delaySeconds, NSString *requestToken);

static BOOL UNORegisterCategoryAndPost(NSDictionary *command, double delaySeconds, NSString *requestToken)
{
	NSString *categoryIdentifier = UNOString(command, @"categoryIdentifier");
	NSArray *actionCommands = command[@"actions"];
	if (categoryIdentifier.length == 0 || ![actionCommands isKindOfClass:[NSArray class]] || actionCommands.count == 0) {
		UNOAddRequest(command, delaySeconds, requestToken);
		return YES;
	}

	NSMutableArray<UNNotificationAction *> *actions = [NSMutableArray arrayWithCapacity:actionCommands.count];
	for (id value in actionCommands) {
		if (![value isKindOfClass:[NSDictionary class]]) {
			return NO;
		}
		[actions addObject:UNOCreateAction(value)];
	}
	UNNotificationCategory *category = [UNNotificationCategory
		categoryWithIdentifier:categoryIdentifier
		actions:actions
		intentIdentifiers:@[]
		options:UNNotificationCategoryOptionNone];
	NSMutableDictionary<NSString *, UNNotificationCategory *> *registeredCategories = UNORegisteredCategories();
	@synchronized (registeredCategories) {
		registeredCategories[categoryIdentifier] = category;
	}

	[[UNUserNotificationCenter currentNotificationCenter]
		getNotificationCategoriesWithCompletionHandler:^(NSSet<UNNotificationCategory *> *categories) {
			NSDictionary<NSString *, UNNotificationCategory *> *registeredSnapshot;
			@synchronized (registeredCategories) {
				registeredSnapshot = [registeredCategories copy];
			}
			NSMutableSet<UNNotificationCategory *> *merged = categories == nil
				? [NSMutableSet set]
				: [categories mutableCopy];
			for (UNNotificationCategory *existing in [merged copy]) {
				if (registeredSnapshot[existing.identifier] != nil) {
					[merged removeObject:existing];
				}
			}
			[merged addObjectsFromArray:registeredSnapshot.allValues];
			[[UNUserNotificationCenter currentNotificationCenter] setNotificationCategories:merged];
			UNOAddRequest(command, delaySeconds, requestToken);
		}];
	return YES;
}

static UNMutableNotificationContent *UNOCreateContent(NSDictionary *command)
{
	UNMutableNotificationContent *content = [[UNMutableNotificationContent alloc] init];
	content.title = UNOString(command, @"title");
	content.subtitle = UNOString(command, @"subtitle");
	content.body = UNOString(command, @"body");
	content.threadIdentifier = UNOString(command, @"threadIdentifier");
	content.categoryIdentifier = UNOString(command, @"categoryIdentifier");

	NSMutableDictionary *userInfo = [NSMutableDictionary dictionary];
	userInfo[UNOLaunchArgumentKey] = UNOString(command, @"launchArgument");
	userInfo[UNOProtocolUriKey] = UNOString(command, @"protocolUri");
	userInfo[UNOMuteAudioKey] = @([command[@"muteAudio"] boolValue]);
	userInfo[UNOSuppressDisplayKey] = @([command[@"suppressDisplay"] boolValue]);
	NSMutableDictionary *actionInfo = [NSMutableDictionary dictionary];
	for (NSDictionary *action in command[@"actions"]) {
		NSString *identifier = UNOString(action, @"identifier");
		if (identifier.length > 0) {
			actionInfo[identifier] = @{
				@"argument": UNOString(action, @"argument"),
				@"protocolUri": UNOString(action, @"protocolUri"),
				@"inputId": UNOString(action, @"inputId"),
			};
		}
	}
	userInfo[UNOActionsKey] = actionInfo;
	content.userInfo = userInfo;

	if (!UNOBool(command, @"muteAudio") && !UNOBool(command, @"suppressDisplay")) {
		content.sound = [UNNotificationSound defaultSound];
	}
	if (@available(macOS 12.0, *)) {
		content.interruptionLevel = UNOBool(command, @"suppressDisplay")
			? UNNotificationInterruptionLevelPassive
			: UNOBool(command, @"highPriority")
				? UNNotificationInterruptionLevelTimeSensitive
				: UNNotificationInterruptionLevelActive;
	}

	NSString *attachmentPath = UNOString(command, @"attachmentSource");
	if (attachmentPath.length > 0) {
		NSError *attachmentError = nil;
		UNNotificationAttachment *attachment = [UNNotificationAttachment
			attachmentWithIdentifier:@"uno.appnotifications.attachment"
			URL:[NSURL fileURLWithPath:attachmentPath]
			options:nil
			error:&attachmentError];
		if (attachment != nil) {
			content.attachments = @[attachment];
		} else {
			NSLog(@"Unable to create app notification attachment: %@", attachmentError);
		}
	}
	return content;
}

static void UNOAddRequest(NSDictionary *command, double delaySeconds, NSString *requestToken)
{
	NSString *identifier = UNOString(command, @"requestIdentifier");
	UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
	UNNotificationTrigger *trigger = delaySeconds > 0
		? [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:MAX(1.0, delaySeconds) repeats:NO]
		: nil;
	UNNotificationRequest *request = [UNNotificationRequest
		requestWithIdentifier:identifier
		content:UNOCreateContent(command)
		trigger:trigger];

	NSMutableDictionary<NSString *, NSString *> *requestTokens = UNORequestTokens();
	@synchronized (requestTokens) {
		if (![requestTokens[identifier] isEqualToString:requestToken]) {
			return;
		}
		[center removePendingNotificationRequestsWithIdentifiers:@[identifier]];
		[center removeDeliveredNotificationsWithIdentifiers:@[identifier]];
		@synchronized (UNOStateGate()) {
			[uno_delivered_identifiers removeObject:identifier];
			[uno_pending_identifiers addObject:identifier];
			uno_identifier_generation++;
		}
		[center addNotificationRequest:request withCompletionHandler:^(NSError *error) {
			BOOL isCurrentRequest;
			@synchronized (requestTokens) {
				isCurrentRequest = [requestTokens[identifier] isEqualToString:requestToken];
				if (isCurrentRequest) {
					[requestTokens removeObjectForKey:identifier];
				}
			}
			if (error != nil) {
				if (isCurrentRequest) {
					@synchronized (UNOStateGate()) {
						[uno_pending_identifiers removeObject:identifier];
						uno_identifier_generation++;
					}
				}
				NSLog(@"Unable to add app notification request: %@", error);
			}
		}];
	}
}

void uno_notifications_set_callbacks(
	uno_notification_activated_fn_ptr activated,
	uno_notification_delivered_fn_ptr delivered)
{
	uno_notification_activated = activated;
	uno_notification_delivered = delivered;
}

bool uno_notifications_is_supported(void)
{
	return NSClassFromString(@"UNUserNotificationCenter") != nil &&
		[NSBundle mainBundle].bundleIdentifier.length > 0;
}

int32_t uno_notifications_get_setting(void)
{
	if (!uno_notifications_is_supported()) {
		return 5;
	}
	@synchronized (UNOStateGate()) {
		return (int32_t)uno_authorization_status;
	}
}

void uno_notifications_initialize(void)
{
	if (!uno_notifications_is_supported()) {
		return;
	}
	@synchronized (UNOStateGate()) {
		if (uno_pending_identifiers == nil) {
			uno_pending_identifiers = [NSMutableSet set];
			uno_delivered_identifiers = [NSMutableSet set];
		}
	}
	UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
	if (uno_notification_center_delegate == nil || center.delegate != uno_notification_center_delegate) {
		UNOUserNotificationCenterDelegate *delegate = [[UNOUserNotificationCenterDelegate alloc] init];
		if (center.delegate != delegate) {
			delegate.previousDelegate = center.delegate;
		}
		uno_notification_center_delegate = delegate;
		center.delegate = delegate;
	}
	UNORefreshAuthorizationStatus();
	UNORefreshIdentifiers();
}

void uno_notifications_request_authorization(void)
{
	uno_notifications_initialize();
	if (!uno_notifications_is_supported()) {
		return;
	}
	[[UNUserNotificationCenter currentNotificationCenter]
		requestAuthorizationWithOptions:(UNAuthorizationOptionAlert | UNAuthorizationOptionSound | UNAuthorizationOptionBadge)
		completionHandler:^(BOOL granted, NSError *error) {
			if (error != nil) {
				NSLog(@"Unable to request app notification authorization: %@", error);
			}
			UNORefreshAuthorizationStatus();
		}];
}

bool uno_notifications_post(const char *command_json, double delay_seconds)
{
	if (!uno_notifications_is_supported() || command_json == NULL) {
		return false;
	}
	NSData *data = [[NSData alloc] initWithBytes:command_json length:strlen(command_json)];
	NSError *jsonError = nil;
	id parsed = [NSJSONSerialization JSONObjectWithData:data options:0 error:&jsonError];
	if (![parsed isKindOfClass:[NSDictionary class]]) {
		NSLog(@"Unable to parse app notification command: %@", jsonError);
		return false;
	}
	NSDictionary *command = parsed;
	NSString *identifier = UNOString(command, @"requestIdentifier");
	if (!UNOIsRequestIdentifier(identifier)) {
		return false;
	}
	NSString *requestToken = NSUUID.UUID.UUIDString;
	NSMutableDictionary<NSString *, NSString *> *requestTokens = UNORequestTokens();
	@synchronized (requestTokens) {
		requestTokens[identifier] = requestToken;
	}
	return UNORegisterCategoryAndPost(command, delay_seconds, requestToken);
}

bool uno_notifications_remove(const char *request_identifier)
{
	if (!uno_notifications_is_supported() || request_identifier == NULL) {
		return false;
	}
	NSString *identifier = [NSString stringWithUTF8String:request_identifier];
	if (!UNOIsRequestIdentifier(identifier)) {
		return false;
	}
	NSMutableDictionary<NSString *, NSString *> *requestTokens = UNORequestTokens();
	@synchronized (requestTokens) {
		[requestTokens removeObjectForKey:identifier];
	}
	UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
	[center removePendingNotificationRequestsWithIdentifiers:@[identifier]];
	[center removeDeliveredNotificationsWithIdentifiers:@[identifier]];
	@synchronized (UNOStateGate()) {
		[uno_pending_identifiers removeObject:identifier];
		[uno_delivered_identifiers removeObject:identifier];
		uno_identifier_generation++;
	}
	return true;
}

static NSArray<NSString *> * _Nullable UNOGetIdentifiers(
	NSString *prefix,
	BOOL includePending,
	BOOL includeDelivered)
{
	BOOL identifiersReady;
	@synchronized (UNOStateGate()) {
		identifiersReady = uno_identifiers_ready;
	}
	if (!identifiersReady) {
		UNORefreshIdentifiers();
		return nil;
	}
	NSMutableSet<NSString *> *identifiers = [NSMutableSet set];
	@synchronized (UNOStateGate()) {
		if (includePending) {
			for (NSString *identifier in uno_pending_identifiers) {
				if ([identifier hasPrefix:prefix]) {
					[identifiers addObject:identifier];
				}
			}
		}
		if (includeDelivered) {
			for (NSString *identifier in uno_delivered_identifiers) {
				if ([identifier hasPrefix:prefix]) {
					[identifiers addObject:identifier];
				}
			}
		}
	}
	UNORefreshIdentifiers();
	return identifiers.allObjects;
}

bool uno_notifications_remove_all(const char *request_identifier_prefix)
{
	if (!uno_notifications_is_supported() || request_identifier_prefix == NULL) {
		return false;
	}
	NSString *prefix = [NSString stringWithUTF8String:request_identifier_prefix];
	if (prefix.length == 0) {
		return false;
	}
	NSMutableDictionary<NSString *, NSString *> *requestTokens = UNORequestTokens();
	@synchronized (requestTokens) {
		for (NSString *identifier in [requestTokens.allKeys copy]) {
			if ([identifier hasPrefix:prefix]) {
				[requestTokens removeObjectForKey:identifier];
			}
		}
	}
	NSMutableSet<NSString *> *cachedIdentifiers = [NSMutableSet set];
	@synchronized (UNOStateGate()) {
		for (NSString *identifier in uno_pending_identifiers) {
			if ([identifier hasPrefix:prefix]) {
				[cachedIdentifiers addObject:identifier];
			}
		}
		for (NSString *identifier in uno_delivered_identifiers) {
			if ([identifier hasPrefix:prefix]) {
				[cachedIdentifiers addObject:identifier];
			}
		}
	}
	UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
	NSArray<NSString *> *cachedSnapshot = cachedIdentifiers.allObjects;
	[center removePendingNotificationRequestsWithIdentifiers:cachedSnapshot];
	[center removeDeliveredNotificationsWithIdentifiers:cachedSnapshot];
	@synchronized (UNOStateGate()) {
		[uno_pending_identifiers minusSet:cachedIdentifiers];
		[uno_delivered_identifiers minusSet:cachedIdentifiers];
		uno_identifier_generation++;
	}
	[center getPendingNotificationRequestsWithCompletionHandler:^(NSArray<UNNotificationRequest *> *requests) {
		NSMutableArray<NSString *> *identifiers = [NSMutableArray array];
		for (UNNotificationRequest *request in requests) {
			if ([request.identifier hasPrefix:prefix]) {
				[identifiers addObject:request.identifier];
			}
		}
		[center removePendingNotificationRequestsWithIdentifiers:identifiers];
		@synchronized (UNOStateGate()) {
			[uno_pending_identifiers minusSet:[NSSet setWithArray:identifiers]];
			uno_identifier_generation++;
		}
	}];
	[center getDeliveredNotificationsWithCompletionHandler:^(NSArray<UNNotification *> *notifications) {
		NSMutableArray<NSString *> *identifiers = [NSMutableArray array];
		for (UNNotification *notification in notifications) {
			if ([notification.request.identifier hasPrefix:prefix]) {
				[identifiers addObject:notification.request.identifier];
			}
		}
		[center removeDeliveredNotificationsWithIdentifiers:identifiers];
		@synchronized (UNOStateGate()) {
			[uno_delivered_identifiers minusSet:[NSSet setWithArray:identifiers]];
			uno_identifier_generation++;
		}
	}];
	return true;
}

char * _Nullable uno_notifications_get_identifiers_json(
	const char *request_identifier_prefix,
	bool include_pending,
	bool include_delivered)
{
	if (!uno_notifications_is_supported() || request_identifier_prefix == NULL) {
		return NULL;
	}
	NSString *prefix = [NSString stringWithUTF8String:request_identifier_prefix];
	NSArray<NSString *> *identifiers = UNOGetIdentifiers(prefix, include_pending, include_delivered);
	if (identifiers == nil) {
		return NULL;
	}
	NSError *error = nil;
	NSData *json = [NSJSONSerialization dataWithJSONObject:identifiers options:0 error:&error];
	if (json == nil) {
		NSLog(@"Unable to serialize app notification identifiers: %@", error);
		return NULL;
	}
	NSString *value = [[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding];
	return value == nil ? NULL : strdup(value.UTF8String);
}

void uno_notifications_free_string(char * _Nullable value)
{
	free(value);
}