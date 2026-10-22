# Filter Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the reader's tools out of the page into a header button and a side panel, and show a tailored-view
note with a "full CV" link in the hero.

**Architecture:** The tools box (`Sections/Tools.razor`) becomes a `<dialog>`. It renders right after the header,
opens non-modal (`show()`) beside the content on wider screens, and opens modal (`showModal()`) as a bottom sheet on
phones. A header button opens it. A small new script module (`js/view.js`) holds the pure logic the page script needs
for the badge and the note, and is tested with Jint. `cv.js` keeps its `state`, `remember()` and `apply()`, and adds
opening, closing, copying, clearing, the badge and the note.

**Tech Stack:** .NET 10 Razor components prerendered by `HtmlRenderer`; plain CSS on the operandor design system
0.2.0; a vanilla ES module; xUnit v3, Shouldly, AngleSharp and Jint in the tests.

**Spec:** `docs/superpowers/specs/2026-10-23-filter-panel-design.md` (approved). Read it before any task.

## Global Constraints

- Work on a new branch `claude/filter-panel` from `main`. The local `claude/engine-0.2.0` is merged.
- Touch only `src/Sipos.Resume.Theme.Operandor`, `tests/Sipos.Resume.Theme.Operandor.Tests` and `docs/`. No change in
  Core, no new package.
- Progressive enhancement: everything new is closed or `hidden` in the markup, and only the script shows it.
- The four resource files must stay in step: `ThemeTextShould` fails when a satellite misses a key or a placeholder.
- Use the spec's Hungarian texts verbatim: "Szűrés", "Szűrés és nézet", "Bezárás", "Szűrők törlése", "Szabott
  nézet:", "egy oldal", "{0} kiemelve", "{0} aktív beállítás". The "full CV" link reuses `FocusAll` ("Teljes
  önéletrajz").
- Note format: `<lead> <parts joined by " · ">`. The parts come in this order: focus label, one page, technology.
  Example: "Szabott nézet: Szoftverarchitekt · egy oldal · .NET kiemelve".
- Breakpoints:
  - phones: `max-width:640px`, the theme's existing breakpoint;
  - content moves aside: `min-width:1360px` (920 px column, 360 px panel and 40 px padding on each side);
  - the panel is 360 px wide.
