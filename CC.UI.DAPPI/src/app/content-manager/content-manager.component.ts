import { Component } from '@angular/core';
import { Sidebar } from '../sidebar/sidebar.component';
import {
  ContentItem,
  ContentTableComponent,
} from '../content-table/content-table.component';
import { MatIconModule } from '@angular/material/icon';
import { ButtonComponent } from '../button/button.component';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';

@Component({
    selector: 'app-content-manager',
    imports: [
        Sidebar,
        ContentTableComponent,
        MatIconModule,
        MatButtonModule,
        ButtonComponent,
        CommonModule,
    ],
    templateUrl: './content-manager.component.html',
    styleUrl: './content-manager.component.scss'
})
export class ContentManagerComponent {
  disabled: boolean = false;
  selectedType: string = 'Article';
  itemsData: ContentItem[] = [
    {
      id: 0,
      title: 'description',
      description: 'description...',
      status: 'Draft',
    },
    {
      id: 7,
      title: 'Pagination',
      description: 'description...',
      status: 'Draft',
    },
    {
      id: 4,
      title: 'Cover',
      description: 'description...',
      status: 'Published',
    },
    {
      id: 5,
      title: 'author',
      description: 'description...',
      status: 'Published',
    },
  ];
}
