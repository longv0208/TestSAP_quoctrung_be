---
title: "Automated Excel Test Suite Plan"
description: "Map 225 Excel-designed cases to the .NET 8 services, extend existing xUnit coverage, and resolve specification gaps before execution."
status: pending
priority: P1
effort: 32h
branch: master
tags: [backend, testing, api, xunit, traceability]
created: 2026-09-16
---

# Automated Excel Test Suite Plan

## Overview

Convert the 15 Excel UT sheets (225 designed cases) into maintainable automated tests without duplicating the current suite or encoding behavior that production code/specification does not support. Preserve xUnit, EF Core InMemory, `TestServices`, `FakeClock`, and silent notification doubles. No code was changed during research.

## Baseline Findings

- Solution: API/Application/Domain/Infrastructure plus `FURPMS.Tests`; target `net8.0`.
- Framework: xUnit 2.5.3, Test SDK 17.8.0, coverlet 6.0.0, EF Core InMemory 8.0.11; no Moq.
- Existing tests already cover many business rules using real InMemory repositories; prefer this over mocks.
- Workbook status “Passed 225” is a template/cache value: execution dates and defect IDs are blank.
- Current environment lacks .NET SDK; baseline and final execution are blocked until SDK is installed.

## Phases

| # | Phase | Status | Effort | Link |
|---|---|---|---:|---|
| 1 | Harness, baseline, and traceability | Pending | 4h | [phase-01](./phase-01-harness-traceability.md) |
| 2 | Auth, users, and research cycles | Pending | 6h | [phase-02](./phase-02-auth-users-cycles.md) |
| 3 | Proposal draft, submit, and withdraw | Pending | 6h | [phase-03](./phase-03-proposal-workflows.md) |
| 4 | Councils, invitations, review, and scoring | Pending | 8h | [phase-04](./phase-04-council-review-scoring.md) |
| 5 | Progress and final reports | Pending | 4h | [phase-05](./phase-05-report-workflows.md) |
| 6 | Execute, coverage, and Excel reconciliation | Pending | 4h | [phase-06](./phase-06-execution-reconciliation.md) |

## Cross-cutting acceptance criteria

- Every `UTCID01`–`UTCID15` has a stable automated-test ID and source-sheet reference.
- Tests assert result, exception type/message category, persistence, authorization, notifications, and boundary values as applicable.
- Tests are isolated with unique InMemory database names and deterministic time; no real email or external AI calls.
- Workbook outcomes are updated only from actual test output; never mark designed cases as passed in advance.
- Production behavior changes require explicit design decision and matching API/business-rule documentation before tests are changed.

## Dependencies and decisions

- Install .NET 8 SDK and restore packages before execution.
- Resolve UT06 state-machine scope, UT12 review workflow ownership, UT02 password-confirmation/policy scope, and UT10 role/chair rules before finalizing assertions.
- Reconcile Excel exception vocabulary with project middleware conventions (`ArgumentException`, `InvalidOperationException`, `ForbiddenException`, `KeyNotFoundException`).
- Keep existing tests; add focused cases or parameterized data only where coverage is missing.

## Out of scope

- No production/test code changes in this planning turn.
- No forced Moq adoption, API redesign, or implementation of undocumented Excel behavior.
