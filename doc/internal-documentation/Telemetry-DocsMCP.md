# Docs MCP Telemetry

**Event Name Prefix:** `uno/docs-mcp`

The Uno Platform Docs MCP (Model Context Protocol) is an Azure-hosted service providing documentation search and retrieval capabilities for Uno Platform documentation. Telemetry tracks search patterns, performance, caching efficiency, and ingestion operations.

---

## Overview

- **Service Name**: Uno Platform Docs MCP
- **Repository**: https://github.com/unoplatform/uno.docs-mcp
- **Event Name Prefix**: `uno/docs-mcp`
- **Description**: Azure-hosted Model Context Protocol service for Uno Platform documentation search and retrieval

---

## Telemetry Implementation

- **Telemetry Framework**: Application Insights via `Microsoft.ApplicationInsights.AspNetCore v2.23.0`
- **Service**: `MonitoringService` (implements `IMonitoringService`)
- **Location**: `src/AzureUnoDocsMcp/Services/MonitoringService.cs`

---

## Custom Events Tracked

| Event Name | Description | Properties | Metrics |
|------------|-------------|------------|---------|
| `SearchRequest` | Tracks incoming search queries | `Query` (string), `TopK` (string), `ContentType` (string, optional), `UserAgent` (string, optional) | None |
| `SearchResults` | Tracks search results and performance | `Query` (string), `ResultCount` (string), `FromCache` (string "true"/"false"), `UserAgent` (string, optional) | `ResponseTimeMs` (double), `ResultsPerSecond` (double) |
| `CacheHit` | Tracks successful cache retrievals | `CacheKeyHash` (string, 8-char SHA256 hash), `OperationType` (string) | None |
| `CacheMiss` | Tracks cache misses requiring fresh data retrieval | `CacheKeyHash` (string, 8-char SHA256 hash), `OperationType` (string) | None |
| `ThrottleEvent` | Tracks rate limiting and throttling events | `ClientId` (string), `EventType` (string: "throttled", "allowed", "queued") | `WaitDurationMs` (double), `CurrentRequestCount` (int) |
| `IngestionEvent` | Tracks document ingestion and processing | `EventType` (string: "started", "completed", "error") | `DocumentCount` (int), `ProcessingTimeMs` (double), `DocumentsPerSecond` (double, calculated) |
| `SearchError` | Tracks search failures and exceptions | `Query` (string), `UserAgent` (string, optional) | None |

**Implementation Notes:**
- **SearchRequest**: Uses `RequestTelemetry` to track incoming HTTP requests
- **SearchResults**: Uses `EventTelemetry` with calculated `ResultsPerSecond` metric (resultCount / (responseTimeMs / 1000.0))
- **CacheHit/CacheMiss**: Uses `EventTelemetry` for cache performance tracking
- **ThrottleEvent**: Uses `EventTelemetry`; negative wait durations are logged as warnings and not tracked
- **IngestionEvent**: Uses `EventTelemetry`; `DocumentsPerSecond` calculated when processingTimeMs > 0 and documentCount > 0
- **SearchError**: Uses `ExceptionTelemetry` with Message property

---

## Custom Metrics Tracked

| Metric Name | Type | Description |
|-------------|------|-------------|
| `SearchResponseTime` | Double | Individual search performance in milliseconds |
| `SearchResultCount` | Int | Number of results returned per search |
| `CacheHitRate` | Counter | Incremented by 1 when search results from cache |
| `CacheMissRate` | Counter | Incremented by 1 when search requires fresh retrieval |
| `SearchErrors` | Counter | Incremented by 1 on each search error |
| `Cache.{operationType}.Hits` | Counter | Granular cache hits by operation type (e.g., Cache.Search.Hits) |
| `Cache.{operationType}.Misses` | Counter | Granular cache misses by operation type (e.g., Cache.Search.Misses) |
| `Throttle.{eventType}` | Counter | Count of throttle events by type (e.g., Throttle.throttled, Throttle.allowed) |
| `Throttle.{eventType}.WaitDurationMs` | Double | Wait duration by throttle event type |
| `Throttle.WaitDurationMs` | Double | Overall throttling wait duration |
| `Ingestion.{eventType}` | Counter | Count of ingestion events by type (e.g., Ingestion.started, Ingestion.completed) |

---

## Privacy and Security

Based on the MonitoringService.cs implementation:

