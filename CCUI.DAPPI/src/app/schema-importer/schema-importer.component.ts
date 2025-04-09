import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatMenuModule } from '@angular/material/menu';

interface SchemaAttribute {
  name: string;
  type: string;
  target?: string;
  relation?: string;
  description?: string;
  required?: boolean;
  multiple?: boolean;
  allowedTypes?: string[];
  components?: string[];
  targetField?: string;
  maxLength?: number;
  inversedBy?: string;
}

interface SchemaCollection {
  collectionName: string;
  info: {
    singularName: string;
    pluralName: string;
    displayName: string;
    description: string;
  };
  options: {
    draftAndPublish: boolean;
  };
  pluginOptions: any;
  attributes: {
    [key: string]: any;
  };
  kind: string;
  modelType: string;
  modelName: string;
  uid: string;
  globalId: string;
}

@Component({
  selector: 'app-schema-importer',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatMenuModule,
  ],
  templateUrl: './schema-importer.component.html',
  styleUrls: ['./schema-importer.component.scss'],
})
export class SchemaImporterComponent {
  jsonInput: string = '';
  isValidJson: boolean = true;
  validationMessage: string = '';
  parsedSchema: SchemaCollection | null = null;
  attributeList: SchemaAttribute[] = [];
  showJsonPreview: boolean = false;

  sampleSchema: string = `{
    "collectionName": "articles",
    "info": {
      "singularName": "article",
      "pluralName": "articles",
      "displayName": "Article",
      "description": "Create your blog content"
    },
    "options": {
      "draftAndPublish": true
    },
    "pluginOptions": {},
    "attributes": {
      "title": {
        "type": "string"
      },
      "description": {
        "type": "text",
        "maxLength": 80
      },
      "slug": {
        "type": "uid",
        "targetField": "title"
      },
      "cover": {
        "type": "media",
        "multiple": false,
        "required": false,
        "allowedTypes": ["images", "files", "videos"]
      },
      "author": {
        "type": "relation",
        "relation": "manyToOne",
        "target": "api::author.author",
        "inversedBy": "articles"
      },
      "category": {
        "type": "relation",
        "relation": "manyToOne",
        "target": "api::category.category",
        "inversedBy": "articles"
      },
      "blocks": {
        "type": "dynamiczone",
        "components": ["shared.media", "shared.quote", "shared.rich-text", "shared.slider"]
      }
    },
    "kind": "collectionType",
    "modelType": "contentType",
    "modelName": "article",
    "uid": "api::article.article",
    "globalId": "Article"
  }`;

  constructor() {}

  parseJsonInput(): void {
    if (!this.jsonInput.trim()) {
      this.isValidJson = false;
      this.validationMessage = 'Please enter a JSON schema';
      return;
    }

    try {
      this.parsedSchema = JSON.parse(this.jsonInput);
      this.isValidJson = true;
      this.validationMessage = 'Schema parsed successfully';
      this.processSchema();
    } catch (e) {
      this.isValidJson = false;
      this.validationMessage = `Invalid JSON: ${(e as Error).message}`;
      this.parsedSchema = null;
      this.attributeList = [];
    }
  }

  processSchema(): void {
    if (!this.parsedSchema) return;

    this.attributeList = [];
    const attributes = this.parsedSchema.attributes;

    for (const key in attributes) {
      if (attributes.hasOwnProperty(key)) {
        const attr = attributes[key];
        this.attributeList.push({
          name: key,
          type: attr.type,
          description: attr.description,
          required: attr.required,
          multiple: attr.multiple,
          allowedTypes: attr.allowedTypes,
          components: attr.components,
          targetField: attr.targetField,
          maxLength: attr.maxLength,
          target: attr.target,
          relation: attr.relation,
          inversedBy: attr.inversedBy,
        });
      }
    }
  }

  toggleJsonPreview(): void {
    this.showJsonPreview = !this.showJsonPreview;
  }

  getPrettyJson(): string {
    return this.parsedSchema ? JSON.stringify(this.parsedSchema, null, 2) : '';
  }

  getAttributeIcon(type: string): string {
    switch (type) {
      case 'string':
        return 'text_fields';
      case 'text':
        return 'subject';
      case 'media':
        return 'image';
      case 'relation':
        return 'link';
      case 'uid':
        return 'key';
      case 'datetime':
        return 'schedule';
      case 'dynamiczone':
        return 'widgets';
      case 'boolean':
        return 'check_box';
      case 'email':
        return 'email';
      case 'password':
        return 'password';
      case 'integer':
      case 'float':
      case 'decimal':
      case 'number':
        return 'calculate';
      case 'enumeration':
        return 'list';
      case 'json':
        return 'code';
      default:
        return 'data_object';
    }
  }

  getAttributeTypeLabel(attr: SchemaAttribute): string {
    if (attr.type === 'relation' && attr.relation) {
      return `${attr.relation} relation to ${attr.target?.split('.').pop()}`;
    }

    if (attr.type === 'media' && attr.allowedTypes?.length) {
      return `Media (${attr.allowedTypes.join(', ')})`;
    }

    if (attr.type === 'dynamiczone' && attr.components?.length) {
      return `Dynamic Zone (${attr.components.length} components)`;
    }

    if (attr.type === 'text' && attr.maxLength) {
      return `Text (max ${attr.maxLength} chars)`;
    }

    if (attr.type === 'uid' && attr.targetField) {
      return `UID from ${attr.targetField}`;
    }

    return attr.type;
  }

  saveSchema(): void {
    alert('Schema saved successfully!');
  }
}
