// The page's interactive features. The build derives everything (search index, technologies, position profiles, job ad
// vocabulary) and writes it into the #cv-data block; this script only applies it. Without the script the whole CV
// shows and the tools stay hidden.
import { find, key, match } from "./text.js";

const data = JSON.parse(document.getElementById("cv-data")?.textContent ?? "null");
const words = data?.text ?? {};
const format = (template, ...values) => (template ?? "").replace(/\{(\d+)\}/g, (_, index) => String(values[Number(index)]));

// A polite live region for confirmations such as "Link copied".
const live = document.createElement("p");
live.className = "op-visually-hidden";
live.setAttribute("role", "status");
document.body.append(live);
const announce = (message) => {
    live.textContent = "";
    requestAnimationFrame(() => { live.textContent = message; });
};

// Theme switch: the design system's ThemeToggle markup driven by the shared kernel's theme.js, in the order of its
// ThemeCycle.Next (system → the opposite of the system's → the system's → system).
const theme = window.OperandorSharedKernelUI?.theme;
const toggle = document.querySelector("[data-cv-theme-toggle]");
if (theme && toggle) {
    toggle.hidden = false;
    const next = (preference, system) => {
        const opposite = system === "dark" ? "light" : "dark";
        return preference === "system" ? opposite : preference === opposite ? system : "system";
    };
    const icons = { light: "sun", dark: "moon", system: "monitor" };
    const labels = { dark: words.SwitchToDark, light: words.SwitchToLight, system: words.SwitchToSystem };
    const show = () => {
        const preference = theme.getPreference();
        const coming = next(preference, theme.getSystemTheme());
        const icon = toggle.querySelector(".op-icon");
        for (const property of ["mask-image", "-webkit-mask-image"]) {
            const value = icon?.style.getPropertyValue(property);
            if (value) {
                icon.style.setProperty(property, value.replace(/[a-z-]+\.svg/, `${icons[preference]}.svg`));
            }
        }
        if (labels[coming]) {
            toggle.setAttribute("aria-label", labels[coming]);
            toggle.title = labels[coming];
        }
    };
    toggle.addEventListener("click", () => {
        theme.setPreference(next(theme.getPreference(), theme.getSystemTheme()));
        show();
    });
    matchMedia("(prefers-color-scheme: dark)").addEventListener("change", show);
    show();
}

// Menus: the header's and the language menu's <details> close on a click outside, on Escape and when a link in them
// is followed, as the design system's components do once their runtime starts.
const openMenus = () => document.querySelectorAll("details.op-langmenu[open], details.op-header__menu[open]");
document.addEventListener("click", (event) => {
    for (const menu of openMenus()) {
        if (!menu.contains(event.target) || event.target.closest("a")) {
            menu.open = false;
        }
    }
});
document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape") {
        return;
    }
    for (const menu of openMenus()) {
        menu.open = false;
        menu.querySelector("summary")?.focus();
    }
});

// Deep links: a section's or an item's # link copies its address; without clipboard access it simply leads there.
for (const anchor of document.querySelectorAll("[data-cv-copy]")) {
    anchor.addEventListener("click", async (event) => {
        if (!navigator.clipboard) {
            return;
        }
        event.preventDefault();
        history.replaceState(null, "", anchor.href);
        try {
            await navigator.clipboard.writeText(anchor.href);
            anchor.classList.add("is-copied");
            announce(words.LinkCopied);
            setTimeout(() => anchor.classList.remove("is-copied"), 1600);
        } catch {
            location.hash = new URL(anchor.href).hash;
        }
    });
}

