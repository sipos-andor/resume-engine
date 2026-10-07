// Folds, tokenizes and matches text exactly as the engine's C# does (TextNormalizer, SearchIndex.Find,
// JobVocabulary.Match), so the page finds what the build indexed. The engine's tests run this module with Jint on the
// same vectors as the C# (tests/Shared/text-vectors.json).

// Letters that do not decompose into a base letter and a mark, and İ and the final ς, which toLowerCase treats unlike
// .NET's ToLowerInvariant; the C# maps the same ones.
const letters = [["İ", "i"], ["ς", "σ"], ["đ", "d"], ["ł", "l"], ["ø", "o"], ["ß", "ss"], ["æ", "ae"], ["œ", "oe"]];

/** Lowercases, transliterates and removes accents: "Sípos Đurđevo" becomes "sipos durdevo". */
export const fold = (text) => {
    let lower = text.toLowerCase();
    for (const [from, to] of letters) {
        lower = lower.split(from).join(to);
    }
    return lower.normalize("NFD").replace(/\p{Mn}/gu, "").normalize("NFC");
};

/** Splits folded text into words, keeping "c#", ".net", "c++" and "node.js" whole and dropping trailing dots. */
export const tokens = (text) =>
    (fold(text).match(/[\p{L}\p{N}#+.]+/gu) ?? [])
        .map((token) => token.replace(/\.+$/, ""))
        .filter((token) => token.length > 0);

/** The key that makes two spellings of a technology the same, as the C# Keys.Of. */
export const key = (text) => tokens(text).join(" ");

const contains = (list, phrase) => {
    for (let start = 0; start + phrase.length <= list.length; start++) {
        let found = true;
        for (let i = 0; i < phrase.length && found; i++) {
            found = list[start + i] === phrase[i];
        }
        if (found) {
            return true;
        }
    }
    return false;
};

/** The vocabulary terms whose name or an alias appears in a text as whole consecutive words. */
export const match = (vocabulary, text) => {
    const list = tokens(text);
    return vocabulary.filter((term) => term.phrases.some((phrase) => contains(list, phrase)));
};

/** The search index's entries that have a word starting with every word of the query. */
export const find = (index, query) => {
    const words = tokens(query);
    if (words.length === 0) {
        return [];
    }
    let hits = null;
    for (const word of words) {
        const found = new Set();
        for (const [token, entries] of Object.entries(index.tokens)) {
            if (token.startsWith(word)) {
                entries.forEach((entry) => found.add(entry));
            }
        }
        hits = hits === null ? found : new Set([...hits].filter((entry) => found.has(entry)));
    }
    return [...hits].sort((a, b) => a - b).map((entry) => index.entries[entry]);
};
