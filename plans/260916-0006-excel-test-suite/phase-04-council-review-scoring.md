# Phase 4 — Councils, invitations, review, and scoring

## Context Links

- `FURPMS.Infrastructure/Services/CouncilService.cs`
- `FURPMS.Infrastructure/Services/ReviewScoringService.cs`
- `FURPMS.API/Controllers/CouncilsController.cs`
- `FURPMS.API/Controllers/ReviewerFeedbackController.cs`
- `FURPMS.API/Controllers/ReviewScoringController.cs`
- Existing tests: `FURPMS.Tests/Councils/CouncilServiceTests.cs`, `CouncilExpertiseTests.cs`, `CouncilMeetingConflictTests.cs`, `MeetingVisibilityTests.cs`, `FURPMS.Tests/ReviewRounds/ReviewRoundServiceTests.cs`, `ReviewBoardServiceTests.cs`, `ReviewScoringServiceMinutesTests.cs`, `FURPMS.Tests/Review/ScoreDivergenceTests.cs`, `CouncilMinutesTests.cs`, `ResearchOrderWinnerTests.cs`

## Overview

- Priority: P1
- Status: Pending
- Effort: 8h
- Cover UT10–UT13 (60 cases), prioritizing workflow/state/authorization paths and avoiding duplication of strong existing council/scoring tests.

## Key Insights and Mapping

| Sheet | Target | Existing coverage | Gap/blocker |
|---|---|---|---|
| UT10 assignCouncilMember | `CouncilService.AddMemberAsync`, `CouncilsController` | council expertise/conflict/service tests overlap | invalid role, chair uniqueness, COI, expertise, capacity, duplicate member, meeting/invite gate |
| UT11 respondInvitation | `CouncilService.RespondToMembershipAsync` | no dedicated named file identified | accept/decline, reason requirement, deadline expiry, duplicate response, wrong user/member, notifications, replacement flow |
| UT12 submitReview | no `ReviewService`; current `ReviewerFeedbackController` persists feedback | `CouncilMinutesTests`, `ReviewBoardServiceTests` partial but not Excel workflow | review ownership, round/proposal state, required fields, duplicate feedback, meeting attendance, lock/finalization; production ownership must be clarified |
| UT13 submitScore | `ReviewScoringService.SubmitScoreAsync` and minutes methods | `ReviewScoringServiceMinutesTests`, divergence/acceptance tests overlap | rubric total/criterion boundaries, reviewer role, one score per reviewer/round, edit window, quorum, chair decision and finalization |

## Requirements

- Apply current project rules: REVIEW council 3–5, ACCEPTANCE 5–7; review scores by all attending members, acceptance numeric score by reviewers and outcome vote by attendees; chair final decision, not automatic majority.
- Keep score scale/rubric configurable and assert total/criterion constraints from seeded rubric, not magic numbers in test logic.
- Test authorization separately from business-state validation. Verify duplicate feedback/score and finalized-minute immutability.
- Do not rename current service to satisfy Excel terminology without an approved production design.

## Architecture

Build council/meeting/round fixtures through existing helpers. Use real InMemory repositories for lifecycle and persistence. Use controller tests for current-user lookup and route authorization. For UT12, first create a decision record describing whether feedback is the intended “review submission” or a missing service/endpoint; only then write complete assertions.

## Related Code Files

- Extend focused tests under `FURPMS.Tests/Councils`, `Review`, and `ReviewRounds`; create `FURPMS.Tests/Invitations` only if needed.
- Read-only until decisions: `ReviewerFeedbackController.cs`, related DTO/interfaces, and round entities/services.
- If production workflow is approved later, update API contract/business rules together with tests.

## Implementation Steps

1. Map all 60 UTCIDs against existing test methods and identify duplicate assertions.
2. Build reusable council fixtures for valid size, expertise mismatch, COI, schedule conflict, and chair cases.
3. Add invitation response matrix including expiry and decline reason.
4. Reconcile UT12 expected “ReviewService” with actual feedback endpoint and state model.
5. Add score/rubric boundary tests, duplicate/edit/finalized cases, and persistence checks.
6. Verify minutes save/approve, quorum, chair decision, notifications, and proposal status transitions.

## Todo List

- [ ] Complete UT10 assignment matrix.
- [ ] Complete UT11 response matrix.
- [ ] Produce UT12 service/endpoint gap decision before tests.
- [ ] Complete UT13 rubric and lifecycle matrix.
- [ ] Assert COI and authorization paths with 400/403 distinctions.
- [ ] Reuse `TestBallots` realistic 100-point records.

## Success Criteria

- Existing review tests remain green; new cases cover only missing rows.
- No test claims majority auto-finalization contrary to current chair-decision rule.
- UT12 is either fully mapped to an approved implementation or explicitly blocked, never silently marked passed.

## Risks and Security

- Reviewer identity and proposal ownership are authorization-sensitive; always test current-user claims and COI.
- Do not include reviewer personal data in snapshots or failure messages.

## Next Steps

Provide accepted proposal/contract fixtures to phase 5; carry UT12 decision into phase 6.
