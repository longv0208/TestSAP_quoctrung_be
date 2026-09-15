# Phase 1 — Harness, baseline, and traceability

## Context Links

- `demo unit test.xlsx`: sheets `UT01_login` through `UT15_submitFinalReport`, 15 cases per sheet.
- `FURPMS.Tests/FURPMS.Tests.csproj`
- `FURPMS.Tests/Helpers/TestDbContextFactory.cs`
- `FURPMS.Tests/Helpers/TestServices.cs`
- `FURPMS.Tests/Helpers/TestNotifier.cs`
- `FURPMS.Tests/Reminders/DeadlineReminderScannerTests.cs` (`FakeClock`)
- `docs/TEST_CHECKLIST.md`, `docs/BUSINESS_RULES.md`

## Overview

- Priority: P1
- Status: Pending
- Effort: 4h
- Establish a repeatable test-data/ID convention, inventory current coverage, and make the Excel-to-test relationship auditable.

## Key Insights

- Existing tests use actual repositories over EF Core InMemory, not Moq. Keep that convention.
- Test services already centralize construction, clock, notifications, and external-service doubles.
- Workbook “Passed 225” is not evidence: execution date and defect columns are empty.
- No .NET SDK is installed in the research environment; baseline must run after toolchain setup.

## Requirements

- Add a stable mapping convention, e.g. `[Trait("ExcelSheet", "UT08_submitProposal")]` and `[Trait("ExcelCase", "UTCID03")]`.
- One automated test may cover one case; parameterized tests are allowed only when setup/assertions remain unambiguous.
- Record existing test name/file, planned test name, target symbol, status (`covered`, `partial`, `new`, `blocked`), and notes.
- Assert observable behavior, persistence, side effects, and exception-to-HTTP mapping where controller tests are justified.

## Architecture

Use one shared fixture/data-builder layer in `FURPMS.Tests/Helpers` and keep domain workflow tests at service level. Use controller tests only for claims extraction, authorization attributes, request binding, and middleware-compatible status mapping. Generate unique InMemory database names per test; reset/seed explicitly.

## Related Code Files

- Create/modify tests only after approval: `FURPMS.Tests/Helpers/*`, focused test folders, and optional traceability artifact.
- Read-only production references: `FURPMS.Infrastructure/Services/*`, `FURPMS.API/Controllers/*`.
- No files deleted.

## Implementation Steps

1. Install/verify .NET 8 SDK; restore solution and run the existing suite unchanged.
2. Inventory all existing test classes and classify overlap against each `UTCIDxx`.
3. Define shared builders for users/roles, cycles, proposals, councils, rounds, reports, scores, and documents.
4. Add traits/naming convention and a case matrix kept beside the tests or generated from the workbook.
5. Mark cases that require production/spec clarification instead of writing speculative assertions.
6. Capture baseline pass/fail/skipped counts and coverage before additions.

## Todo List

- [ ] Verify `dotnet --info`, restore, and baseline test count.
- [ ] Build the 225-row traceability matrix.
- [ ] Tag or name tests with sheet and `UTCID`.
- [ ] Reuse `TestServices`, `FakeClock`, `TestNotifier`, and `TestBallots`.
- [ ] Define deterministic seed IDs and unique database lifecycle.
- [ ] Identify duplicate tests before adding new classes.

## Success Criteria

- Baseline output reproducible on a clean checkout.
- Every Excel case has a mapping row and an explicit status.
- No test sends email, calls Gemini, or depends on wall-clock time.

## Risk Assessment

- InMemory behavior differs from PostgreSQL for constraints/query translation. Keep targeted PostgreSQL smoke tests for provider-sensitive behavior.
- Broad fixture reuse can leak state. Prefer per-test context or explicit transaction-equivalent reset.

## Security Considerations

- Never commit real credentials, JWT secrets, uploaded personal data, or API keys.
- Test password hashes and claims with synthetic values only.

## Next Steps

Proceed to phases 2–5 after baseline; block only the cases listed in the decision register.
