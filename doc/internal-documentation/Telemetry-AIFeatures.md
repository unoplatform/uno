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

## Privacy Notes

- **No PII**: No personally identifiable information is collected
- **Token Handling**: Raw bearer tokens are never logged; stable SHA256 hashes are used for opaque tokens
- **User ID**: Extracted from JWT claims when available

## Reference

For more detailed information, see the [Uno AI Features Repository](https://github.com/unoplatform/uno.ai-private).
