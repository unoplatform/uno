//
//  UNONotifications.h
//

#pragma once

#import "UnoNativeMac.h"
#import <UserNotifications/UserNotifications.h>

NS_ASSUME_NONNULL_BEGIN

typedef void (*uno_notification_activated_fn_ptr)(
	const char *request_identifier,
	const char *argument,
	const char *protocol_uri,
	const char *input_id,
	const char *user_text);

typedef void (*uno_notification_delivered_fn_ptr)(const char *request_identifier);

bool uno_notifications_is_supported(void);
int32_t uno_notifications_get_setting(void);
void uno_notifications_initialize(void);
void uno_notifications_request_authorization(void);
bool uno_notifications_post(const char *command_json, double delay_seconds);
bool uno_notifications_remove(const char *request_identifier);
bool uno_notifications_remove_all(const char *request_identifier_prefix);
char * _Nullable uno_notifications_get_identifiers_json(
	const char *request_identifier_prefix,
	bool include_pending,
	bool include_delivered);
void uno_notifications_free_string(char * _Nullable value);
void uno_notifications_set_callbacks(
	uno_notification_activated_fn_ptr activated,
	uno_notification_delivered_fn_ptr delivered);

NS_ASSUME_NONNULL_END