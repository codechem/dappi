const globals = require('globals');
const typescript = require('@typescript-eslint/eslint-plugin');
const typescriptParser = require('@typescript-eslint/parser');
const angularEslint = require('@angular-eslint/eslint-plugin');
const angularTemplateEslint = require('@angular-eslint/eslint-plugin-template');
const angularParser = require('@angular-eslint/template-parser');
const prettierPlugin = require('eslint-plugin-prettier');
const unusedImportsPlugin = require('eslint-plugin-unused-imports');
const prettierConfig = require('eslint-config-prettier');

module.exports = [
  {
    ignores: [
      '**/.angular/**',
      '**/node_modules/**',
      '**/*.cache/**',
      'dist/**',
      '**/vite/**'
    ]
  },
  
  {
    files: ['**/*.ts'],
    languageOptions: {
      parser: typescriptParser,
      parserOptions: {
        ecmaVersion: 2020,
        sourceType: 'module',
        project: './tsconfig.json',
      },
      globals: {
        ...globals.node,
      },
    },
    plugins: {
      '@typescript-eslint': typescript,
      '@angular-eslint': angularEslint,
      'prettier': prettierPlugin,
      'unused-imports': unusedImportsPlugin,
    },
    rules: {
      'no-undef': 'off',
      ...typescript.configs['eslint-recommended'].rules,
      ...typescript.configs['recommended'].rules,
      ...angularEslint.configs.recommended.rules,
      
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/no-unused-vars': [
        'warn', 
        { 
          argsIgnorePattern: '^_',
          varsIgnorePattern: '^_',
          caughtErrorsIgnorePattern: '^_'
        }
      ],
      '@typescript-eslint/no-non-null-assertion': 'off',
      '@typescript-eslint/no-unnecessary-condition': 'off',
      
      'prettier/prettier': ['error'],
      'unused-imports/no-unused-imports': 'error',
      'unused-imports/no-unused-vars': 'off'
    }
  },
  
  {
    files: ['**/*.html'],
    plugins: {
      '@angular-eslint/template': angularTemplateEslint,
    },
    languageOptions: {
      parser: angularParser,
    },
    rules: {
      ...angularTemplateEslint.configs.recommended.rules,
      '@angular-eslint/contextual-lifecycle': 'off'
    }
  },
  
  {
    files: ['**/*.js'],
    rules: {
      '@typescript-eslint/no-var-requires': 'off',
      'node/no-unsupported-features/es-builtins': 'off'
    }
  },
  
  prettierConfig
];