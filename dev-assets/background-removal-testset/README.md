# Background removal test set V0.1

Research fixtures for Background Remover V0.2-P1; not bundled with the application and never downloaded by `dotnet build`.

`testset.json` is the single fixture authority: stable case IDs, original source page and download URL, author, selected free license, original-file SHA256, dimensions and inspection focus. Only this README, the manifest, preparation script and results template are Git assets. `images/` is ignored.

Current set: **16 images (15 photographs + 1 illustration)**, covering the 16 requested categories using `category` plus `additionalCategories`. Original files are retained for 13 cases. Three 1920px official renditions are explicitly identified after Wikimedia's original-file endpoint requested thumbnail use. Stored hashes always describe the actual downloaded rendition, not an unacquired original. The performance high-resolution role uses the **6000×4000** low-contrast original; the fine-hair original is **4912×7360**.

Visual source review found that the necklace acquired under ID `product-white-background` actually has a gray gradient; its category is corrected to hard-edge product. White-background coverage comes from the Nikon camera (`product-hard-edge`). Acquisition IDs remain fixed to preserve already recorded tensor/log provenance; do not infer category or quality from an ID alone.

## Acquire or verify

From the repository root:

```powershell
.\dev-assets\background-removal-testset\prepare-testset.ps1
.\dev-assets\background-removal-testset\prepare-testset.ps1 -VerifyOnly
```

The script uses ordinary HTTPS, checks every SHA256, skips correct existing images, and stops on failed downloads or mismatches. It never overwrites an existing mismatched image, retries a rate-limit response automatically, changes license metadata or downloads models. Failed partial downloads remain in ignored `images/` for inspection. Retry only after investigating the reported failure and respecting the source's rate limit.

## License and review rules

Sources are Wikimedia Commons file pages, not generic search-result images. Attribution and selected license are recorded per image; preserve those credits and license links when sharing originals or derived comparisons. CC BY-SA derivative works retain the applicable ShareAlike obligations. Public-domain entries identify their federal-government provenance. These licenses concern copyright; they do not grant endorsement, trademark or personality rights.

One anime illustration is intentionally included alongside photographs. Transparent/low-contrast images are hard cases, not promised supported quality. Existing alpha, if any, is recorded rather than silently flattened into a claimed raw-camera photograph. No private user images or invented ground-truth masks are included.

## Fixed Performance Set

Use the manifest's `performanceSet` in its listed order for every candidate: ordinary portrait, fine hair, animal fur, hard-edge product, complex background and high resolution. Do not substitute a convenient image for a slow/failing candidate. A safety stop leaves the remaining cases **NOT RUN**, not successful or timed at zero.

Synthetic checks are a separate exact-input diagnostic, not members of this real-image set. Session initialization, inference-only timings, preprocessing/postprocessing, CPU/GPU runtime versions and resource sampling must be described separately. A warm median over differing images is a mixed-image measurement, not repeated latency of one image.

## Quality outputs

Local outputs belong in `artifacts/background-v02-p1/results/<candidate>/`: transparent PNGs plus `dark-background-preview/` and `white-background-preview/`. Per-case side-by-side sheets are under `artifacts/background-v02-p1/quality-comparison/`. A missing candidate output must be visibly identified; never replace it with the original or another model's output.

Use `results-template.md` for optional human scores. Subject Integrity, Fine Detail, Background Residue and Edge Halo are each 1–5 (5 = best). Pay special attention to hair, shoulders, clothing and original-background color contamination on both dark and white composites. Numeric scores remain unfilled until supplied by a person; successful inference is not quality acceptance.

On 2026-09-03 the owner supplied **qualitative acceptance: PASSED** for B Static BiRefNet relative to A after reviewing ordinary portrait, fine hair, hard-edge product, backlight hair, transparent hard case and thin structure. B is the Production Approved Candidate (KEEP MODEL / CHANGE EXPORT). No 1–5 scores were supplied. This accepts the export replacement, not universal matting quality or the pending V0.2 production integration/UI. BEN2 missing cases remain NOT RUN.
