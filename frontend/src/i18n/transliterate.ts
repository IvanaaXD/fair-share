/**
 * Serbian Cyrillic -> Latin. The mapping is one-to-one, so the Latin version of the app is
 * generated from the Cyrillic texts instead of being translated and maintained separately.
 * The same function is used for texts that come from the API (errors, notifications).
 */
const CYRILLIC_TO_LATIN: Record<string, string> = {
  А: 'A', Б: 'B', В: 'V', Г: 'G', Д: 'D', Ђ: 'Đ', Е: 'E', Ж: 'Ž', З: 'Z', И: 'I',
  Ј: 'J', К: 'K', Л: 'L', Љ: 'Lj', М: 'M', Н: 'N', Њ: 'Nj', О: 'O', П: 'P', Р: 'R',
  С: 'S', Т: 'T', Ћ: 'Ć', У: 'U', Ф: 'F', Х: 'H', Ц: 'C', Ч: 'Č', Џ: 'Dž', Ш: 'Š',
  а: 'a', б: 'b', в: 'v', г: 'g', д: 'd', ђ: 'đ', е: 'e', ж: 'ž', з: 'z', и: 'i',
  ј: 'j', к: 'k', л: 'l', љ: 'lj', м: 'm', н: 'n', њ: 'nj', о: 'o', п: 'p', р: 'r',
  с: 's', т: 't', ћ: 'ć', у: 'u', ф: 'f', х: 'h', ц: 'c', ч: 'č', џ: 'dž', ш: 'š',
};

export function toLatin(text: string): string {
  let result = '';
  for (const char of text) {
    result += CYRILLIC_TO_LATIN[char] ?? char;
  }
  return result;
}

/** Transliterates every string in a (nested) translation object, keeping its shape. */
export function transliterateResources<T>(resources: T): T {
  if (typeof resources === 'string') {
    return toLatin(resources) as T;
  }
  if (resources !== null && typeof resources === 'object') {
    const copy: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(resources)) {
      copy[key] = transliterateResources(value);
    }
    return copy as T;
  }
  return resources;
}
