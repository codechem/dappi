# dappi-query

Fluent query-string builder for [Dappi](https://github.com/codechem/dappi) headless CMS APIs. Zero runtime dependencies, TypeScript-first.

It builds the `filter[...]`, `SortBy`/`SortDirection`, `Offset`/`Limit`, `SearchTerm`, `fields` and `include` parameters that Dappi's generated list endpoints understand.

## Install

```bash
npm install dappi-query
```

## Usage

```ts
import { createQuery, field } from 'dappi-query';

const qs = createQuery()
  .or(field('title').containsIgnoreCase('breaking'))
  .and(field('author.name').eq('john'))
  .and(field('views').gt(100))
  .sort('publishedAt', 'desc')
  .page(2, 20)
  .select('id', 'title')
  .include('*')
  .toQueryString();

// filter[0][$or][title][$cic]=breaking&filter[1][$and][author][name][$eq]=john
//   &filter[2][$and][views][$gt]=100&Offset=20&Limit=20
//   &SortBy=publishedAt&SortDirection=Descending&fields=id%2Ctitle&include=*

fetch(`https://api.example.com/articles?${qs}`);
// or: createQuery()...toURL('https://api.example.com/articles')
```

## Filtering

Start a condition with `field()` (dotted paths or separate segments navigate relations) and add it with `.and()` / `.or()`.

```ts
field('author.name').containsIgnoreCase('john');
field('author', 'name').containsIgnoreCase('john'); // equivalent
```

> Dappi strips a leading `$or` marker but not `$and`, so make the **first** clause an `$or`. `.where()` is an alias for `.or()` for that reason.

### Operations

| Method | Token | Meaning |
| --- | --- | --- |
| `eq(v)` | `$eq` | equals |
| `eqIgnoreCase(v)` | `$eqic` | equals, case-insensitive |
| `ne(v)` | `$ne` | not equals |
| `lt` / `lte` / `gt` / `gte` | `$lt` `$lte` `$gt` `$gte` | comparison |
| `contains(v)` | `$c` | contains |
| `containsIgnoreCase(v)` | `$cic` | contains, case-insensitive |
| `notContains(v)` | `$nc` | not contains |
| `notContainsIgnoreCase(v)` | `$ncic` | not contains, case-insensitive |
| `startsWith(v)` | `$sw` | starts with |
| `endsWith(v)` | `$ew` | ends with |
| `isNull()` | `$null` | is null |
| `isNotNull()` | `$notnull` | is not null |

Values are serialized so Dappi infers the type (int / bool / Guid / DateTime / string). Arrays and comma-separated strings become server-side lists. `In`/`NotIn` are not exposed because they are not implemented server-side yet.

## Sorting, pagination, search, shaping

| Method | Parameter |
| --- | --- |
| `sort(field, 'asc' \| 'desc')` | `SortBy`, `SortDirection` |
| `limit(n)` / `offset(n)` | `Limit`, `Offset` |
| `page(pageNumber, pageSize)` | `Offset = (pageNumber - 1) * pageSize`, `Limit = pageSize` |
| `search(term)` | `SearchTerm` |
| `select(...fields)` | `fields` (comma-separated) |
| `include(value = '*')` | `include` |

## Output

- `toQueryString()` — the query string, no leading `?`
- `toURL(baseUrl)` — appends to `baseUrl` with the correct `?`/`&`
- `toString()` — alias for `toQueryString()`

## Development

```bash
npm install
npm test        # vitest
npm run build   # emit dist/
```
