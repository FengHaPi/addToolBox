# Background removal quality review

Status: **Owner visual acceptance = PASSED for B relative to A (2026-09-03)**. Qualitative owner acceptance only; numeric scores were not supplied and remain unfilled. No automated score is a human acceptance result.

B Static BiRefNet is the **Production Approved Candidate**: ordinary portrait, fine hair and hard-edge product passed; backlight hair, transparent hard case and thin structure showed no obvious additional degradation relative to A sufficient to reject replacement. Direction: **KEEP MODEL / CHANGE EXPORT**. This does not promise perfect transparent objects/hair/thin structures or accept the pending V0.2 integration/UI. The blank per-case numerical table below is retained for optional future scoring, not an outstanding prerequisite to this qualitative decision. BEN2 missing outputs remain NOT RUN and are not approved.

Reviewer: __________  Date: __________  Model hashes / run labels: __________

Each score is 1–5, **5 = best**: intact intended subjects; retained fine detail; little background residue; little edge halo. Inspect both dark-gray and white previews at native resolution, especially hair, fur, shoulders, clothing and original-background color spill. Leave unavailable outputs unscored and mark NOT RUN. Do not score NO OUTPUT placeholders as image results.

| Case | Candidate | Subject Integrity | Fine Detail | Background Residue | Edge Halo | Notes / acceptance |
| --- | --- | --- | --- | --- | --- | --- |
| portrait-normal | A Baseline | — | — | — | — | PENDING USER REVIEW |
| portrait-normal | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| portrait-normal | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| portrait-short-hair | A Baseline | — | — | — | — | PENDING USER REVIEW |
| portrait-short-hair | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| portrait-short-hair | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| portrait-long-hair | A Baseline | — | — | — | — | PENDING USER REVIEW |
| portrait-long-hair | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| portrait-long-hair | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| hair-fine | A Baseline | — | — | — | — | PENDING USER REVIEW |
| hair-fine | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| hair-fine | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| backlight-hair | A Baseline | — | — | — | — | PENDING USER REVIEW |
| backlight-hair | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| backlight-hair | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| animal-fur | A Baseline | — | — | — | — | PENDING USER REVIEW |
| animal-fur | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| animal-fur | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| product-hard-edge | A Baseline | — | — | — | — | PENDING USER REVIEW |
| product-hard-edge | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| product-hard-edge | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| product-white-background | A Baseline | — | — | — | — | PENDING USER REVIEW |
| product-white-background | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| product-white-background | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| similar-color-background | A Baseline | — | — | — | — | PENDING USER REVIEW |
| similar-color-background | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| similar-color-background | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| thin-structure | A Baseline | — | — | — | — | PENDING USER REVIEW |
| thin-structure | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| thin-structure | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| multiple-subjects | A Baseline | — | — | — | — | PENDING USER REVIEW |
| multiple-subjects | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| multiple-subjects | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| complex-background | A Baseline | — | — | — | — | PENDING USER REVIEW |
| complex-background | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| complex-background | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| illustration-anime | A Baseline | — | — | — | — | PENDING USER REVIEW |
| illustration-anime | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| illustration-anime | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| low-contrast | A Baseline | — | — | — | — | PENDING USER REVIEW |
| low-contrast | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| low-contrast | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| transparent-hard-case | A Baseline | — | — | — | — | PENDING USER REVIEW |
| transparent-hard-case | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| transparent-hard-case | C BEN2 | — | — | — | — | PENDING USER REVIEW |
| high-resolution | A Baseline | — | — | — | — | PENDING USER REVIEW |
| high-resolution | B Static BiRefNet | — | — | — | — | PENDING USER REVIEW |
| high-resolution | C BEN2 | — | — | — | — | PENDING USER REVIEW |

## Decision

- A/B ordinary portrait, fine hair and product checked: __________
- Any structural subject change or detail regression in B: __________
- BEN2 detail / halo compared with A/B (only if output exists): __________
- Remaining unreviewed / unavailable cases: __________
- Owner-approved export replacement: B Static BiRefNet; qualitative PASSED on 2026-09-03. Production integration remains subject to separate runtime, regression and UI gates.
