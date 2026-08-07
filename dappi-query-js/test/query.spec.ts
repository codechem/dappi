import { describe, expect, it } from 'vitest';
import { createQuery, field, serializeValue } from '../src/index';
import { decodePairs, parseFilterKey } from './collection-filter-parser';

describe('field / condition', () => {
  it('splits dotted paths into nested segments', () => {
    const filter = field('author.name').containsIgnoreCase('john');
    expect(filter.fields).toEqual(['author', 'name']);
    expect(filter.operation).toBe('cic');
    expect(filter.value).toBe('john');
  });

  it('accepts separate segments', () => {
    const filter = field('author', 'name').eq('john');
    expect(filter.fields).toEqual(['author', 'name']);
  });

  it('throws on empty path', () => {
    expect(() => field('')).toThrow();
    expect(() => field('   ')).toThrow();
  });

  it('maps every operation to its wire token', () => {
    const f = () => field('x');
    expect(f().eq(1).operation).toBe('eq');
    expect(f().eqIgnoreCase('a').operation).toBe('eqic');
    expect(f().ne(1).operation).toBe('ne');
    expect(f().lt(1).operation).toBe('lt');
    expect(f().lte(1).operation).toBe('lte');
    expect(f().gt(1).operation).toBe('gt');
    expect(f().gte(1).operation).toBe('gte');
    expect(f().contains('a').operation).toBe('c');
    expect(f().containsIgnoreCase('a').operation).toBe('cic');
    expect(f().notContains('a').operation).toBe('nc');
    expect(f().notContainsIgnoreCase('a').operation).toBe('ncic');
    expect(f().startsWith('a').operation).toBe('sw');
    expect(f().endsWith('a').operation).toBe('ew');
    expect(f().isNull().operation).toBe('null');
    expect(f().isNotNull().operation).toBe('notnull');
  });
});

describe('serializeValue', () => {
  it('handles primitives, dates, arrays and nullish', () => {
    expect(serializeValue(42)).toBe('42');
    expect(serializeValue(true)).toBe('true');
    expect(serializeValue('hello')).toBe('hello');
    expect(serializeValue(new Date('2024-01-02T03:04:05.000Z'))).toBe('2024-01-02T03:04:05.000Z');
    expect(serializeValue([1, 2, 3])).toBe('1,2,3');
    expect(serializeValue(null)).toBe('');
    expect(serializeValue(undefined)).toBe('');
  });
});

describe('DappiQuery.toQueryString', () => {
  it('is empty for an empty query', () => {
    expect(createQuery().toQueryString()).toBe('');
  });

  it('builds a single filter clause with an index', () => {
    const qs = createQuery().or(field('title').containsIgnoreCase('breaking')).toQueryString();
    expect(qs).toBe('filter[0][$or][title][$cic]=breaking');
  });

  it('increments the index per clause and preserves operators', () => {
    const qs = createQuery()
      .or(field('title').containsIgnoreCase('breaking'))
      .and(field('views').gt(100))
      .toQueryString();
    expect(qs).toBe('filter[0][$or][title][$cic]=breaking&filter[1][$and][views][$gt]=100');
  });

  it('serializes nested field paths', () => {
    const qs = createQuery().or(field('author.name').eq('john')).toQueryString();
    expect(qs).toBe('filter[0][$or][author][name][$eq]=john');
  });

  it('adds pagination, sorting, search, fields and include', () => {
    const qs = createQuery()
      .page(2, 20)
      .sort('publishedAt', 'desc')
      .search('foo bar')
      .select('id', 'title')
      .include()
      .toQueryString();

    const params = new Map(decodePairs(qs));
    expect(params.get('Offset')).toBe('20');
    expect(params.get('Limit')).toBe('20');
    expect(params.get('SortBy')).toBe('publishedAt');
    expect(params.get('SortDirection')).toBe('Descending');
    expect(params.get('SearchTerm')).toBe('foo bar');
    expect(params.get('fields')).toBe('id,title');
    expect(params.get('include')).toBe('*');
  });

  it('defaults sort direction to Ascending', () => {
    const qs = createQuery().sort('name').toQueryString();
    const params = new Map(decodePairs(qs));
    expect(params.get('SortDirection')).toBe('Ascending');
  });

  it('encodes reserved characters in values', () => {
    const qs = createQuery().or(field('title').eq('a & b = c')).toQueryString();
    const [[key, value]] = decodePairs(qs);
    expect(key).toBe('filter[0][$or][title][$eq]');
    expect(value).toBe('a & b = c');
  });

  it('computes offset from page number', () => {
    const params = new Map(decodePairs(createQuery().page(3, 15).toQueryString()));
    expect(params.get('Offset')).toBe('30');
    expect(params.get('Limit')).toBe('15');
  });

  it('rejects a non-positive page number', () => {
    expect(() => createQuery().page(0, 10)).toThrow();
  });
});

describe('DappiQuery.toURL', () => {
  it('appends with ? when no query present', () => {
    const url = createQuery().or(field('id').eq('1')).toURL('https://api.test/articles');
    expect(url).toBe('https://api.test/articles?filter[0][$or][id][$eq]=1');
  });

  it('appends with & when the base already has a query', () => {
    const url = createQuery().limit(5).toURL('https://api.test/articles?foo=1');
    expect(url).toBe('https://api.test/articles?foo=1&Limit=5');
  });

  it('returns the base URL unchanged for an empty query', () => {
    expect(createQuery().toURL('https://api.test/articles')).toBe('https://api.test/articles');
  });
});

describe('round-trip against the CollectionFilter parser', () => {
  it('parses back to the intended filter clauses', () => {
    const qs = createQuery()
      .or(field('author.name').containsIgnoreCase('john'))
      .and(field('views').gt(100))
      .and(field('deletedAt').isNull())
      .toQueryString();

    const filterPairs = decodePairs(qs).filter(([key]) => key.startsWith('filter'));
    const parsed = filterPairs.map(([key, value]) => parseFilterKey(key, value));

    expect(parsed).toEqual([
      { fields: ['author', 'name'], operator: 'or', operation: 'cic', value: 'john' },
      { fields: ['views'], operator: 'and', operation: 'gt', value: '100' },
      { fields: ['deletedAt'], operator: 'and', operation: 'null', value: '' },
    ]);
  });
});
