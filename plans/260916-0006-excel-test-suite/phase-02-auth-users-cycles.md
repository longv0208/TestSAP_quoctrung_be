# Phase 2 — Auth, users, and research cycles

## Context Links

- `FURPMS.Infrastructure/Services/AuthService.cs`
- `FURPMS.Infrastructure/Services/UserService.cs`
- `FURPMS.Infrastructure/Services/CycleService.cs`
- `FURPMS.API/Controllers/AuthController.cs`
- `FURPMS.API/Controllers/UsersController.cs`
- `FURPMS.API/Controllers/CyclesController.cs`
- Existing tests: `FURPMS.Tests/Users/AcademicWorksTests.cs`, `FURPMS.Tests/MasterData/SystemSettingServiceTests.cs`, `FURPMS.Tests/Cycles/CycleValidationTests.cs`, `FURPMS.Tests/Cycles/CycleDeadlineExtensionTests.cs`

## Overview

- Priority: P1
- Status: Pending
- Effort: 6h
- Implement/complete 90 cases for UT01–UT06, reusing existing coverage and isolating unresolved contract differences.

## Key Insights and Mapping

| Sheet | Target | Existing overlap | Add/verify |
|---|---|---|---|
| UT01 login | `AuthService.LoginAsync`, `AuthController` | partial auth tests may exist outside named Excel suite | valid login, unknown/disabled user, wrong password, malformed input, claims/role payload, case/whitespace policy |
| UT02 changePassword | `AuthService.ChangePasswordAsync` | no dedicated file identified | current-password failure, same password, hash update, missing user, authorization, password policy; confirmation is blocked because DTO has only current/new password |
| UT03 createUser | `UserService.CreateUserAsync`, `UsersController` | user/academic tests partial | required fields, email format/duplicate, role validity, actor permission, default state, audit/notification behavior |
| UT04 updateUser | `UserService.UpdateUserAsync`, `UsersController` | user tests partial | missing user, duplicate email, protected fields, role/actor permission, inactive/deleted user, idempotent update |
| UT05 createResearchCycle | `CycleService.CreateCycleAsync`, `CyclesController` | `CycleValidationTests` partial | date ordering, duplicate name/year, one research type per cycle, required master data, actor permission, boundary dates |
| UT06 updateCycleStatus | `CycleService.OpenCycleAsync`/`CloseCycleAsync`, `CyclesController` | cycle validation/deadline tests partial | valid transitions, repeated transition, missing cycle, deadline prerequisites, close/open authorization; richer Excel state machine requires decision |

## Requirements

- Use project exception conventions, not generic `BusinessException`: 400 `ArgumentException`, 409 `InvalidOperationException`, 403 `ForbiddenException`, 404 `KeyNotFoundException`, 401 only authentication.
- Verify both returned DTO and persisted entity; password tests must never assert raw password storage.
- Boundary cases must use explicit UTC values and `FakeClock` where time-dependent.
- Controller tests should validate claims/role policy, while service tests own business rules.

## Architecture

Arrange through `TestServices` and real InMemory repositories. Seed roles/users/master data with builders. For login, inject deterministic JWT service or assert service result without token cryptography where existing seams permit. For cycle transitions, model currently exposed operations first; do not invent an API for Excel-only statuses.

## Related Code Files

- Create focused tests under `FURPMS.Tests/Auth`, `Users`, and `Cycles` only where matrix shows gaps.
- Modify shared builders only if a missing fixture blocks multiple phases.
- Production files are read-only until UT06/UT02 decisions are approved.

## Implementation Steps

1. Match each UTCID to existing test methods and mark exact assertion gaps.
2. Add parameterized valid/invalid credential and validation datasets.
3. Add persistence and authorization assertions for create/update operations.
4. Add UTC boundary and duplicate-name/year cycle tests.
5. Compare UT06 expected transition table with actual `OpenCycleAsync`/`CloseCycleAsync` behavior.
6. Escalate DTO/state-machine mismatch; do not weaken tests to pass.

## Todo List

- [ ] Map 15 cases each for UT01–UT06.
- [ ] Cover 401/403/404/409/400 distinctions.
- [ ] Verify password hash changes and no secret leakage.
- [ ] Verify cycle type, date, duplicate-name, and permission rules.
- [ ] Record UT02 confirmation/policy and UT06 transition decisions.

## Success Criteria

- All implementable UT01–UT06 rows green and isolated.
- No assertion depends on exception text beyond stable user-facing category/content requirements.
- Blocked rows clearly identified with required production/spec change.

## Risks and Security

- Login tests can accidentally expose secrets in failure output; use synthetic credentials and sanitized assertions.
- Cycle status tests can encode obsolete rules; cite `docs/BUSINESS_RULES.md`/QĐ543 decision before changing behavior.

## Next Steps

Feed unresolved UT02/UT06 decisions into the phase-06 reconciliation register.
