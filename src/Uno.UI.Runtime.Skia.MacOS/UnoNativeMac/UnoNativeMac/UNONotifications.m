//
//  UNONotifications.m
//

#import "UNONotifications.h"
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

static NSString *const UNOImmediatePrefix = @"uno.appnotifications.";
static NSString *const UNOScheduledPrefix = @"uno.toastschedules.";
static NSString *const UNOActionsKey = @"uno.appnotifications.actions";
static NSString *const UNOLaunchArgumentKey = @"uno.appnotifications.launchArgument";
static NSString *const UNOProtocolUriKey = @"uno.appnotifications.protocolUri";
static NSString *const UNOMuteAudioKey = @"uno.appnotifications.muteAudio";
static NSString *const UNOSuppressDisplayKey = @"uno.appnotifications.suppressDisplay";
static const int64_t UNONotificationOperationTimeoutSeconds = 10;
static uno_notification_activated_fn_ptr uno_notification_activated;
static uno_notification_delivered_fn_ptr uno_notification_delivered;
static UNAuthorizationStatus uno_authorization_status = UNAuthorizationStatusNotDetermined;
static BOOL uno_authorization_status_ready = NO;
static NSUInteger uno_authorization_refresh_generation = 0;
static NSUInteger uno_authorization_completed_generation = 0;
static NSMutableSet<NSString *> *uno_pending_identifiers;
static NSMutableSet<NSString *> *uno_delivered_identifiers;
static BOOL uno_identifiers_ready = NO;
static NSUInteger uno_identifier_generation = 0;
static NSUInteger uno_refresh_generation = 0;

@interface UNONotificationPostOperation : NSObject
{
@public
	dispatch_semaphore_t completion;
}

@property(nonatomic, copy) NSString *identifier;
@property(nonatomic) BOOL completed;
@property(nonatomic) BOOL canceled;
@property(nonatomic) BOOL succeeded;

- (instancetype)initWithIdentifier:(NSString *)identifier;

@end

@implementation UNONotificationPostOperation

- (instancetype)initWithIdentifier:(NSString *)identifier
{
	self = [super init];
	if (self != nil) {
		completion = dispatch_semaphore_create(0);
		_identifier = [identifier copy];
	}
	return self;
}

@end

static NSObject *UNOStateGate(void)
{
	static NSObject *gate;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		gate = [[NSObject alloc] init];
	});
	return gate;
}

static NSCondition *UNOAuthorizationCondition(void)
{
	static NSCondition *condition;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		condition = [[NSCondition alloc] init];
	});
	return condition;
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

static NSMutableDictionary<NSString *, UNONotificationPostOperation *> *UNORequestOperations(void)
{
	static NSMutableDictionary<NSString *, UNONotificationPostOperation *> *operations;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		operations = [NSMutableDictionary dictionary];
	});
	return operations;
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
	NSCondition *condition = UNOAuthorizationCondition();
	[condition lock];
	NSUInteger refreshGeneration = ++uno_authorization_refresh_generation;
	[condition broadcast];
	[condition unlock];
	[[UNUserNotificationCenter currentNotificationCenter]
		getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings *settings) {
			[condition lock];
			if (refreshGeneration == uno_authorization_refresh_generation) {
				uno_authorization_status = settings.authorizationStatus;
				uno_authorization_status_ready = YES;
				uno_authorization_completed_generation = refreshGeneration;
			}
			[condition broadcast];
			[condition unlock];
		}];
}

static BOOL UNORefreshAuthorizationStatusAndWait(void)
{
	UNORefreshAuthorizationStatus();
	NSCondition *condition = UNOAuthorizationCondition();
	NSDate *timeout = [NSDate dateWithTimeIntervalSinceNow:UNONotificationOperationTimeoutSeconds];
	[condition lock];
	while (!uno_authorization_status_ready ||
		uno_authorization_completed_generation != uno_authorization_refresh_generation) {
		if (![condition waitUntilDate:timeout]) {
			[condition unlock];
			return NO;
		}
	}
	[condition unlock];
	return YES;
}

