export class Pluralizer {
    private static readonly Irregular: Map<string, string> = new Map([
        ["person", "people"],
        ["man", "men"],
        ["woman", "women"],
        ["child", "children"],
        ["tooth", "teeth"],
        ["foot", "feet"],
        ["mouse", "mice"],
        ["goose", "geese"],
        ["ox", "oxen"],
        ["louse", "lice"],
        ["die", "dice"],
        ["index", "indices"],
        ["appendix", "appendices"],
        ["cactus", "cacti"],
        ["focus", "foci"],
        ["fungus", "fungi"],
        ["nucleus", "nuclei"],
        ["radius", "radii"],
        ["stimulus", "stimuli"],
        ["analysis", "analyses"],
        ["thesis", "theses"],
        ["crisis", "crises"],
        ["phenomenon", "phenomena"],
        ["criterion", "criteria"],
        ["datum", "data"]
    ]);

    private static readonly Uncountables: Set<string> = new Set([
        "sheep", "fish", "deer", "series", "species", "money", "rice", "information",
        "equipment", "knowledge", "traffic", "baggage", "furniture", "advice"
    ]);

    private static readonly F_Exceptions: Set<string> = new Set([
        "roof", "belief", "chef", "chief", "proof", "safe"
    ]);

    private static readonly O_Es_Exceptions: Set<string> = new Set([
        "hero", "echo", "potato", "tomato", "torpedo", "veto"
    ]);

    public static pluralizeEN(word: string): string {
        if (!word || !word.trim()) {
            return word;
        }

        const lower = word.toLowerCase();

        // Uncountables
        if (this.Uncountables.has(lower)) {
            return word;
        }

        // Irregular
        if (this.Irregular.has(lower)) {
            return this.matchCase(word, this.Irregular.get(lower)!);
        }

        // Common endings: s, x, z, ch, sh → +es
        if (/(s|x|z|ch|sh)$/.test(lower)) {
            return this.matchCase(word, word + "es");
        }

        // Ends with consonant + y → -ies
        if (/[^aeiou]y$/.test(lower)) {
            return this.matchCase(word, this.takeCharsFromEnd(word, 1) + "ies");
        }

        // Ends with vowel + y → +s
        if (lower.endsWith("y")) {
            return this.matchCase(word, word + "s");
        }

        // Ends with -fe or -f → -ves (except some)
        if (lower.endsWith("fe")) {
            if (this.F_Exceptions.has(lower)) {
                return this.matchCase(word, word + "s");
            }
            return this.matchCase(word, this.takeCharsFromEnd(word, 2) + "ves");
        }

        if (lower.endsWith("f")) {
            if (this.F_Exceptions.has(lower)) {
                return this.matchCase(word, word + "s");
            }
            return this.matchCase(word, this.takeCharsFromEnd(word, 1) + "ves");
        }

        // Ends with -o → +es for exceptions
        if (lower.endsWith("o")) {
            if (this.O_Es_Exceptions.has(lower)) {
                return this.matchCase(word, word + "es");
            }
            return this.matchCase(word, word + "s");
        }

        // Ends with -is → -es
        if (lower.endsWith("is")) {
            return this.matchCase(word, this.takeCharsFromEnd(word, 2) + "es");
        }

        // Default: add 's'
        return this.matchCase(word, word + "s");
    }

    private static matchCase(original: string, result: string): string {
        if (!original) {
            return result;
        }

        if (this.isAllUpper(original)) {
            return result.toUpperCase();
        }

        if (this.isCapitalized(original)) {
            return result.charAt(0).toUpperCase() + result.slice(1);
        }

        return result;
    }

    private static takeCharsFromEnd(text: string, numChars: number): string {
        return text.substring(0, text.length - numChars);
    }

    private static isAllUpper(s: string): boolean {
        return [...s].every(c => !/[a-z]/.test(c) || c === c.toUpperCase());
    }

    private static isCapitalized(s: string): boolean {
        return s.length > 1 && s[0] === s[0].toUpperCase() && s[1] === s[1].toLowerCase();
    }
}