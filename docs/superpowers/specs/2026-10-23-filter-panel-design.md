# Filter panel: the reader's tools leave the page for a header button and a side panel

- Status: accepted
- Date: 2026-10-23
- Theme: `Sipos.Resume.Theme.Operandor`

## Intent and why

The reader's tools (`Sections/Tools.razor`: search, focus, technology, one-page view, print and the job ad matcher)
sit in a bordered box between the hero and the first section. Measured on andor.sipos.io (Hungarian page):

| Viewport | Box starts at | Box height | Page height |
|---|---|---|---|
| 1440×900 | 821 px, the bottom edge of the first screen | 266 px | ~13 500 px |
| 375×812 | 1605 px | 480 px, more than half a screen | ~23 000 px |

So the first thing after the hero is a settings box rather than the CV, and once the reader has scrolled past it the
tools are out of reach.

Who uses the tools, as the CV's owner confirmed:

- **The owner** sets a focus, a technology or the one-page view, copies the address (`?focus=…&tech=…&view=short`)
  and sends it to a recruiter. The owner also uses the job ad matcher, before applying, to see how well an ad fits.
- **Recruiters** rarely filter. Many of them arrive on a tailored link.

So the tools should leave the reading path but stay reachable from anywhere. A recruiter who opens a tailored link
must see at once that the view is tailored, and must be able to reach the full CV in one click.

## Goals

