# fenster — Sprint 6 Blazor Decisions

**Date:** Sprint 6
**Author:** Fenster (Frontend Dev)

## Decisions

### CSV Export deferred (JS interop)
`SalesSummaryPage.ExportCsvAsync` builds the CSV string in memory but triggers a snackbar placeholder instead of a browser file download. Wiring `IJSRuntime` + a JS helper for `URL.createObjectURL` / `<a download>` is deferred to Sprint 7. The data path is complete; only the browser trigger is missing.

### SignalR degrades gracefully in dev
Both `ApprovalList` and `NotificationBell` wrap `HubService.StartAsync()` in a try/catch. In local dev without real Azure AD tokens, SignalR connection will fail silently — the pages still load and function with manual refresh. No dev-time stubs or mock hubs added.

### NotificationBell — client-side read state only
`NotificationEntry.IsRead` is managed entirely in-memory on the Blazor circuit. Marking notifications as read does **not** persist to any backend. If the user navigates away or the circuit is reset, unread state resets. A server-side read-receipt endpoint is out of scope for Sprint 6.

### `NotificationEntry` defined in Services layer
`NotificationEntry` record lives in `Services/NotificationHubService.cs` so both the hub service and the `NotificationBell` Shared component can reference it without a circular dependency. The model belongs with the service that produces it.
