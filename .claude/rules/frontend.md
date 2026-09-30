---
paths:
  - "src/TaskFlow.Web/**"
---

# Frontend rules (TaskFlow.Web)

- API calls go through `api/client.ts` (`api<T>()`). It adds the bearer token, retries once after a refresh on 401,
  and throws `ApiError` with `errorCode` and field `errors`. Don't call `fetch` directly.
- Server state lives in TanStack Query hooks in `api/tasks.ts`. Mutations invalidate `taskKeys.all` and
  `taskKeys.summary`. Status changes stay optimistic, with rollback and a toast.
- The access token lives only in memory (`AuthProvider`). Never write tokens to localStorage or sessionStorage.
- Filters, sort and view live in the URL (`useTaskFilters`), not in component state.
- Dropdowns use `components/Select` (Radix popover at ≥768px, bottom sheet below). Modals, drawers, sheets and confirms
  use `components/Overlay` (Radix Dialog, so scroll lock and focus trap come for free). Don't use native `<select>` or
  hand-rolled overlays.
- Styling: CSS Modules plus the tokens in `styles/tokens.css`; no hard-coded colours outside the tokens. The breakpoints
  are ≥1024 desktop, 768–1023 tablet and <768 phone, and must match `docs/mockups/index.html`. Layout grids use
  `minmax(0, 1fr)`, so no width ever scrolls sideways.
- Priority is never shown by colour alone (bars + label). Every control has an accessible name, and form errors use
  `aria-invalid` + `aria-describedby`.
- Client validation mirrors the API rules (`validateTask`, `passwordChecks`, `LIMITS` in `api/types.ts`); keep them in
  sync with `TaskFieldLimits` and `AccountRules`.
- Props callbacks use property syntax (`onChange: (v: T) => void`), not method syntax (type-aware lint flags unbound methods).
  No `autoFocus`: dialogs focus a field marked `data-initial-focus`. Rows ignore clicks from controls (`fromControl`).
- Before committing: `npm run typecheck` (it includes the test files), `npm run lint`, `npm run knip`, `npm test` and `npm run build`. For layout
  changes, also check 375, 768 and 1024+ widths in the browser.