1. The tools box is gone from between the hero and the sections.
2. A **filter button** in the header opens a **panel** that holds every tool.
3. A **tailored-view note** in the hero says what the link has set, with a **"full CV"** link that removes it.
4. Two new actions in the panel: **copy link** (the current view's address) and **clear filters**.
5. Progressive enhancement holds: without the script there is no button, panel or note, and the whole CV reads as
   today.

## Non-goals

- No change to what the tools do: focus ordering, technology dimming, search (`text.js`), the one-page view, the job ad
  matcher's matching, and the URL parameters all keep working as they do.
- Search stays out of the address, as today.
- The language menu keeps linking to each language's plain home page. It drops the tailored parameters today, and it
  will keep doing so.
- The matcher's wording stays as it is, including its formal hint.
- No new component in the operandor design system. Version 0.2.0 has `op-dialog`, `op-tag` and `op-badge` but no
  drawer, so the panel is built in the theme.

## Anti-goals

These would make the change a failure even if it technically worked:

- A recruiter on a tailored link can't tell the CV is filtered, or can't find the way back to the full CV.
- On a wide desktop the panel covers the CV column while the owner sets up a link. The owner must see each change take
  effect as it is made.
- Any control shows that does nothing without the script.
- Something that works today stops working: the URL state, the profile ordering, the one-page view, a search hit or
  timeline link going back to the full view, the tailored PDF button, or print.

## Decided with the owner

### Header button

- It goes in `SiteHeader`'s `Actions` slot, before the PDF button. On a 375 px screen the slot stays in the header
  bar (the PDF button is visible there; only the language menu moves into the menu behind the toggle), so the button
  is reachable on phones too.
- Its label is "Szűrés" in Hungarian, translated in the other three languages, with a sliders icon.
- A count badge shows how many link settings are active: a focus, a technology, the one-page view (0 to 3). At 0
  there is no badge. The job ad matcher and the search are not counted.
- On narrow screens it shows only the icon and the badge. The label stays available as its accessible name.
- It is hidden in the markup and shown by the script, as the tools are today. It carries `aria-expanded` and
  `aria-controls`.

### Panel

- **Contents, top to bottom:**
  - a title ("Szűrés és nézet") and a close button;
  - search, with its result links and status line;
  - focus (only when the CV has focus profiles, as today);
  - technology;
  - the view switch (only when the CV has a one-page view, as today);
  - **copy link** and **print**;
  - the job ad matcher, as a collapsed section;
  - **clear filters**.
- **Wide desktop.** The panel opens on the right, under the sticky header, at full remaining height with its own
  scroll. It is non-modal: no backdrop, and the CV stays interactive. The page content moves aside so the panel does
  not cover the 920 px column. At 1440 px a panel of about 360 px leaves 1080 px, enough for the column.
- **Medium widths, where the column and the panel don't fit side by side.** The same non-modal panel covers the right
  part of the content.
- **Phones (the existing 640 px breakpoint).** The panel is a bottom sheet about three quarters of the screen high.
  It is modal, with a backdrop, and a tap on the backdrop closes it.
- **Closing.** The close button and Escape close the panel everywhere, and on phones the backdrop does too. Opening
  moves keyboard focus into the panel; closing returns it to the header button. On phones, focus stays inside the
  sheet while it is open.
- **Not remembered.** The open state is not stored and not put in the address. A page opened from a tailored link
  starts with the panel closed.
- **Search hits.** On a phone, following a search hit closes the sheet so the reader lands on the item. On desktop
  the panel stays open.
- **Print.** The button and the panel never print.

### Copy link

- It copies the current address, with the focus, technology and view parameters the script already keeps in it, and
  announces the existing "Link másolva" text through the page's live region.
- It reuses the existing `CopyLink` text.

### Clear filters

- It resets the focus, the technology, the view (to full) and the search. It also empties the matcher's text and
  removes its highlights (`cv-tag--ad`, `cv-skill--ad`) and its result. The address loses its parameters.

### Tailored-view note in the hero

- It sits **under the job title**, replacing today's `cv-hero__focus` paragraph (`data-cv-focus-note`), so it is on
  the first screen on phones as well. The tailored PDF button (`data-cv-focus-download`) keeps working as today.
- It shows only when at least one link setting is active (focus, technology, one-page view). The search and the matcher
  never show it.
- Its text names what is set, in translatable fragments the script joins with " · ": a lead ("Szabott nézet:"),
  the focus profile's label, "egy oldal", "{0} kiemelve". Only the set parts appear. Example, as the owner chose
  it: "Szabott nézet: Szoftverarchitekt · egy oldal · .NET kiemelve".
  - Why fragments: a sentence ("…szerepre szabott, egyoldalas nézet") would need the profile label declined in
    Croatian and Serbian. The content holds one form of each label.
- Next to it is a **"Teljes önéletrajz →"** link that brings back the full CV with a clean address.
- It looks like the approved mockup (shown inline in the conversation, not saved as a file):
  - a box with rounded corners (about 8 px), not a pill: it wraps on a phone, and round ends would squeeze the wrapped
    lines, as with the search results;
  - a thin accent border and a light accent tint;
  - inside, in one row that wraps: a target icon, the text, then the "Teljes önéletrajz →" link in the accent color
    and medium weight;
  - at 375 px the link drops to its own line inside the box.
- **Print.** The note's text prints, as today's focus note does; the "full CV" link does not.

### Job ad matcher

- It moves into the panel unchanged: same text field, button, result list and highlights.
- On desktop the panel doesn't cover the column, so the owner sees the highlights in the CV right away. On phones the
  result list in the panel is enough.

### Texts

All new texts exist in the four resource files (`ThemeText.resx`, `.hu`, `.hr`, `.sr-Latn`). `ThemeTextShould`
already fails when a satellite misses a key or a placeholder. Texts the script shows go in `ResumeData.ScriptKeys`.

## Calls made without the owner (check these)

- **The "full CV" link** is a real link to the page's own address without parameters. It reloads the page instead of
  resetting it in the script, which is robust and also clears the search and the matcher.
- **The new texts in Hungarian:** button "Szűrés", panel title "Szűrés és nézet", "Bezárás", "Szűrők törlése",
  "Szabott nézet", "egy oldal", "{0} kiemelve", "Teljes önéletrajz", and an accessible count such as "{0} aktív
  beállítás".
- **Without a clipboard API** (`navigator.clipboard`), the copy link button stays hidden.
- **Release.** The plan ends at a pull request to `resume-engine`'s `main`. Two steps follow only on the owner's
  explicit go-ahead, each on its own:
  - tagging a new minor version, `v0.3.0` (MinVer), because what the page shows changes;
  - in the site repo, moving the four `Sipos.Resume.*` package versions to it and rebuilding, in the usual "Build the
    site with resume engine X" commit.

## Where the work happens

`resume-engine`, on a new branch from `main` (the local `claude/engine-0.2.0` is already merged):

- `Pages/ResumePage.razor`: the button in the header's `Actions` slot; the tools leave the card band. The band then
  starts with the sections; keep its spacing right.
- `Sections/Tools.razor`: becomes the panel.
- `Sections/Hero.razor`: the note and its link, in place of the focus paragraph.
- `wwwroot/css/cv.css`: the panel, its three layouts, the note, print.
- `wwwroot/js/cv.js`: open and close, the badge, copy link, clear, the note's text. It reuses the existing `state`,
  `remember()` and `apply()`.
- `Localization/ThemeText*.resx`, `Rendering/ResumeData.cs` (`ScriptKeys`).
- Tests in `tests/Sipos.Resume.Theme.Operandor.Tests`:
  - `HidesToolsUntilScriptGivenPage` and its kin: the button, the panel and the note are hidden in the markup.
  - The button is in the header.
  - The note's link points to the page's plain address.
  - The new keys exist in all four languages.

## Left to the builder

- Exact breakpoints, panel width and animation.
- Which element the panel is (`<dialog>` with `show()`/`showModal()`, or an `aside`), as long as the behavior above
  holds.
- The icon and the resource key names.
- How the content moves aside on wide screens.

No browser tests exist (Jint only runs `text.js`). Check the behavior by hand in a browser:

- at about 1440, 1024 and 375 px wide;
- in light and dark;
- by keyboard alone;
- with a tailored link opened fresh (`?focus=…&tech=…&view=short`);
- in print preview.
