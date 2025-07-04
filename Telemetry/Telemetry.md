# DevServer Telemetry Events Inventory

This table lists all telemetry events emitted by the Uno DevServer, with their properties and measurements, for GDPR/privacy review. A red dot (🔴) marks fields that should be anonymized. The last column indicates if the event is global (server-wide) or per-connection.

| Event Name                      | Properties (string)                                                                                      | Measurements (double)                        | Sensitive / Notes                            | Scope         |
|----------------------------------|---------------------------------------------------------------------------------------------------------|----------------------------------------------|----------------------------------------------|---------------|
| **DevServer.Startup**            | HasSolution, MachineName🔴, OSVersion🔴                                                                  | ProcessorCount                              | MachineName🔴, OSVersion🔴 may be sensitive   | Global        |
| **DevServer.Shutdown**           | ShutdownType ("Graceful"/"Crash")                                                                      | UptimeSeconds                               |                                              | Global        |
| **DevServer.StartupFailure**     | ErrorMessage🔴, ErrorType, StackTrace🔴                                                                  | UptimeSeconds                               | ErrorMessage🔴/StackTrace🔴 may be sensitive  | Global        |
| **AddIn.Discovery.Start**        | SolutionId                                                                                              |                                              | SolutionId is a new GUID per run             | Global        |
| **AddIn.Discovery.Complete**     | SolutionId, Result, AddInList                                                                           | AddInCount, DurationMs                      | AddInList: filenames only                    | Global        |
| **AddIn.Discovery.Error**        | SolutionId, ErrorMessage🔴, ErrorType                                                                   | DurationMs                                  | ErrorMessage🔴 may be sensitive               | Global        |
| **AddIn.Loading.Start**          | AssemblyList                                                                                            |                                              | AssemblyList: filenames only                 | Global        |
| **AddIn.Loading.Complete**       | AssemblyList, Result                                                                                    | DurationMs, LoadedAssemblies, FailedAssemblies |                                              | Global        |
| **AddIn.Loading.Error**          | AssemblyList, ErrorMessage🔴, ErrorType                                                                 | DurationMs, LoadedAssemblies, FailedAssemblies | ErrorMessage🔴 may be sensitive               | Global        |
| **Processor.Discovery.Start**    | AppInstanceId, BasePath🔴, IsFile                                                                       |                                              | BasePath🔴 may be a local path               | Global        |
| **Processor.Discovery.Complete** | AppInstanceId, BasePath🔴, IsFile, Result                                                               | DurationMs, AssembliesProcessed, ProcessorsLoadedCount, ProcessorsFailedCount | BasePath🔴 may be a local path                | Global        |
| **Processor.Discovery.Error**    | ErrorMessage🔴, ErrorType                                                                               | DurationMs, AssembliesCount, ProcessorsLoadedCount, ProcessorsFailedCount | ErrorMessage🔴 may be sensitive               | Global        |
| **Client.Connection.Opened**     | (All key/value pairs from connectionContext.Metadata)🔴                                                 |                                              | Metadata may contain sensitive info🔴         | Per-connection |
| **Client.Connection.Closed**     | ConnectionId, RemoteIpAddress🔴                                                                         | DurationSeconds                             | RemoteIpAddress🔴 may be sensitive            | Per-connection |

**Notes:**
- Fields marked with a red dot (🔴) are potentially sensitive and should be anonymized or excluded in production telemetry.
- Lists (AddInList, AssemblyList) contain only filenames, not full paths.
- Connection metadata should be audited to ensure no personal data is transmitted.
- When in doubt, prefer anonymization or exclusion of these fields in production analytics.
