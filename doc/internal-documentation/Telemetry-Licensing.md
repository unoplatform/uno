# Licensing Telemetry

**Event Name Prefix:** `uno/licensing`

Licensing telemetry tracks license management operations, user navigation, and API interactions.

## License Manager Events (Client)

Client-side license manager events:

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `logout-requested` | - | - | User requested logout |
| `logout-success` | - | - | Logout completed successfully |
| `logout-failure` | `error` (string) | - | Logout failed |
| `login-requested` | - | - | User requested login |
| `login-success` | - | - | Login completed successfully |
| `login-failure` | `error` (string), `errorDescription` (string) | - | Login failed |
| `login-canceled` | - | - | User canceled login |
| `login-timeout` | `error` (string), `errorDescription` (string) | - | Login timed out |
| `license-status` | `LicenseName` (string), `LicenseStatus` (string), `TrialDaysRemaining` (int), `LicenseExpiryDate` (date) | - | License status checked |
| `manager-started` | `Feature` (string) | - | License manager started for a feature |
| `manager-failed` | `errorType` (string), `errorStackTrace` (string) | - | License manager failed |

## Navigation Events

User navigation events:

| Event Name | Description |
|------------|-------------|
| `nav-to-my-account` | User navigated to My Account |
| `nav-to-help-overview` | User navigated to Help Overview |
| `nav-to-help-getting-started` | User navigated to Getting Started help |
| `nav-to-help-counter-tutorial` | User navigated to Counter Tutorial |
| `nav-to-help-report-issue` | User navigated to Report Issue |
| `nav-to-help-suggest-feature` | User navigated to Suggest Feature |
| `nav-to-help-ask-question` | User navigated to Ask Question |
| `nav-to-discord-server` | User navigated to Discord server |
| `nav-to-youtube-channel` | User navigated to YouTube channel |
| `nav-to-end-user-agreement` | User navigated to End User Agreement |
| `nav-to-privacy-policy` | User navigated to Privacy Policy |
| `nav-to-purchase-now` | User navigated to Purchase |
| `nav-to-trial-extension` | User navigated to Trial Extension |

## License API Events (Server)

Server-side licensing API events:

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `get-user` | - | - | User info requested |
| `get-user-success` | - | `DurationMs` | User info retrieved successfully |
| `get-user-failure` | `Error` (string) | `DurationMs` | User info retrieval failed |
| `get-licenses` | - | - | Licenses requested |
| `get-licenses-success` | - | `DurationMs` | Licenses retrieved successfully |
| `get-licenses-failure` | `Error` (string) | `DurationMs` | License retrieval failed |
| `get-offers` | - | - | Offers requested |
| `get-offers-success` | `Details` (string) | `DurationMs` | Offers retrieved successfully |
| `get-offers-failure` | `Error` (string) | `DurationMs` | Offer retrieval failed |
| `get-features` | - | - | Features requested |
| `get-features-success` | - | `DurationMs` | Features retrieved successfully |
| `get-features-failure` | - | `DurationMs` | Feature retrieval failed |
| `invalid-api-key` | - | - | API key validation failed |
| `invalid-token` | - | - | Token validation failed |
| `no-license-id` | - | - | No license ID provided |
| `id-not-found` | - | - | License ID not found |
| `license-feature-not-granted` | `Feature` (string) | - | Feature not granted by license |
| `license-feature-info-returned` | `Feature` (string) | - | Feature info returned |

## DevServer Licensing Events

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `license-manager-start` | `Feature` (string) | - | License manager started in DevServer |

## Property Value Examples

Common property values across Licensing events:

- **Error**: "NetworkError", "AuthenticationFailed", "ServerTimeout", "InvalidCredentials", "ConnectionRefused"
- **error / errorDescription**: Human-readable error messages like "Unable to connect to authentication server", "Invalid username or password"
- **LicenseName**: "Community", "Professional", "Enterprise", "Trial"
- **LicenseStatus**: "Active", "Expired", "Suspended", "NotFound", "Invalid"
- **TrialDaysRemaining**: 0-30 (integer representing days)
- **Feature**: "HotDesign", "HotReload", "XAML Designer", "Mobile Support"
- **ErrorType**: "NetworkException", "TimeoutException", "AuthException", "ValidationException"
- **Details** (get-offers-success): JSON string containing offer information

## Reference

For more detailed information, see the [Uno Licensing Repository](https://github.com/unoplatform/uno.licensing).
