# Phase 6 — Execute, coverage, and Excel reconciliation

## Context Links

- `FURPMS.Tests/FURPMS.Tests.csproj`
- `demo unit test.xlsx`
- `docs/TEST_CHECKLIST.md`
- `docs/PROGRESS.md`
- `docs/BUSINESS_RULES.md`

## Overview

- Priority: P1
- Status: Pending
- Effort: 4h
- Run the completed suite, classify failures, measure coverage, and produce an evidence-backed Excel reconciliation.

## Key Insights

- Environment currently has no .NET SDK, so test execution is a prerequisite, not an assumed result.
- Workbook cached statistics must not be overwritten as if they were execution evidence.
- A failing test can indicate code defect, stale Excel expectation, fixture defect, provider difference, or unresolved business decision; classify before fixing.

## Requirements

- Run restore/build/test, then coverage via coverlet collector or supported report tool.
- Capture test count, pass/fail/skip, duration, and coverage by target service/controller.
- Reconcile each UTCID with actual result: Passed, Failed, Blocked, or Not implemented; include test name and evidence.
- Preserve original Excel case text and classifications; add execution date and defect/decision references only from real runs.
- Do not change production/test code merely to make the workbook green.

## Architecture

Use CI/local command output as source of truth. Keep a machine-readable test naming/trait convention so a case can be filtered, e.g. `dotnet test --filter "ExcelSheet=UT08_submitProposal&ExcelCase=UTCID03"`. If the workbook is edited, preserve its formatting/formulas and recalculate/verify formula errors.

## Related Code Files

- Test project and test files from phases 1–5.
- Optional output artifact under the plan directory; do not modify production docs unless implementation changes require the documented sync rules.
- No production files deleted.

## Implementation Steps

1. Install .NET 8 SDK and restore packages.
2. Run baseline and full suite; rerun failures in isolation.
3. Generate coverage and map untested target methods/branches.
4. Classify failures and route code defects/spec mismatches/blocked decisions.
5. Re-run after approved fixes; record final evidence.
6. Update workbook execution columns and summary only when actual results exist.

## Todo List

- [ ] Confirm SDK version and clean build.
- [ ] Run all 225 case IDs plus regression suite.
- [ ] Produce service/controller coverage breakdown.
- [ ] Resolve or explicitly list all failures and blocked cases.
- [ ] Reconcile Normal/Abnormal/Boundary counts with outcomes.
- [ ] Verify no formula errors or fabricated “passed” status.

## Success Criteria

- Reproducible green result for all approved implementable cases, or a transparent failure/blocker register.
- Coverage gaps tied to target symbols, not only aggregate percentage.
- Excel report distinguishes designed cases from executed evidence.

## Risk Assessment

- CI may lack SDK/packages or differ in provider behavior; pin SDK/package versions and document environment.
- Parallel tests sharing InMemory names can contaminate state; enforce unique names.
- Coverage can be inflated by trivial controller lines; prioritize business-rule branch coverage.

## Security Considerations

- Sanitize logs and test artifacts; exclude secrets and real personal information.
- Do not upload workbook/test artifacts containing credentials to public repositories.

## Next Steps

After completion, update relevant project progress/checklist documentation through the normal docs-management workflow and obtain approval before any production behavior changes.
