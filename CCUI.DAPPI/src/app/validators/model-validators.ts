import {AbstractControl, ValidationErrors} from '@angular/forms';

export class ModelValidators {
  private static csharpKeywords = [
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

  static pascalCase(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    const pascalCaseRegex = /^[A-Z][a-zA-Z0-9]*$/;

    if (!value) {
      return null;
    }

    if (typeof value !== 'string') {
     return {invalidPascalCase: true};
    }

    if (!pascalCaseRegex.test(control.value)) {
      return {invalidPascalCase: true};
    }

    return null;
  }

  static reservedKeyword(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (!value) {
      return null;
    }

    if (ModelValidators.csharpKeywords.includes(control.value.toLowerCase())) {
      return {reservedKeyword: true};
    }

    return null;

  }
}