static BOOL UNOEnsureAuthorizationStatusReady(void)
{
	NSCondition *condition = UNOAuthorizationCondition();
	[condition lock];
	BOOL ready = uno_authorization_status_ready &&
		uno_authorization_completed_generation == uno_authorization_refresh_generation;
	[condition unlock];
	if (ready) {
		return YES;
	}
	return UNORefreshAuthorizationStatusAndWait();
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

@property(nonatomic, strong, nullable) id<UNUserNotificationCenterDelegate> previousDelegate;

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

typedef void (^UNOBoolCompletion)(BOOL succeeded);
typedef void (^UNOIdentifiersCompletion)(NSArray<NSString *> *identifiers);

static UNMutableNotificationContent *UNOCreateContent(NSDictionary *command);

static BOOL UNOIsPostingIdentifierSuffix(NSString *value)
{
	if (value.length != 32) {
		return NO;
	}
	NSCharacterSet *invalidCharacters = [[NSCharacterSet characterSetWithCharactersInString:@"0123456789abcdefABCDEF"] invertedSet];
	return [value rangeOfCharacterFromSet:invalidCharacters].location == NSNotFound;
}

static NSString * _Nullable UNOLogicalRequestIdentifier(NSDictionary *command)
{
	id commandId = command[@"id"];
	if ([commandId isKindOfClass:[NSNumber class]]) {
		unsigned long long identifier = [commandId unsignedLongLongValue];
		if (identifier > 0 && identifier <= UINT32_MAX) {
			return [NSString stringWithFormat:@"%@%llu", UNOImmediatePrefix, identifier];
		}
	}

	NSString *requestIdentifier = UNOString(command, @"requestIdentifier");
	if (![requestIdentifier hasPrefix:UNOScheduledPrefix]) {
		return nil;
	}
	NSString *value = [requestIdentifier substringFromIndex:UNOScheduledPrefix.length];
	NSRange separator = [value rangeOfString:@"."];
	NSString *scheduleIdentifier = separator.location == NSNotFound
		? value
		: [value substringToIndex:separator.location];
	return !UNOIsPostingIdentifierSuffix(scheduleIdentifier)
		? nil
		: [UNOScheduledPrefix stringByAppendingString:scheduleIdentifier];
}

static BOOL UNOIdentifierMatchesLogicalIdentifier(NSString *identifier, NSString *logicalIdentifier)
{
	if ([identifier isEqualToString:logicalIdentifier]) {
		return YES;
	}
	NSString *postingPrefix = [logicalIdentifier stringByAppendingString:@"."];
	return [identifier hasPrefix:postingPrefix] &&
		UNOIsPostingIdentifierSuffix([identifier substringFromIndex:postingPrefix.length]);
}

static BOOL UNOIsOperationActive(UNONotificationPostOperation *operation)
{
	@synchronized (operation) {
		return !operation.completed && !operation.canceled;
	}
}

static void UNOFinalizePostOperation(UNONotificationPostOperation *operation)
{
	NSMutableDictionary<NSString *, UNONotificationPostOperation *> *operations = UNORequestOperations();
	@synchronized (operations) {
		if (operations[operation.identifier] == operation) {
			[operations removeObjectForKey:operation.identifier];
		}
	}
	dispatch_semaphore_signal(operation->completion);
}

static BOOL UNOCompletePostOperation(UNONotificationPostOperation *operation, BOOL succeeded)
{
	BOOL completed = NO;
	@synchronized (operation) {
		if (!operation.completed) {
			operation.completed = YES;
			operation.succeeded = succeeded;
			completed = YES;
		}
	}
	if (completed) {
		UNOFinalizePostOperation(operation);
	}
	return completed;
}

static BOOL UNOCancelPostOperation(UNONotificationPostOperation *operation)
{
	BOOL canceled = NO;
	@synchronized (operation) {
		if (!operation.completed) {
			operation.canceled = YES;
			operation.completed = YES;
			operation.succeeded = NO;
			canceled = YES;
		}
	}
	if (canceled) {
		UNOFinalizePostOperation(operation);
	}
	return canceled;
}

static void UNORemoveRequestIdentifier(NSString *identifier)
{
	UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
	[center removePendingNotificationRequestsWithIdentifiers:@[identifier]];
	[center removeDeliveredNotificationsWithIdentifiers:@[identifier]];
	@synchronized (UNOStateGate()) {
		[uno_pending_identifiers removeObject:identifier];
		[uno_delivered_identifiers removeObject:identifier];
		uno_identifier_generation++;
	}
}

static void UNORegisterCategory(
	NSDictionary *command,
	UNONotificationPostOperation *operation,
	UNOBoolCompletion completionHandler)
{
	NSString *categoryIdentifier = UNOString(command, @"categoryIdentifier");
	NSArray *actionCommands = command[@"actions"];
	if (categoryIdentifier.length == 0 || ([actionCommands isKindOfClass:[NSArray class]] && actionCommands.count == 0)) {
		completionHandler(YES);
		return;
	}
	if (![actionCommands isKindOfClass:[NSArray class]]) {
		completionHandler(NO);
		return;
	}

	NSMutableArray<UNNotificationAction *> *actions = [NSMutableArray arrayWithCapacity:actionCommands.count];
	for (id value in actionCommands) {
		if (![value isKindOfClass:[NSDictionary class]]) {
			completionHandler(NO);
			return;
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
			if (!UNOIsOperationActive(operation)) {
				return;
			}
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
			completionHandler(YES);
		}];
}

static void UNOGetReplacedRequestIdentifiers(
	NSString *logicalIdentifier,
	NSString *postingIdentifier,
	UNONotificationPostOperation *operation,
	UNOIdentifiersCompletion completionHandler)
{
	UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
	dispatch_group_t group = dispatch_group_create();
	__block NSArray<UNNotificationRequest *> *pending = @[];
	__block NSArray<UNNotification *> *delivered = @[];

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
		if (!UNOIsOperationActive(operation)) {
			return;
		}
		NSMutableSet<NSString *> *identifiers = [NSMutableSet set];
		for (UNNotificationRequest *request in pending) {
			if (![request.identifier isEqualToString:postingIdentifier] &&
				UNOIdentifierMatchesLogicalIdentifier(request.identifier, logicalIdentifier)) {
				[identifiers addObject:request.identifier];
			}
		}
		for (UNNotification *notification in delivered) {
			NSString *identifier = notification.request.identifier;
			if (![identifier isEqualToString:postingIdentifier] &&
				UNOIdentifierMatchesLogicalIdentifier(identifier, logicalIdentifier)) {
				[identifiers addObject:identifier];
			}
		}
		completionHandler(identifiers.allObjects);
	});
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

