// The pure helpers of the page script: the {0} formatter, the tailored view's note and the filter button's count. cv.js
// imports them and does the DOM work; the engine's tests run this module with Jint. The state is cv.js's:
// { focus, technology, view, query }.

/** Fills the {0}, {1}… placeholders of a word of the page's text with the values. */
export const format = (template, ...values) => (template ?? "").replace(/\{(\d+)\}/g, (_, index) => String(values[Number(index)]));

/** How many link settings are active: a focus, a technology, the one-page view. The search is not one. */
export const activeCount = (state) => [state.focus !== "", state.technology !== "", state.view === "short"].filter(Boolean).length;

// Decision: the note is a lead and the parts that are set, joined with " · ": the profile's label, "one page", the
// technology with a word.
// Why: a sentence would need the profile's label declined in Croatian and Serbian; the content holds one form of it.
/** The tailored view's note: "Tailored view: Software architect · one page · Azure highlighted", or "" without a part. */
export const noteText = (state, data, words) => {
    const parts = [];
    const focus = data.focus.find((entry) => entry.id === state.focus);
    if (focus) {
        parts.push(focus.label);
    }
    if (state.view === "short") {
        parts.push(words.ViewNoteShort);
    }
    const technology = data.technologies.find((entry) => entry.key === state.technology);
    if (technology) {
        parts.push(format(words.ViewNoteTechnology, technology.name));
    }
    return parts.length === 0 ? "" : `${words.ViewNoteLead} ${parts.join(" · ")}`;
};
