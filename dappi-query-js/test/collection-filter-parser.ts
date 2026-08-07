// JS port of Dappi's CollectionFilter parsing, so tests round-trip the query
// strings without a running backend.
// Reference: Dappi.HeadlessCms/ActionFilters/CollectionFilter.cs

const OPERATIONS = new Set([
  'eq',
  'eqic',
  'ne',
  'lt',
  'lte',
  'gt',
  'gte',
  'c',
  'cic',
  'nc',
  'ncic',
  'sw',
  'ew',
  'null',
  'notnull',
]);

export interface ParsedFilter {
  fields: string[];
  operator: 'and' | 'or';
  operation: string;
  value: string;
}

/** Parse one `filter...` query key + value the way the C# action filter does. */
export function parseFilterKey(key: string, value: string): ParsedFilter {
  const brackets = [...key.matchAll(/\[(.*?)\]/g)].map((match) => match[1] ?? '');

  const result: ParsedFilter = {
    fields: [],
    operator: 'or', // Filter model default
    operation: '',
    value,
  };

  for (const token of brackets) {
    if (token === '$and' || token === '$or') {
      result.operator = token.slice(1) as 'and' | 'or';
    } else if (token.startsWith('$')) {
      const op = token.slice(1).toLowerCase();
      if (!OPERATIONS.has(op)) {
        throw new Error(`Invalid filter operation: ${op}`);
      }
      result.operation = op;
    } else if (/^\d+$/.test(token)) {
      // index segment: ignored server-side (only differentiates keys)
    } else {
      result.fields.push(token);
    }
  }

  return result;
}

/** Split a query string into decoded [key, value] pairs. */
export function decodePairs(queryString: string): Array<[string, string]> {
  if (queryString.length === 0) {
    return [];
  }
  return queryString.split('&').map((pair) => {
    const eq = pair.indexOf('=');
    const rawKey = eq === -1 ? pair : pair.slice(0, eq);
    const rawValue = eq === -1 ? '' : pair.slice(eq + 1);
    return [decodeURIComponent(rawKey), decodeURIComponent(rawValue)];
  });
}