static void UNOAddRequest(
	NSDictionary *command,
	double delaySeconds,
	NSString *logicalIdentifier,
	UNONotificationPostOperation *operation)
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

	if (!UNOIsOperationActive(operation)) {
		return;
	}
	[center addNotificationRequest:request withCompletionHandler:^(NSError *error) {
		if (error != nil) {
			NSLog(@"Unable to add app notification request: %@", error);
			UNOCompletePostOperation(operation, NO);
			return;
		}
		if (!UNOIsOperationActive(operation)) {
			UNORemoveRequestIdentifier(identifier);
			return;
		}
		UNOGetReplacedRequestIdentifiers(
			logicalIdentifier,
			identifier,
			operation,
			^(NSArray<NSString *> *replacedIdentifiers) {
				BOOL committed = NO;
				BOOL removePostingIdentifier = NO;
				@synchronized (operation) {
					if (operation.completed || operation.canceled) {
						removePostingIdentifier = YES;
					} else {
						[center removePendingNotificationRequestsWithIdentifiers:replacedIdentifiers];
						[center removeDeliveredNotificationsWithIdentifiers:replacedIdentifiers];
						@synchronized (UNOStateGate()) {
							[uno_pending_identifiers minusSet:[NSSet setWithArray:replacedIdentifiers]];
							[uno_delivered_identifiers minusSet:[NSSet setWithArray:replacedIdentifiers]];
							if (![uno_delivered_identifiers containsObject:identifier]) {
								[uno_pending_identifiers addObject:identifier];
							}
							uno_identifier_generation++;
						}
						operation.completed = YES;
						operation.succeeded = YES;
						committed = YES;
					}
				}
				if (removePostingIdentifier) {
					UNORemoveRequestIdentifier(identifier);
				} else if (committed) {
					UNOFinalizePostOperation(operation);
					UNORefreshIdentifiers();
				}
			});
	}];
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
	if (!UNORefreshAuthorizationStatusAndWait()) {
		return 0;
	}
	NSCondition *condition = UNOAuthorizationCondition();
	[condition lock];
	int32_t status = (int32_t)uno_authorization_status;
	[condition unlock];
	return status;
}

