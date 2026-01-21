import { AbstractControl, ValidationErrors } from '@angular/forms';
import { Observable, of, take } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ModelField } from '../models/content.model';
import { ModelResponse } from '../models/content.model'; 
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

const INT32_MAX = 2147483647;

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
    collectionTypeFields: Observable<ModelField[] | undefined>,
    excludeFieldName?: string
  ): (control: AbstractControl) => Observable<ValidationErrors | null> {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);
      }
      return collectionTypeFields.pipe(
        take(1),
        map((fields) => {
          return fields?.some(
            (x) => x.fieldName.toLowerCase() === control.value.toLowerCase() &&
                   x.fieldName.toLowerCase() !== excludeFieldName?.toLowerCase()
          )
            ? { fieldNameIsTaken: true }
            : null;
        }),
        catchError(() => of(null))
      );
    };
  }

  static fieldNameSameAsModel(modelName:string) {
    return (control: AbstractControl) : ValidationErrors | null => {
      const value = control.value;

      if (!value) {
        return null;
      }
      if (value === modelName) {
        return { fieldNameSameAsModel: true };
      }

      return null;
      }
  }

  static validRegex(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (!value) {
      return null;
    }

    if (typeof value !== 'string') {
      return { invalidRegex: true };
    }

    try {
      const trimmed = value.trim();
      if (trimmed.startsWith('/') && trimmed.lastIndexOf('/') > 0) {
        const lastSlash = trimmed.lastIndexOf('/');
        const pattern = trimmed.substring(1, lastSlash);
        const flags = trimmed.substring(lastSlash + 1);
        new RegExp(pattern, flags);
      } else {
        new RegExp(trimmed);
      }
      return null;
    } catch (e) {
      return { invalidRegex: true };
    }
  }

  static validMinTextValue(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (value === null || value === undefined || value === '') {
      return null;
    }

    const numValue = Number(value);

    if (!Number.isInteger(numValue) || numValue < 0) {
      return { invalidMinTextValue: true };
    }

    return null;
  }

  static validMaxTextValue(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (value === null || value === undefined || value === '') {
      return null;
    }

    const numValue = Number(value);

    if (!Number.isInteger(numValue) || numValue < 0) {
      return { invalidMaxTextValue: true };
    }

    return null;
  }

  static validNumericInput(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    const valueString = String(value);
    const numericRegex = /^-?\d+(\.\d+)?$/;

    if (!numericRegex.test(valueString)) {
      return { invalidNumberInput: true };
    }

    return null;
  }

  static validMinValue(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (value === null || value === undefined || value === '') {
      return null;
    }

    const numValue = Number(value);

    if (isNaN(numValue)) {
      return { invalidMinValue: true };
    }

    return null;
  }

  static validMaxValue(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (value === null || value === undefined || value === '') {
      return null;
    }

    const numValue = Number(value);

    if (isNaN(numValue)) {
      return { invalidMaxValue: true };
    }

    return null;
  }

  static minMaxValueValidator(control: AbstractControl): ValidationErrors | null {
    const minValue = control.get('min')?.value;
    const maxValue = control.get('max')?.value;

    if (minValue !== null && minValue !== undefined && minValue !== '' &&
        maxValue !== null && maxValue !== undefined && maxValue !== '' &&
        Number(minValue) > Number(maxValue)) {
      return { minValueGreaterThanMaxValue: true };
    }

    return null;
  }
}