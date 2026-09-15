# Phase 5 — Progress and final reports

## Context Links

- `FURPMS.Infrastructure/Services/ProgressReportService.cs`
- `FURPMS.Infrastructure/Services/FinalReportService.cs`
- `FURPMS.API/Controllers/ProgressReportsController.cs`
- `FURPMS.API/Controllers/FinalReportsController.cs`
- Existing tests: `FURPMS.Tests/Progress/ProgressReportEvaluationTests.cs`, `ProgressReportScheduleTests.cs`, `FinalReportServiceTests.cs`, `FURPMS.Tests/Contracts/ContractLifecycleTests.cs`, `ContractClosureTests.cs`

## Overview

- Priority: P1
- Status: Pending
- Effort: 4h
- Cover UT14–UT15 (30 cases) using contract/report fixtures and current staff-vs-council workflow rules.

## Key Insights and Mapping

| Sheet | Target | Existing coverage | Gap focus |
|---|---|---|---|
| UT14 submitProgressReport | `ProgressReportService.SubmitAsync`, `ProgressReportsController` | schedule/evaluation tests overlap | owner/contract linkage, report period, required file/content, deadline, duplicate submission, status and notification |
| UT15 submitFinalReport | `FinalReportService.SubmitAsync`, `FinalReportsController` | `FinalReportServiceTests` overlap | contract state, required final deliverables, file validation, owner/permission, duplicate/late submit, downstream acceptance/closure effects |

## Requirements

- Progress reports are submitted by PI and evaluated directly by Staff, not an invented progress council.
- Final submission must respect contract/project lifecycle and current acceptance result/finalization rules.
- Assert file metadata/size/type through existing configuration, but avoid brittle binary snapshots.
- Assert report period and deadline using UTC/fake clock; distinguish late submission from missing resource and permission errors.

## Architecture

Seed a completed proposal, contract, schedule, and relevant deliverables. Exercise service methods for business rules; use controller tests only for claims, route, and response envelope. Reuse existing report builders and notification persistence checks.

## Related Code Files

- Extend `FURPMS.Tests/Progress` and, if necessary, `FURPMS.Tests/Contracts`.
- Shared fixture changes belong in `FURPMS.Tests/Helpers` and must remain backwards-compatible.
- No production changes in this plan unless a failing case exposes an approved rule defect.

## Implementation Steps

1. Map 15 cases for each sheet to existing tests/new tests.
2. Add report lifecycle/state/ownership datasets.
3. Add file and boundary deadline tests.
4. Verify persisted status, audit/notification side effects, and idempotency.
5. Verify final report prerequisites and contract closure behavior.

## Todo List

- [ ] Complete UT14 matrix and identify duplicate schedule/evaluation assertions.
- [ ] Complete UT15 matrix and reuse final-report tests.
- [ ] Cover abnormal and boundary rows without real email/external storage.
- [ ] Verify staff evaluation remains separate from council scoring.

## Success Criteria

- All implementable report cases are green and deterministic.
- No progress test requires a council or reviewer role.
- Final-report assertions match current contract/acceptance state machine.

## Risks and Security

- File upload tests must use disposable local fixtures and validate allow-list/size without embedding sensitive documents.
- Avoid asserting implementation-specific storage paths or URLs.

## Next Steps

Combine all phase outputs in phase 6 and reconcile the workbook only from test artifacts.
