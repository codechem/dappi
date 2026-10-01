import { Operation } from './operations';

export interface Filter {
  readonly fields: string[];
  readonly operation: Operation;
  readonly value: string;
}

// Dappi infers the type server-side and splits on `,` to build a list.
export function serializeValue(value: unknown): string {
  if (value === null || value === undefined) {
    return '';
  }
  if (value instanceof Date) {
    return value.toISOString();
  }
  if (Array.isArray(value)) {
    return value.map(serializeValue).join(',');
  }
  return String(value);
}

export class Condition {
  constructor(private readonly fields: string[]) {}

  private make(operation: Operation, value: unknown): Filter {
    return { fields: this.fields, operation, value: serializeValue(value) };
  }

  eq(value: unknown): Filter {
    return this.make(Operation.Eq, value);
  }

  eqIgnoreCase(value: string): Filter {
    return this.make(Operation.Eqic, value);
  }

  ne(value: unknown): Filter {
    return this.make(Operation.Ne, value);
  }

  lt(value: unknown): Filter {
    return this.make(Operation.Lt, value);
  }

  lte(value: unknown): Filter {
    return this.make(Operation.Lte, value);
  }

  gt(value: unknown): Filter {
    return this.make(Operation.Gt, value);
  }

  gte(value: unknown): Filter {
    return this.make(Operation.Gte, value);
  }

  contains(value: string): Filter {
    return this.make(Operation.C, value);
  }

  containsIgnoreCase(value: string): Filter {
    return this.make(Operation.Cic, value);
  }

  notContains(value: string): Filter {
    return this.make(Operation.Nc, value);
  }

  notContainsIgnoreCase(value: string): Filter {
    return this.make(Operation.Ncic, value);
  }

  startsWith(value: string): Filter {
    return this.make(Operation.Sw, value);
  }

  endsWith(value: string): Filter {
    return this.make(Operation.Ew, value);
  }

  isNull(): Filter {
    return this.make(Operation.Null, '');
  }

  isNotNull(): Filter {
    return this.make(Operation.Notnull, '');
  }
}

// Accepts dotted paths (`author.name`) or separate segments (`'author', 'name'`).
export function field(...path: string[]): Condition {
  const segments = path
    .flatMap((part) => part.split('.'))
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0);

  if (segments.length === 0) {
    throw new Error('field() requires at least one non-empty path segment');
  }

  return new Condition(segments);
}
