import { Filter } from './condition';
import { LogicalOperator, SortDirection } from './operations';

interface FilterClause {
  operator: LogicalOperator;
  filter: Filter;
}

export type SortDirectionInput = SortDirection | 'asc' | 'desc';

function normalizeDirection(direction: SortDirectionInput): SortDirection {
  if (direction === 'desc' || direction === SortDirection.Descending) {
    return SortDirection.Descending;
  }
  return SortDirection.Ascending;
}

/**
 * Fluent builder for a Dappi-compatible query string.
 *
 * Dappi strips a leading `$or` marker but not `$and`, so the first clause should
 * generally be an `$or`; {@link DappiQuery.where} defaults to it for that reason.
 */
export class DappiQuery {
  private readonly clauses: FilterClause[] = [];
  private sortField?: string;
  private sortDirection: SortDirection = SortDirection.Ascending;
  private limitValue?: number;
  private offsetValue?: number;
  private searchTerm?: string;
  private readonly selectedFields: string[] = [];
  private includeValue?: string;

  and(filter: Filter): this {
    this.clauses.push({ operator: LogicalOperator.And, filter });
    return this;
  }

  or(filter: Filter): this {
    this.clauses.push({ operator: LogicalOperator.Or, filter });
    return this;
  }

  where(filter: Filter): this {
    return this.or(filter);
  }

  sort(field: string, direction: SortDirectionInput = 'asc'): this {
    this.sortField = field;
    this.sortDirection = normalizeDirection(direction);
    return this;
  }

  limit(value: number): this {
    this.limitValue = value;
    return this;
  }

  offset(value: number): this {
    this.offsetValue = value;
    return this;
  }

  page(pageNumber: number, pageSize: number): this {
    if (pageNumber < 1) {
      throw new Error('page() expects a 1-based page number');
    }
    this.limitValue = pageSize;
    this.offsetValue = (pageNumber - 1) * pageSize;
    return this;
  }

  search(term: string): this {
    this.searchTerm = term;
    return this;
  }

  select(...fields: string[]): this {
    const names = fields
      .flatMap((entry) => entry.split(','))
      .map((name) => name.trim())
      .filter((name) => name.length > 0);
    this.selectedFields.push(...names);
    return this;
  }

  include(value = '*'): this {
    this.includeValue = value;
    return this;
  }

  toQueryString(): string {
    const parts: string[] = [];

    // Bracket tokens and Dappi property names are URL-safe, so keys are emitted
    // literally (Strapi-style); only values are percent-encoded.
    this.clauses.forEach((clause, index) => {
      const fieldPath = clause.filter.fields.map((segment) => `[${segment}]`).join('');
      const key = `filter[${index}][$${clause.operator}]${fieldPath}[$${clause.filter.operation}]`;
      parts.push(`${key}=${encodeURIComponent(clause.filter.value)}`);
    });

    if (this.offsetValue !== undefined) {
      parts.push(`Offset=${this.offsetValue}`);
    }
    if (this.limitValue !== undefined) {
      parts.push(`Limit=${this.limitValue}`);
    }
    if (this.sortField !== undefined) {
      parts.push(`SortBy=${encodeURIComponent(this.sortField)}`);
      parts.push(`SortDirection=${this.sortDirection}`);
    }
    if (this.searchTerm) {
      parts.push(`SearchTerm=${encodeURIComponent(this.searchTerm)}`);
    }
    if (this.selectedFields.length > 0) {
      parts.push(`fields=${encodeURIComponent(this.selectedFields.join(','))}`);
    }
    if (this.includeValue !== undefined) {
      parts.push(`include=${encodeURIComponent(this.includeValue)}`);
    }

    return parts.join('&');
  }

  toString(): string {
    return this.toQueryString();
  }

  toURL(baseUrl: string): string {
    const qs = this.toQueryString();
    if (qs.length === 0) {
      return baseUrl;
    }
    const separator = baseUrl.includes('?') ? '&' : '?';
    return `${baseUrl}${separator}${qs}`;
  }
}

export function createQuery(): DappiQuery {
  return new DappiQuery();
}
