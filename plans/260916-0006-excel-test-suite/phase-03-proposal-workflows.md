# Phase 3 — Proposal draft, submit, and withdraw

## Context Links

- `FURPMS.Infrastructure/Services/ProposalService.cs`
- `FURPMS.API/Controllers/ProposalsController.cs`
- `FURPMS.Tests/Proposals/ProposalSubmissionTests.cs`
- `FURPMS.Tests/Proposals/ProposalDocumentUploadTests.cs`
- `FURPMS.Tests/Proposals/ProposalExtractionServiceTests.cs`
- `FURPMS.Tests/Proposals/FileNameNormalizationTests.cs`
- `FURPMS.Tests/Budget/ProposalBudgetServiceTests.cs`
- `FURPMS.Tests/Budget/BudgetCapPolicyTests.cs`

## Overview

- Priority: P1
- Status: Pending
- Effort: 6h
- Cover UT07–UT09 (45 cases) around draft persistence, submission locking/validation, and withdrawal.

## Key Insights and Mapping

| Sheet | Target | Existing coverage | Gap focus |
|---|---|---|---|
| UT07 saveDraft | `ProposalService.CreateProposalAsync`/`UpdateProposalAsync` | document/upload/extraction and budget tests overlap | create/update ownership, revision behavior, partial form, attachment metadata, budget totals/caps, UTC and idempotency |
| UT08 submitProposal | `ProposalService.SubmitProposalAsync` | `ProposalSubmissionTests` overlap | PI authorization, required fields, CV confirmation, deadline/cycle status, budget cap, duplicate submission, lock after submit, notification/reopen hook |
| UT09 withdrawProposal | `ProposalService.WithdrawProposalAsync` | no dedicated named file identified | owner-only, allowed status/window, repeated/missing proposal, persistence/status, notification/audit side effects |

## Requirements

- Respect dual intake: manual structured fields and optional upload+AI; tests must not require AI for normal submission.
- Verify QĐ543-derived budget policy through master data, including category percentage limits and total cap; avoid hardcoded test assumptions where settings are configurable.
- Assert soft-delete/status semantics and query-filter visibility where applicable.
- Test CV stale/up-to-date confirmation behavior without storing real personal files.

## Architecture

Use real proposal repositories and `TestServices` with deterministic users/cycles. Build proposals at each lifecycle state rather than mutating hidden fields in test code. Stub AI/document extraction through existing test doubles only; use representative synthetic documents for extraction tests already covered.

## Related Code Files

- Add or extend tests under `FURPMS.Tests/Proposals` and `FURPMS.Tests/Budget`.
- Reuse shared proposal/budget builders; no production changes in this phase unless a blocked contract decision is approved.

## Implementation Steps

1. Map each UTCID01–15 for UT07–UT09 to existing methods or new tests.
2. Add state-based datasets: draft, submitted, revision-required, withdrawn, expired/closed cycle.
3. Assert create/update field ownership, revision number, attachment, and budget persistence.
4. Assert submission prerequisites and exact exception category/status.
5. Assert post-submit immutability and withdrawal constraints.
6. Verify notifications are persisted through `TestNotifier`, not merely queued.

## Todo List

- [ ] Cover 45 proposal cases with stable IDs.
- [ ] Include normal, abnormal, and boundary classifications from workbook.
- [ ] Verify budget total is derived from six categories and cap is configurable.
- [ ] Verify submission locks data and triggers expected downstream hooks.
- [ ] Verify withdrawal is owner/status/deadline constrained.

## Success Criteria

- No duplicate tests for already-covered upload/extraction/budget rules.
- Every submission/withdrawal branch checks state and persistence.
- Tests remain deterministic with no real file-system or external AI dependency unless explicitly scoped as integration tests.

## Risks and Security

- Uploaded content can contain secrets; use disposable test files and clean them up.
- Soft-delete/query filters may hide records unexpectedly; assert both filtered and direct repository behavior where relevant.

## Next Steps

Use proposal fixtures from this phase as prerequisites for council/review phases.
