# Privacy & Compliance

## GDPR Compliance

Uno Platform telemetry is designed to be GDPR compliant:

- **Transparency**: All telemetry collection is documented and disclosed
- **User Control**: Users can opt out at any time using environment variables or IDE settings
- **Data Minimization**: Only essential, non-identifying data is collected
- **No PII**: No personally identifiable information is collected
- **Security**: Data is transmitted securely to Microsoft Azure Application Insights

## Data Collection Policy

### What IS Collected

- Event names and timestamps
- Version information (IDE, plugin, SDK)
- Anonymous user IDs (hashed tokens, hashed MAC addresses)
- Performance measurements (durations, counts)
- Feature usage patterns (which features are used, how often)
- Error types (exception types only, no stack traces)
- Operating system and architecture information
- Target frameworks and platforms
- Hashed directory paths (SHA256)

### What is NOT Collected

- **Personal Information**: No usernames, email addresses, or account details
- **User Content**: No file paths, source code, or project-specific information
- **Detailed Errors**: No stack traces or detailed error messages that could contain sensitive data
- **Raw Tokens**: Bearer tokens are never logged; only SHA256 hashes for opaque tokens

## Disabling Telemetry

Users can disable telemetry in multiple ways:

### Visual Studio
1. Tools → Options → Uno Platform → Telemetry
2. Uncheck "Enable telemetry"

### VS Code
1. Open Settings (Ctrl+,)
2. Search for "Uno Platform Telemetry"
3. Uncheck "Enable Telemetry"

### Rider
1. File → Settings → Tools → Uno Platform
2. Uncheck "Send usage statistics"

### Environment Variable
Set the environment variable:
```bash
export UNO_PLATFORM_TELEMETRY_OPTOUT=true
```

Or in Windows:
```powershell
$env:UNO_PLATFORM_TELEMETRY_OPTOUT = "true"
```

This will disable telemetry for all Uno Platform tools.

## Data Retention

- **Application Insights**: Default retention is 90 days
- **Azure Tables**: Configurable retention based on storage policies

Data is automatically purged after the retention period.

## Instrumentation Keys

| Environment | Instrumentation Key |
|-------------|---------------------|
| **Production** | `9a44058e-1913-4721-a979-9582ab8bedce` |
| **Development** | `81286976-e3a4-49fb-b03b-30315092dbc4` |

## Contact

For privacy concerns or questions about telemetry:

- **Email**: info@platform.uno
- **GitHub Issues**: [unoplatform/uno/issues](https://github.com/unoplatform/uno/issues)