const tools = document.querySelector("[data-cv-tools]");
const main = document.querySelector("[data-cv-view-root]");
if (data && tools && main) {
    tools.hidden = false;
    const items = [...document.querySelectorAll("[data-cv-id]")];
    const work = items.filter((item) => item.dataset.cvTech !== undefined);
    const byId = new Map(items.map((item) => [item.dataset.cvId, item]));
    const bars = [...document.querySelectorAll("[data-cv-bar]")];
    const search = document.getElementById("cv-search");
    const focus = document.getElementById("cv-focus");
    const technology = document.getElementById("cv-technology");
    const results = document.getElementById("cv-search-results");
    const status = document.getElementById("cv-search-status");
    const summary = document.querySelector("[data-cv-summary]");
    const focusNote = document.querySelector("[data-cv-focus-note]");
    const viewButtons = [...document.querySelectorAll("[data-cv-view]")];
    const params = new URLSearchParams(location.search);
    const state = {
        focus: data.focus.some((view) => view.id === params.get("focus")) ? params.get("focus") : "",
        technology: data.technologies.some((entry) => entry.key === params.get("tech")) ? params.get("tech") : "",
        view: params.get("view") === "short" && viewButtons.length > 0 ? "short" : "full",
        query: "",
    };

    const remember = () => {
        const url = new URL(location.href);
        for (const [name, value] of [["focus", state.focus], ["tech", state.technology], ["view", state.view === "short" ? "short" : ""]]) {
            if (value) {
                url.searchParams.set(name, value);
            } else {
                url.searchParams.delete(name);
            }
        }
        history.replaceState(null, "", url);
    };

    const applyFocus = () => {
        const view = data.focus.find((entry) => entry.id === state.focus);
        const emphasized = new Set(view?.emphasized ?? []);
        for (const item of items) {
            item.classList.toggle("cv-focus", emphasized.has(item.dataset.cvId) && item.dataset.cvTech !== undefined);
        }
        const strengths = document.querySelector('[data-cv-order="strengths"]');
        for (const element of strengths?.children ?? []) {
            const index = view ? view.strengths.indexOf(element.dataset.cvId) : -1;
            element.style.order = index < 0 ? "" : String(index);
        }
        const groups = document.querySelector('[data-cv-order="skill-groups"]');
        for (const element of groups?.children ?? []) {
            const index = view ? view.skillGroups.indexOf(Number(element.dataset.cvGroup)) : -1;
            element.style.order = index < 0 ? "" : String(index);
        }
        const skills = new Set(view?.skills ?? []);
        for (const skill of document.querySelectorAll("[data-cv-skill]")) {
            skill.classList.toggle("cv-skill--match", skills.has(skill.dataset.cvSkill));
        }
        if (summary) {
            summary.textContent = view?.summary ?? summary.dataset.cvSummaryDefault;
        }
        if (focusNote) {
            focusNote.hidden = !view;
            focusNote.textContent = view ? `${focus?.labels?.[0]?.textContent ?? ""}: ${view.label}` : "";
        }
        for (const button of document.querySelectorAll("[data-cv-focus-download]")) {
            button.hidden = button.dataset.cvFocusDownload !== state.focus;
        }
    };

    const applyTechnology = () => {
        const chosen = data.technologies.find((entry) => entry.key === state.technology);
        const using = new Set(chosen?.items ?? []);
        // A position stays lit when one of its projects uses the technology.
        for (const item of work) {
            const lit = !chosen || using.has(item.dataset.cvId)
                || [...item.querySelectorAll("[data-cv-tech]")].some((child) => using.has(child.dataset.cvId));
            item.classList.toggle("cv-dim", !lit);
        }
        for (const bar of bars) {
            bar.querySelector("rect")?.classList.toggle("is-dim", Boolean(chosen) && !using.has(bar.dataset.cvBar));
        }
        for (const tag of document.querySelectorAll(".cv-tags .op-tag")) {
            tag.classList.toggle("cv-tag--match", Boolean(chosen) && key(tag.textContent) === chosen.key);
        }
        return chosen ? format(words.TechnologyItems, chosen.name, chosen.items.length) : "";
    };

    const applySearch = () => {
        for (const item of items) {
            item.classList.remove("cv-hit");
        }
        results.replaceChildren();
        results.hidden = true;
        if (!state.query.trim()) {
            return "";
        }
        const found = find(data.search, state.query);
        for (const [id, title] of found) {
            byId.get(id)?.classList.add("cv-hit");
            const link = document.createElement("a");
            link.href = `#${encodeURIComponent(id)}`;
            link.textContent = title;
            results.append(link);
        }
        results.hidden = found.length === 0;
        return found.length === 0 ? words.SearchNoResults : format(words.SearchResults, found.length);
    };

    const applyView = () => {
        main.dataset.cvView = state.view;
        for (const button of viewButtons) {
            button.setAttribute("aria-pressed", String(button.dataset.cvView === state.view));
        }
    };

    const apply = () => {
        applyFocus();
        applyView();
        const messages = [applyTechnology(), applySearch()].filter(Boolean);
        status.textContent = messages.join(" · ");
    };

    if (focus) {
        focus.value = state.focus;
        focus.addEventListener("change", () => { state.focus = focus.value; remember(); apply(); });
    }
    technology.value = state.technology;
    technology.addEventListener("change", () => { state.technology = technology.value; remember(); apply(); });
    let typing;
    search.addEventListener("input", () => {
        clearTimeout(typing);
        typing = setTimeout(() => { state.query = search.value; apply(); }, 120);
    });
    for (const button of viewButtons) {
        button.addEventListener("click", () => { state.view = button.dataset.cvView; remember(); apply(); });
    }
    document.querySelector("[data-cv-print]")?.addEventListener("click", () => window.print());

    // A search hit, a skill's evidence or a timeline bar may lead to an item the one-page view hides: the full view
    // comes back first, so the link arrives somewhere.
    document.addEventListener("click", (event) => {
        const link = event.target.closest?.('a[href^="#"]');
        if (!link || state.view !== "short") {
            return;
        }
        const target = document.getElementById(decodeURIComponent(link.getAttribute("href").slice(1)));
        if (target?.closest('[data-cv-short="false"], [data-cv-extra]')) {
            state.view = "full";
            remember();
            apply();
        }
    });

    // Job ad matcher: the ad's text never leaves the browser.
    const matchText = document.getElementById("cv-match-text");
    const matchResult = document.getElementById("cv-match-result");
    document.querySelector("[data-cv-match]")?.addEventListener("click", () => {
        const terms = match(data.vocabulary, matchText.value);
        const keys = new Set(terms.map((term) => key(term.term)));
        for (const tag of document.querySelectorAll(".cv-tags .op-tag")) {
            tag.classList.toggle("cv-tag--match", keys.has(key(tag.textContent)));
        }
        for (const skill of document.querySelectorAll("[data-cv-skill]")) {
            skill.classList.toggle("cv-skill--match", keys.has(skill.dataset.cvSkill));
        }
        matchResult.replaceChildren();
        if (terms.length === 0) {
            matchResult.textContent = matchText.value.trim() ? words.MatchNone : "";
            return;
        }
        const heading = document.createElement("p");
        heading.textContent = format(words.MatchCoverage, terms.length);
        const list = document.createElement("ul");
        for (const term of [...terms].sort((a, b) => Number(b.skill) - Number(a.skill))) {
            const entry = document.createElement("li");
            entry.className = "op-tag cv-tag--match";
            entry.textContent = term.term;
            list.append(entry);
        }
        matchResult.append(heading, list);
    });

    apply();
}