- **Query Hashing**: Full queries are tracked in properties (not hashed)
- **Cache Key Hashing**: Cache keys are hashed using SHA256, first 8 hex characters only (e.g., "A1B2C3D4")
- **User Agent Tracking**: Optional User-Agent strings are tracked when provided
- **Client Identification**: ClientId tracked for throttling events
- **No PII**: Implementation focuses on operational metrics

---

## Property Value Examples

### SearchRequest Example

- **Query**: "uno platform xaml binding"
- **TopK**: "10"
- **ContentType**: "documentation"
- **UserAgent**: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"

### SearchResults Example

- **Query**: "uno platform xaml binding"
- **ResultCount**: "8"
- **FromCache**: "true"
- **UserAgent**: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
- **ResponseTimeMs**: 45.3
- **ResultsPerSecond**: 176.82

### CacheHit Example

- **CacheKeyHash**: "A1B2C3D4"
- **OperationType**: "Search"

### CacheMiss Example

- **CacheKeyHash**: "E5F6G7H8"
- **OperationType**: "Document"

### ThrottleEvent Example

- **ClientId**: "client-abc123"
- **EventType**: "throttled"
- **WaitDurationMs**: 500.0
- **CurrentRequestCount**: 15

### IngestionEvent Example

- **EventType**: "completed"
- **DocumentCount**: 1250
- **ProcessingTimeMs**: 5432.1
- **DocumentsPerSecond**: 230.08

### SearchError Example

- **Query**: "invalid search query"
- **UserAgent**: "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)"
- **Message**: "Search service unavailable"

---

## Automatic ASP.NET Core Telemetry

Application Insights automatically tracks:
- HTTP request/response tracking with correlation IDs
- Dependency calls to Azure AI Search API
- Exception tracking with full stack traces
- Performance counters and system metrics

---

## Monitoring Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/monitoring/health` | GET | Simple health status check |
| `/api/monitoring/info` | GET | Monitoring guidance and Application Insights portal links |
| `/healthz` | GET | Basic health check |

---

## Configuration

Application Insights is configured in `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key-here..."
  },
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

---

## Example KQL Queries

### Cache Hit Rate Over Time
```kql
// Cache hit rate over time
customMetrics
| where name in ("CacheHitRate", "CacheMissRate")
| summarize hits = sum(valueCount) by bin(timestamp, 1h), name
| render timechart
```

### Top Search Patterns
```kql
// Top search patterns
customEvents
| where name == "SearchResults"
| summarize requests = count(), 
            avgResponseTime = avg(todouble(customMeasurements.ResponseTimeMs)) 
    by Query = tostring(customDimensions.Query)
| top 10 by requests
```

### Throttling Analysis
```kql
// Throttling analysis
customEvents
| where name == "ThrottleEvent"
| summarize count() by EventType = tostring(customDimensions.EventType), 
                       bin(timestamp, 1h)
| render timechart
```

### Search Performance Analysis
```kql
// Search performance analysis
customEvents
| where name == "SearchResults"
| extend responseTime = todouble(customMeasurements.ResponseTimeMs)
| extend resultCount = toint(customDimensions.ResultCount)
| extend fromCache = tobool(customDimensions.FromCache)
| summarize 
    avgResponseTime = avg(responseTime),
    p50ResponseTime = percentile(responseTime, 50),
    p95ResponseTime = percentile(responseTime, 95),
    avgResultCount = avg(resultCount)
    by fromCache
```

### Ingestion Monitoring
```kql
// Ingestion monitoring
customEvents
| where name == "IngestionEvent"
| extend eventType = tostring(customDimensions.EventType)
| extend docCount = toint(customMeasurements.DocumentCount)
| extend processingTime = todouble(customMeasurements.ProcessingTimeMs)
| summarize 
    totalDocs = sum(docCount),
    avgProcessingTime = avg(processingTime),
    count() by eventType
| render barchart
```

---

## References

- **Source Repository**: https://github.com/unoplatform/uno.docs-mcp
- **Monitoring Service**: `src/AzureUnoDocsMcp/Services/MonitoringService.cs`
- **Interface Definition**: `src/AzureUnoDocsMcp/Services/IMonitoringService.cs`
- **Implementation Summary**: `docs/Phase2-Implementation-Summary.md`
- **Tests**: `src/AzureUnoDocsMcp.Tests/MonitoringServiceTests.cs`
