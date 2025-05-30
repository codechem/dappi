import { AbstractControl, ValidationErrors } from '@angular/forms';
import { Observable, of, take } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ModelField } from '../models/content.model';

const CSHARP_KEYWORDS = [
  'abstract',
  'as',
  'base',
  'bool',
  'break',
  'byte',
  'case',
  'catch',
  'char',
  'checked',
  'class',
  'const',
  'continue',
  'decimal',
  'default',
  'delegate',
  'do',
  'double',
  'else',
  'enum',
  'event',
  'explicit',
  'extern',
  'false',
  'finally',
  'fixed',
  'float',
  'for',
  'foreach',
  'goto',
  'if',
  'implicit',
  'in',
  'int',
  'interface',
  'internal',
  'is',
  'lock',
  'long',
  'namespace',
  'new',
  'null',
  'object',
  'operator',
  'out',
  'override',
  'params',
  'private',
  'protected',
  'public',
  'readonly',
  'ref',
  'return',
  'sbyte',
  'sealed',
  'short',
  'sizeof',
  'stackalloc',
  'static',
  'string',
  'struct',
  'switch',
  'this',
  'throw',
  'true',
  'try',
  'typeof',
  'uint',
  'ulong',
  'unchecked',
  'unsafe',
  'ushort',
  'using',
  'virtual',
  'void',
  'volatile',
  'while',
];

export class ModelValidators {
  static pascalCase(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    const pascalCaseRegex = /^[A-Z][a-zA-Z0-9]*$/;

    if (!value) {
      return null;
    }

    if (typeof value !== 'string') {
      return { invalidPascalCase: true };
    }

    if (!pascalCaseRegex.test(control.value)) {
      return { invalidPascalCase: true };
    }

    return null;
  }

  static reservedKeyword(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (!value) {
      return null;
    }

    if (CSHARP_KEYWORDS.includes(control.value.toLowerCase())) {
      return { reservedKeyword: true };
    }

    return null;
  }

  static collectionNameIsTaken(
    collectionTypes: Observable<string[]>
  ): (control: AbstractControl) => Observable<ValidationErrors | null> {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);
      }
      return collectionTypes.pipe(
        take(1),
        map((collectionTypes) => {
          return collectionTypes.map((x) => x.toLowerCase()).includes(control.value.toLowerCase())
            ? { collectionNameIsTaken: true }
            : null;
        }),
        catchError(() => of(null))
      );
    };
  }

  static fieldNameIsTaken(
    collectionTypeFields: Observable<ModelField[]>
  ): (control: AbstractControl) => Observable<ValidationErrors | null> {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);
      }
      return collectionTypeFields.pipe(
        take(1),
        map((collectionTypeFields) => {
          return collectionTypeFields.some(
            (x) => x.fieldName.toLowerCase() === control.value.toLowerCase()
          )
            ? { fieldNameIsTaken: true }
            : null;
        }),
        catchError(() => of(null))
      );
    };
  }
}
