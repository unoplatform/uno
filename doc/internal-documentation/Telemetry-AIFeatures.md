# AI Features Telemetry

**Event Name Prefix:** `uno/ai`

AI Features use Application Insights with custom telemetry initializers to track design threads and operations.

## Application Insights

AI Features telemetry uses Azure Application Insights with custom telemetry initializers that add request context fields to all telemetry items.

## Request Context Fields

The following fields are added to all telemetry requests:

| Field Name | Type | Description |
|------------|------|-------------|
| `design_thread_id` | GUID | Unique identifier for the design thread |
| `parent_design_thread_id` | GUID | Parent design thread identifier (for nested operations) |
| `operation_phase` | Enum | Phase of the operation: `UxDesign`, `XamlGeneration`, `UiImprovement`, `FitnessEvaluation` |
| `loop_iteration` | String | Iteration number for loops |

## Azure Tables Storage

The `CallDetailsEntry` entity in Azure Tables includes:

| Field | Description |
|-------|-------------|
| `DesignThreadId` | Unique design thread identifier |
| `ParentDesignThreadId` | Parent design thread identifier |
| `OperationPhase` | Current operation phase |
| `LoopIteration` | Loop iteration number |

## Field Value Examples

Example values for AI Features telemetry fields:

- **design_thread_id**: "3fa85f64-5717-4562-b3fc-2c963f66afa6" (GUID format)
- **parent_design_thread_id**: "7d4c8a91-2b45-4f89-a1c3-9e7f6d5c4b3a" (GUID format, or null for root operations)
- **operation_phase**: "UxDesign", "XamlGeneration", "UiImprovement", "FitnessEvaluation"
- **loop_iteration**: "1", "2", "3", etc. (string representation of iteration count)

## Privacy Notes

- **No PII**: No personally identifiable information is collected
- **Token Handling**: Raw bearer tokens are never logged; stable SHA256 hashes are used for opaque tokens
- **User ID**: Extracted from JWT claims when available

## Reference

For more detailed information, see the [Uno AI Features Repository](https://github.com/unoplatform/uno.ai-private).
