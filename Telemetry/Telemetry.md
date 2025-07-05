# DevServer Telemetry Events Inventory

This table lists all telemetry events emitted by the Uno DevServer, with their properties and measurements, for GDPR/privacy review. A red dot (🔴) marks fields that are anonymized using a centralized hash helper. The last column indicates if the event is global (server-wide) or per-connection.

| Event Name                      | Properties (string)                                                                                      | Measurements (double)                        | Sensitive / Notes                            | Scope         |
|----------------------------------|---------------------------------------------------------------------------------------------------------|----------------------------------------------|----------------------------------------------|---------------|
| **DevServer.Startup**            | HasSolution, MachineName🔴, OSVersion                                                                   | ProcessorCount                              | MachineName🔴 is anonymized; OSVersion is raw | Global        |
| **DevServer.Shutdown**           | ShutdownType ("Graceful"/"Crash")                                                                      | UptimeSeconds                               |                                              | Global        |
| **DevServer.StartupFailure**     | ErrorMessage, ErrorType, StackTrace                                                                     | UptimeSeconds                               | ErrorMessage/StackTrace may be sensitive (not anonymized) | Global        |
| **AddIn.Discovery.Start**        | SolutionId🔴                                                                                             |                                              | SolutionId🔴 = hash(solution path + machine name) | Global        |
| **AddIn.Discovery.Complete**     | SolutionId🔴, Result, AddInList                                                                          | AddInCount, DurationMs                      | AddInList: filenames only                    | Global        |
| **AddIn.Discovery.Error**        | SolutionId🔴, ErrorMessage, ErrorType                                                                    | DurationMs                                  | ErrorMessage may be sensitive (not anonymized) | Global        |
| **AddIn.Loading.Start**          | AssemblyList                                                                                            |                                              | AssemblyList: filenames only                 | Global        |
| **AddIn.Loading.Complete**       | AssemblyList, Result                                                                                    | DurationMs, LoadedAssemblies, FailedAssemblies |                                              | Global        |
| **AddIn.Loading.Error**          | AssemblyList, ErrorMessage, ErrorType                                                                   | DurationMs, LoadedAssemblies, FailedAssemblies | ErrorMessage may be sensitive (not anonymized) | Global        |
| **Processor.Discovery.Start**    | AppInstanceId, BasePath🔴, IsFile                                                                       |                                              | BasePath🔴 is anonymized                      | Global        |
| **Processor.Discovery.Complete** | AppInstanceId, BasePath🔴, IsFile, Result                                                               | DurationMs, AssembliesProcessed, ProcessorsLoadedCount, ProcessorsFailedCount | BasePath🔴 is anonymized                      | Global        |
| **Processor.Discovery.Error**    | ErrorMessage, ErrorType                                                                                 | DurationMs, AssembliesCount, ProcessorsLoadedCount, ProcessorsFailedCount | ErrorMessage may be sensitive (not anonymized) | Global        |
| **Client.Connection.Opened**     | (All key/value pairs from connectionContext.Metadata)🔴                                                 |                                              | Metadata fields are anonymized               | Per-connection |
| **Client.Connection.Closed**     | ConnectionId, RemoteIpAddress🔴                                                                         | DurationSeconds                             | RemoteIpAddress🔴 is anonymized               | Per-connection |

**Notes:**
- Only fields marked with a red dot (🔴) are anonymized using the centralized `TelemetryHashHelper` (MD5, lowercase hex, no dashes).
- OSVersion, ErrorMessage, and StackTrace are sent as raw values and may contain sensitive information; handle with care.
- Special values: null → "unknown", empty string → "empty".
- Lists (AddInList, AssemblyList) contain only filenames, not full paths.
- Connection metadata is always hashed/anonymized before emission.
- When in doubt, prefer anonymization or exclusion of these fields in production analytics.
- The anonymization is stable and deterministic for the same input, but not reversible.