- Ids the theme adds start with `cv-`, because `Anchors.IsReserved` keeps that prefix for themes.
- House style:
  - test names follow `VerbWhatGivenCondition`, with a one-line comment above a test when its reason isn't obvious;
  - a choice with alternatives gets a `Decision:` / `Why:` / `Considered:` comment, as in the existing files;
  - commit subjects are imperative sentences with no prefix (for example "Move the reader's tools into a header
    panel") and end with the `Co-Authored-By` line.
- Tests run without a restore on this machine:
  - `dotnet test tests/Sipos.Resume.Theme.Operandor.Tests --no-restore`;
  - a restore needs the GitHub Packages token (README, "Developing");
  - the baseline is 87 tests passing.

## Review Focus

1. **Screens 360 px wide or narrower.** The design system hides `.op-header__right .op-btn` at ≤360 px and hides an
   `op-btn`'s icon at ≤400 px. The toggle must therefore not be an `op-btn`, or it disappears or goes blank on small
   phones. Pinned by `KeepsFilterToggleOutOfDesignSystemButtonsGivenHeader` (Task 2).
2. **A stale or hand-edited link** (`?focus=gone&tech=gone`). The script already ignores unknown ids, and the note
   must show nothing rather than "undefined". Pinned by `ShowsNothingGivenUnknownIds` (Task 1).
3. **A CV without focus profiles or a one-page view** (a plain JSON Resume). The panel has no focus or view field,
   and the note can still name a technology. Pinned by `NamesTechnologyAloneGivenNoProfile` (Task 1) and
   `LeavesOutFocusAndViewInPanelGivenPlainResume` (Task 2).
4. **Resizing across 640 px while the panel is open.** It must close, not leave a modal sheet on a wide screen or a
   non-modal panel over a phone page. Checked by hand in Task 3, step 4.
5. **A tailored link opened fresh.** The note, the badge and the tailored PDF are right on first paint, and the panel
   is closed. Pinned by `NamesProfileShortViewAndTechnologyGivenTailoredLink` (Task 1), and checked by hand in Task 4.

---

### Task 1: The note's text and the badge count, with their words

**Files:**
- Create: `src/Sipos.Resume.Theme.Operandor/wwwroot/js/view.js`
- Modify: `src/Sipos.Resume.Theme.Operandor/wwwroot/js/cv.js:6-10`: import `format` from `view.js` with the same `?v=`
  query as `text.js`, and delete the local `format`.
- Modify: `src/Sipos.Resume.Theme.Operandor/OperandorTheme.cs:44`: add `AssetRoot + "/js/view.js"` after `text.js`
  to `RequiredAssets`. The asset version and the output verifier read this list.
- Modify: `src/Sipos.Resume.Theme.Operandor/Localization/ThemeText.resx`, `.hu.resx`, `.hr.resx`, `.sr-Latn.resx`
- Modify: `src/Sipos.Resume.Theme.Operandor/Rendering/ResumeData.cs:153-154` (`ScriptKeys`)
- Create: `tests/Sipos.Resume.Theme.Operandor.Tests/Scripts/ViewScript.cs`
- Modify: `tests/Sipos.Resume.Theme.Operandor.Tests/Rendering/ResumeDataJson.cs`

The test project already copies every `wwwroot/js/*.js` to `Scripts/`.

**Interfaces:**
- Produces, in `view.js`:
  - `export const format = (template, ...values) => string`: moved from `cv.js`, unchanged.
  - `export const activeCount = (state) => number`: counts `state.focus !== ""`, `state.technology !== ""` and
    `state.view === "short"`. The `state` shape is `cv.js`'s: `{ focus, technology, view, query }`.
  - `export const noteText = (state, data, words) => string`:
    - `data.focus` is `[{ id, label }]` and `data.technologies` is `[{ key, name }]`, as in `cv-data`;
    - `words` holds `ViewNoteLead`, `ViewNoteShort` and `ViewNoteTechnology`;
    - it returns `""` when no known part is active.
- Produces script keys: `ActiveSettings`, `ViewNoteLead`, `ViewNoteShort`, `ViewNoteTechnology`.
- Produces page keys: `FilterButton`, `PanelTitle`, `Close`, `ClearFilters`.

- [ ] **Step 1: Write the failing Jint tests in `ViewScript.cs`**

Load the module the way `TextScriptParity` does (`engine.Modules.Add("view", …Scripts/view.js)`, then `Import`). Pass
objects with `JSON.parse`. The English words are `{"ViewNoteLead":"Tailored view:","ViewNoteShort":"one
page","ViewNoteTechnology":"{0} highlighted"}`. The data is `{"focus":[{"id":"architect","label":"Software
architect"}],"technologies":[{"key":"azure","name":"Azure"}]}`.

```csharp
[Fact] CountsOnlyLinkSettingsGivenState
// activeCount({focus:"architect",technology:"azure",view:"short",query:"x"}) == 3
// activeCount({focus:"",technology:"",view:"full",query:"x"}) == 0
[Fact] NamesProfileShortViewAndTechnologyGivenTailoredLink
// noteText({focus:"architect",technology:"azure",view:"short"}, data, en)
//   == "Tailored view: Software architect · one page · Azure highlighted"
[Fact] KeepsSpecWordingGivenHungarianWords
// data with label "Szoftverarchitekt" and technology name ".NET"; words "Szabott nézet:", "egy oldal", "{0} kiemelve"
//   == "Szabott nézet: Szoftverarchitekt · egy oldal · .NET kiemelve"
[Fact] NamesTechnologyAloneGivenNoProfile
// noteText({focus:"",technology:"azure",view:"full"}, {focus:[],technologies:[…azure…]}, en) == "Tailored view: Azure highlighted"
[Fact] ShowsNothingGivenFullCv
// noteText({focus:"",technology:"",view:"full"}, data, en) == ""
[Fact] ShowsNothingGivenUnknownIds
// noteText({focus:"gone",technology:"gone",view:"full"}, data, en) == ""
```

- [ ] **Step 2: Run them and see them fail**

Run: `dotnet test tests/Sipos.Resume.Theme.Operandor.Tests --no-restore --filter "FullyQualifiedName~ViewScript"`
Expected: FAIL, because `Scripts/view.js` is not found.

- [ ] **Step 3: Write `view.js`**

Open the file with a short header comment in `text.js`'s style: pure helpers of the page script, tested with Jint.
Move `format` there, change `cv.js`'s first lines to import it, and add `view.js` to `RequiredAssets`.

- [ ] **Step 4: Run them and see them pass**

Run the same command. Expected: 6 passed.

- [ ] **Step 5: Add the words, and a failing assertion in `ResumeDataJson.CarriesWhatScriptAppliesGivenPage`**

Add the assertion first:
`json.GetProperty("text").GetProperty("ViewNoteLead").GetString().ShouldBe("[ViewNoteLead]");`

Run it and see it fail. Then add the four script keys to `ScriptKeys`, and the eight keys below to the four resource
files:

| Key | en (neutral) | hu | hr | sr-Latn |
|---|---|---|---|---|
| `FilterButton` | Filter | Szűrés | Filtriranje | Filtriranje |
| `PanelTitle` | Filter and view | Szűrés és nézet | Filtriranje i prikaz | Filtriranje i prikaz |
| `Close` | Close | Bezárás | Zatvori | Zatvori |
| `ClearFilters` | Clear filters | Szűrők törlése | Poništi filtre | Poništi filtere |
| `ActiveSettings` | Active settings: {0} | {0} aktív beállítás | Aktivne postavke: {0} | Aktivna podešavanja: {0} |
| `ViewNoteLead` | Tailored view: | Szabott nézet: | Prilagođeni prikaz: | Prilagođeni prikaz: |
| `ViewNoteShort` | one page | egy oldal | jedna stranica | jedna strana |
| `ViewNoteTechnology` | {0} highlighted | {0} kiemelve | istaknuto: {0} | istaknuto: {0} |

- [ ] **Step 6: Run the whole theme suite**

Run: `dotnet test tests/Sipos.Resume.Theme.Operandor.Tests --no-restore`
Expected: all pass, including `ThemeTextShould`, at 93 tests.

- [ ] **Step 7: Commit**

```bash
git add src/Sipos.Resume.Theme.Operandor tests/Sipos.Resume.Theme.Operandor.Tests
git commit -m "Compose the tailored view's note and count its settings in a tested script module"
```

---

### Task 2: Markup: the header toggle, the panel and the hero note

**Files:**
- Modify: `src/Sipos.Resume.Theme.Operandor/Pages/ResumePage.razor`:
  - put the toggle in `<Actions>` before the PDF `OpButton`;
  - move `<Tools />` out of the card band to right after `</SiteHeader>`, before `<main>`;
  - drop the band's `Style="padding-top:24px"`, which only made room for the box.
- Modify: `src/Sipos.Resume.Theme.Operandor/Sections/Tools.razor` (it becomes the panel)
- Modify: `src/Sipos.Resume.Theme.Operandor/Sections/Hero.razor:42`: the note replaces the `cv-hero__focus` paragraph
  and goes right after the job title (after the name when there's no title).
- Modify: the four `ThemeText*.resx`: remove `Tools`, which is no longer used.
- Test: `tests/Sipos.Resume.Theme.Operandor.Tests/Pages/ResumePageRender.cs`

**Interfaces (the selectors Task 3 and Task 4 rely on):**
- **Toggle:**
  - `button.cv-filter-toggle[data-cv-filter-toggle][aria-controls=cv-panel][aria-expanded=false][hidden]`, not
    `op-btn`;
  - children in this order:
    1. the sliders glyph;
    2. `span.cv-filter-toggle__label` with `FilterButton`;
    3. `span.cv-filter-toggle__count[data-cv-filter-count][aria-hidden=true][hidden]`;
    4. `span.op-visually-hidden[data-cv-filter-count-text]`.
- **Panel:**
  - `dialog#cv-panel.cv-panel[data-cv-tools][aria-labelledby=cv-panel-title]`, with no `open` attribute;
  - one child, `div.cv-panel__inner`, containing in order:
    1. a head: `h2#cv-panel-title.cv-panel__title` (`PanelTitle`) and
       `button.op-iconbtn.op-iconbtn--sm[data-cv-panel-close][aria-label=Close]` with `OpIcon Name="x"`;
    2. `div.cv-panel__fields`: the existing search, focus, technology and view fields, with their ids unchanged;
    3. `#cv-search-results` and `#cv-search-status`, unchanged;
    4. `div.cv-panel__actions`: `OpButton Variant="secondary" Size="sm" data-cv-copy-view hidden` (`CopyLink`), then
       the existing print `OpButton`;
    5. the existing `details.cv-match`, unchanged;
    6. `button.cv-panel__clear[data-cv-clear]` (`ClearFilters`).
- **Note:**
  - `p.cv-view-note[data-cv-view-note][hidden]`, containing:
    1. the target glyph;
    2. `span[data-cv-view-note-text]`, empty;
    3. `a.cv-view-note__full[data-cv-full]`: its `href` is `ResumeView.Href(View.Page.Path)` and its text is
       `FocusAll`, followed by `OpIcon Name="arrow-right" Size="16"`.
- **Glyphs:** inline SVGs drawn for the theme, because the design system 0.2.0 has no sliders or target icon.
  - No third-party source, so no licence notice is needed.
  - Each is `class="cv-glyph" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor"
    stroke-width="2" stroke-linecap="round" aria-hidden="true" focusable="false"`.
  - Sliders: `<path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12"
    r="2"/><circle cx="18" cy="18" r="2"/>`.
  - Target: `<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1"/>`.
  - Put them in `Sections/CvGlyph.razor` with a `Name` parameter (`"sliders"`, `"target"`).

- [ ] **Step 1: Write the failing render tests**

Rewrite `HidesToolsUntilScriptGivenPage` (same name) to assert:
- the toggle has `hidden`;
- the panel is a `DIALOG` without `open`;
- the note has `hidden`;
- every `[data-cv-focus-download]` has `hidden` (as before);
- the copy button has `hidden`.

Add:

```csharp
// The panel opens from the header; it is no longer a box between the hero and the sections.
[Fact] OpensToolsFromHeaderGivenPage
// toggle is inside ".op-header__right", before the PDF link (DocumentPosition / index among ".op-header__right" children)
// toggle aria-controls == "cv-panel" and document.GetElementById("cv-panel") is the [data-cv-tools] dialog
// document.QuerySelector("main [data-cv-tools]") is null
// The design system hides header .op-btn at ≤360 px and their icons at ≤400 px.
[Fact] KeepsFilterToggleOutOfDesignSystemButtonsGivenHeader
// toggle.ClassList.Contains("op-btn") is false
[Fact] LeadsNoteToPlainPageGivenHungarianPage
// render index 1; note is the next element sibling of ".cv-hero__title"; a[data-cv-full] href == "/hu/"; its text contains the hu FocusAll
// document.QuerySelector("[data-cv-focus-note]") is null
[Fact] HoldsEveryToolInPanelGivenPage
// within #cv-panel: #cv-search, #cv-focus, #cv-technology, [data-cv-view], [data-cv-print], [data-cv-copy-view],
// details.cv-match, [data-cv-clear], [data-cv-panel-close]; on the English page (index 0, real resources)
// #cv-panel-title text == "Filter and view"
[Fact] LeavesOutFocusAndViewInPanelGivenPlainResume
// change: OffersOnePageViewOnlyGivenMarkedItems's lambda (IsShort = false on strengths, positions, engagements and
// projects) plus FocusProfiles = []; #cv-focus and [data-cv-view] are absent, the toggle and #cv-technology are present
```

- [ ] **Step 2: Run them and see them fail**

Run: `dotnet test tests/Sipos.Resume.Theme.Operandor.Tests --no-restore --filter "FullyQualifiedName~ResumePageRender"`
Expected: the new and the rewritten tests FAIL.

- [ ] **Step 3: Change the markup to the interfaces above, and remove `Tools` from the four resource files**

- Keep `Tools.razor`'s header comment true: it is now the panel, closed in the markup, which the script opens.
- Give the toggle in `ResumePage.razor` a `Decision:` comment: a plain button, not `OpButton`, because of the
  design system's ≤360/≤400 px header rules.

- [ ] **Step 4: Run the theme suite**

Run: `dotnet test tests/Sipos.Resume.Theme.Operandor.Tests --no-restore`
Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add src/Sipos.Resume.Theme.Operandor tests/Sipos.Resume.Theme.Operandor.Tests
git commit -m "Move the reader's tools into a header panel and add the tailored view's note to the hero"
```

---

### Task 3: Behavior in `cv.js`

**Files:**
- Modify: `src/Sipos.Resume.Theme.Operandor/wwwroot/js/cv.js` (the block from `const tools = …`, line 98, to the end)

**Interfaces:**
- Consumes: `activeCount`, `noteText` and `format` from `view.js` (Task 1), and the selectors from Task 2.
- Produces: the class `cv-panel-open` on `document.documentElement` while the panel is open, which Task 4's CSS
  reads.

What to change:

- **Showing the controls.** Show the toggle (`hidden = false`) instead of the tools. The dialog stays closed.
- **`openPanel()`.**
  - Calls `panel.showModal()` when `matchMedia("(max-width:640px)")` matches, and `panel.show()` otherwise. Both
    move focus into the dialog by themselves (to its first focusable element, the close button), which is the
    spec's "opening moves keyboard focus into the panel".
  - Sets `aria-expanded="true"` and adds `cv-panel-open`.
  - The toggle opens and closes the panel.
- **Closing.**
  - One `close` event listener resets `aria-expanded`, removes `cv-panel-open` and focuses the toggle. Every way of
    closing ends there.
  - `data-cv-panel-close` closes the panel.
  - Escape inside the non-modal panel closes it. A modal sheet closes on Escape by itself (`cancel`).
  - A click whose target is the dialog itself (the backdrop) closes it.
  - A `change` on the 640 px media query closes an open panel.
  - A click on a search result link (`#cv-search-results a`) closes the panel when it is modal (`panel.matches(":modal")`).
- **In `apply()`.**
  - The badge: `data-cv-filter-count` gets the count and is `hidden` at 0.
  - `data-cv-filter-count-text` gets `format(words.ActiveSettings, n)`, or `""` at 0.
  - The note: `data-cv-view-note-text` gets `noteText(state, data, words)`, and the note is `hidden` when that text is
    empty.
  - Remove the old `focusNote` code from `applyFocus()`.
- **Copy link.** Show `data-cv-copy-view` only when `navigator.clipboard` exists. A click writes `location.href` and
  `announce(words.LinkCopied)`.
- **Matcher.** Extract its result rendering into `const showMatch = (terms) => …`, then call it from the match button
  and with `[]` from clear.
- **Clear.**
  - Empty `state.focus`, `state.technology` and `state.query`, and set `state.view = "full"`.
  - Reset the search, focus and technology controls and the matcher's text.
  - Call `showMatch([])`, then `remember()` and `apply()`.

- [ ] **Step 1: Make the changes above**

- [ ] **Step 2: Run the theme suite**

Run: `dotnet test tests/Sipos.Resume.Theme.Operandor.Tests --no-restore`
Expected: all pass. The Jint tests cover `view.js`. No browser tests exist, so steps 3 and 4 check `cv.js` by hand.

- [ ] **Step 3: Build a site with the real CV and serve it**

```bash
dotnet run --project samples/SampleSite -c Release -- --content ../sipos-andor.GitHub.io/content --output artifacts/check-site --clean
python -m http.server 8080 -d artifacts/check-site
```

- If the sample host rejects the real content, use `samples/plain-json-resume/content` and check only what doesn't
  need a focus or a one-page view.
- The real CV's focus ids are `architect`, `tech-lead`, `ai-assisted` and `senior-dotnet`.
- The panel is unstyled until Task 4, so check behavior only.

- [ ] **Step 4: Check the behavior in a browser**

- At 1440×900:
  - the toggle opens and closes the panel;
  - on opening, keyboard focus is inside the panel;
  - Escape closes it;
  - focus returns to the toggle;
  - a focus choice updates the badge (1), the note and the address;
  - copy link announces "Link másolva";
  - clear empties everything and the address.
- At 375×812:
  - the panel opens modal;
  - a tap outside it closes it;
  - a search hit closes it and lands on the item.
- With the panel open, resize across 640 px: it closes.
- Open `/hu/?focus=architect&view=short` fresh:
  - the panel is closed;
  - the badge shows 2;
  - the note reads "Szabott nézet: Szoftverarchitekt · egy oldal";
  - the tailored PDF button shows.
- Click "Teljes önéletrajz →": the page is `/hu/` with no note.

- [ ] **Step 5: Commit**

```bash
git add src/Sipos.Resume.Theme.Operandor/wwwroot/js/cv.js
git commit -m "Open the tools from the header and show the tailored view in the hero"
```

---

### Task 4: Styles in `cv.css`

**Files:**
- Modify: `src/Sipos.Resume.Theme.Operandor/wwwroot/css/cv.css`:
  - "Tools" block, lines 46-70: becomes "Filter panel";
  - line 31: remove `.cv-hero__focus`;
  - lines 199-203: the 640 px `.cv-tools__row` rules give way to the panel's phone rules;
  - print, line 231.

**Interfaces:**
- Consumes: the selectors from Task 2 and `html.cv-panel-open` from Task 3.

The values the spec and the mockup fix (everything else is the builder's):

- **Toggle.**
  - 30 px high (the header's `op-btn` height at ≤640 px), 1 px `--border-default` border, `--radius-2` corners,
    transparent background, `--text-heading` color.
  - Hover uses `--surface-hover`. The focus ring is copied from `.op-iconbtn:focus-visible`.
  - At ≤640 px the label is visually hidden (repeat `.op-visually-hidden`'s declarations), so only the glyph and the
    badge show.
- **Badge:** an `--accent` fill with `--text-on-accent` text, 18 px minimum width and height, round.
- **Panel.**
  - `.cv-panel[open]`:
    - `position:fixed`, `top:73px` (72 px header plus its border), `right:0`, `bottom:0`;
    - 360 px wide, `margin:0`, `padding:0`, own scroll;
    - `--surface-card` background, a left `--border-subtle` border;
    - a `z-index` below the header's 30.
  - Style `[open]` only, so a closed dialog keeps the browser's `display:none`.
  - `.cv-panel__inner` has the padding and fills the dialog, so a click inside never reaches the dialog itself.
  - The fields are stacked, and selects are full width (`max-width:none`).
- **Wide screens:** `@media (min-width:1360px)` with `html.cv-panel-open` makes room for 360 px on the right of
  `main` and the footer. How is the builder's call: a margin or padding on `.cv-main` and `.op-footer`.
- **Phones:** in `@media (max-width:640px)`, `.cv-panel[open]` is:
  - `top:auto`, `left:0`, `right:0`, full width, `height:75dvh` (with a `vh` fallback);
  - a top border instead of the left one;
  - top corners `--radius-4`.

  Its `::backdrop` uses `var(--scrim, rgba(20,24,28,.44))`, as `.op-dialog::backdrop` does.
- **Note.**
  - flex, wrapping, `gap: 4px 10px`, 16 px top margin, `10px 12px` padding;
  - 8 px corners, not a pill;
  - a 1 px border of `color-mix(in srgb, var(--accent) 45%, transparent)` on a `--surface-accent-tint` background;
  - text in `--text-heading` at `--fs-body-sm`;
  - the glyph in `--accent`;
  - the link in `--text-link` at `--fw-medium`;
  - at ≤640 px the link takes `flex-basis:100%`.
- **Print.**
  - In the hide list, `.cv-tools` becomes `.cv-panel`, and `.cv-hero__focus[hidden]` becomes
    `.cv-view-note[hidden],.cv-view-note__full`. The header is already hidden, and the toggle with it.
  - The note prints as plain text: no border, background or padding.

- [ ] **Step 1: Make the changes above**

- [ ] **Step 2: Rebuild the check site and check the layout**

Rebuild with Task 3's command.

- 1440×900, light and dark:
  - the open panel sits under the header;
  - the 920 px column moves aside and isn't covered;
  - the note looks like the mockup.
- 1024×768: the panel covers the right of the content, with no backdrop.
- 375×812:
  - a bottom sheet over a backdrop;
  - the toggle shows the glyph and the badge;
  - the note's link is on its own line.
- 360×740: the PDF button is gone (the design system's rule), and the toggle is still there.
- Print preview of `/hu/?focus=architect`: the note's text prints, and neither its link, the toggle nor the panel
  does.

- [ ] **Step 3: Run the whole solution's tests**

Run: `dotnet test Sipos.Resume.slnx --no-restore`
Expected: all pass.

- [ ] **Step 4: Commit**

```bash
git add src/Sipos.Resume.Theme.Operandor/wwwroot/css/cv.css
git commit -m "Style the filter panel, its header button and the tailored view's note"
```

---

### Task 5: Pull request

- [ ] **Step 1: Push the branch and open a pull request to `main`**

The pull request description:
- what changed and why, in a few lines from the spec's intent;
- the spec and plan paths;
- the manual checks from Tasks 3 and 4, with their results.

It ends with the generated-with line.

- [ ] **Step 2: Stop here**

Tagging `v0.3.0`, publishing, and moving the site repo to the new version each need the owner's explicit go-ahead
(spec, "Release").
