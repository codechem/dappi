import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { CommonModule } from '@angular/common';

interface ContentType {
  title: string;
  description: string;
  status?: string;
  lastEdited: string;
}

@Component({
    selector: 'app-recent-content-table',
    imports: [
        MatIconModule,
        MatIconModule,
        MatButtonModule,
        MatCardModule,
        MatTableModule,
        CommonModule,
    ],
    templateUrl: './recent-content-table.component.html',
    styleUrl: './recent-content-table.component.scss'
})
export class RecentContentTableComponent {
  displayedColumns: string[] = [
    'title',
    'description',
    'status',
    'lastEdited',
    'actions',
  ];
  dataSource = new MatTableDataSource<ContentType>([
    {
      title: 'description',
      description: 'description...',
      status: '-',
      lastEdited: '1h ago',
    },
    {
      title: 'Pagination',
      description: 'description...',
      status: '-',
      lastEdited: '3h ago',
    },
    {
      title: 'Cover',
      description: 'description...',
      status: 'Published',
      lastEdited: 'yesterday',
    },
    {
      title: 'author',
      description: 'description...',
      status: 'Published',
      lastEdited: 'yesterday',
    },
    {
      title: 'Published',
      description: 'description...',
      status: '-',
      lastEdited: '3 days ago',
    },
    {
      title: 'Title',
      description: 'description...',
      status: 'Published',
      lastEdited: 'a week ago',
    },
  ]);
}
