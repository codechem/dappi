// Wire tokens match Dappi's C# `Operation` enum name lowercased, prefixed with
// `$` when serialized. `In`/`NotIn` are omitted: declared TODO, not implemented.
export const Operation = {
  Eq: 'eq',
  Eqic: 'eqic',
  Ne: 'ne',
  Lt: 'lt',
  Lte: 'lte',
  Gt: 'gt',
  Gte: 'gte',
  C: 'c',
  Cic: 'cic',
  Nc: 'nc',
  Ncic: 'ncic',
  Sw: 'sw',
  Ew: 'ew',
  Null: 'null',
  Notnull: 'notnull',
} as const;

export type Operation = (typeof Operation)[keyof typeof Operation];

export const LogicalOperator = {
  And: 'and',
  Or: 'or',
} as const;

export type LogicalOperator = (typeof LogicalOperator)[keyof typeof LogicalOperator];

export const SortDirection = {
  Ascending: 'Ascending',
  Descending: 'Descending',
} as const;

export type SortDirection = (typeof SortDirection)[keyof typeof SortDirection];