bool uno_notifications_initialize(void)
{
	if (!uno_notifications_is_supported()) {
		return false;
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
		delegate.previousDelegate = center.delegate;
		uno_notification_center_delegate = delegate;
		center.delegate = delegate;
	}
	UNORefreshIdentifiers();
	return UNOEnsureAuthorizationStatusReady();
}

void uno_notifications_request_authorization(void)
{
	(void)uno_notifications_initialize();
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
	NSString *logicalIdentifier = UNOLogicalRequestIdentifier(command);
	NSString *postingPrefix = [logicalIdentifier stringByAppendingString:@"."];
	if (logicalIdentifier == nil ||
		![identifier hasPrefix:postingPrefix] ||
		!UNOIsPostingIdentifierSuffix([identifier substringFromIndex:postingPrefix.length])) {
		return false;
	}
	UNONotificationPostOperation *operation = [[UNONotificationPostOperation alloc] initWithIdentifier:identifier];
	NSMutableDictionary<NSString *, UNONotificationPostOperation *> *operations = UNORequestOperations();
	@synchronized (operations) {
		operations[identifier] = operation;
	}
	UNORegisterCategory(
		command,
		operation,
		^(BOOL succeeded) {
			if (!succeeded) {
				UNOCompletePostOperation(operation, NO);
			} else if (UNOIsOperationActive(operation)) {
				UNOAddRequest(command, delay_seconds, logicalIdentifier, operation);
			}
		});

	dispatch_time_t timeout = dispatch_time(
		DISPATCH_TIME_NOW,
		UNONotificationOperationTimeoutSeconds * NSEC_PER_SEC);
	if (dispatch_semaphore_wait(operation->completion, timeout) != 0) {
		if (UNOCancelPostOperation(operation)) {
			UNORemoveRequestIdentifier(identifier);
			NSLog(@"Timed out while adding an app notification request.");
			return false;
		}
	}
	@synchronized (operation) {
		return operation.succeeded;
	}
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
	NSMutableDictionary<NSString *, UNONotificationPostOperation *> *operations = UNORequestOperations();
	UNONotificationPostOperation *operation;
	@synchronized (operations) {
		operation = operations[identifier];
	}
	if (operation != nil) {
		UNOCancelPostOperation(operation);
	}
	UNORemoveRequestIdentifier(identifier);
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
	NSMutableDictionary<NSString *, UNONotificationPostOperation *> *operations = UNORequestOperations();
	NSArray<UNONotificationPostOperation *> *matchingOperations;
	@synchronized (operations) {
		NSMutableArray<UNONotificationPostOperation *> *matches = [NSMutableArray array];
		for (NSString *identifier in operations) {
			if ([identifier hasPrefix:prefix]) {
				[matches addObject:operations[identifier]];
			}
		}
		matchingOperations = matches;
	}
	for (UNONotificationPostOperation *operation in matchingOperations) {
		UNOCancelPostOperation(operation);
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
	dispatch_group_t removals = dispatch_group_create();
	dispatch_group_enter(removals);
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
		dispatch_group_leave(removals);
	}];
	dispatch_group_enter(removals);
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
		dispatch_group_leave(removals);
	}];
	dispatch_time_t timeout = dispatch_time(
		DISPATCH_TIME_NOW,
		UNONotificationOperationTimeoutSeconds * NSEC_PER_SEC);
	if (dispatch_group_wait(removals, timeout) != 0) {
		NSLog(@"Timed out while removing app notification requests.");
		return false;
	}
	UNORefreshIdentifiers();
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