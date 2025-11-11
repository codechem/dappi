import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { EnumManagementService } from '../services/common/enum-management.service';
import { EnumsResponse } from '../models/enums-response.model';
import { CreateEnumDialogComponent } from '../create-enum-dialog/create-enum-dialog.component';
import { EditEnumDialogComponent } from '../edit-enum-dialog/edit-enum-dialog.component';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { ViewEnumCodeDialogComponent } from '../view-enum-code-dialog/view-enum-code-dialog.component';

interface EnumDisplayData {
  name: string;
  values: { name: string; value: number }[];
  valueCount: number;
}

@Component({
  selector: 'app-enum-manager',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './enum-manager.component.html',
  styleUrl: './enum-manager.component.scss',
})
export class EnumManagerComponent implements OnInit, OnDestroy {
  enums: EnumDisplayData[] = [];
  displayedColumns: string[] = ['name', 'valueCount', 'actions'];
  loading = false;
  private subscription = new Subscription();

  constructor(
    private enumsService: EnumManagementService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    this.loadEnums();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  loadEnums(): void {
    this.loading = true;
    this.subscription.add(
      this.enumsService.getAllEnums().subscribe({
        next: (response: EnumsResponse) => {
          this.enums = Object.keys(response).map((enumName) => {
            const enumValues = response[enumName];
            return {
              name: enumName,
              values: enumValues,
              valueCount: enumValues.length,
            };
          });
          this.loading = false;
        },
        error: (error) => {
          console.error('Failed to load enums:', error);
          this.snackBar.open('Failed to load enums', 'Close', { duration: 3000 });
          this.loading = false;
        },
      })
    );
  }

  createEnum(): void {
    const dialogRef = this.dialog.open(CreateEnumDialogComponent, {
      width: '600px',
      disableClose: true,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.loadEnums();
        this.snackBar.open('Enum created successfully', 'Close', { duration: 3000 });
      }
    });
  }

  editEnum(enumData: EnumDisplayData): void {
    const dialogRef = this.dialog.open(EditEnumDialogComponent, {
      width: '600px',
      disableClose: true,
      data: enumData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.loadEnums();
        this.snackBar.open('Enum updated successfully', 'Close', { duration: 3000 });
      }
    });
  }

  deleteEnum(enumData: EnumDisplayData): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Enum',
        message: `Are you sure you want to delete the enum "${enumData.name}"? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (confirmed) {
        this.subscription.add(
          this.enumsService.deleteEnum(enumData.name).subscribe({
            next: () => {
              this.loadEnums();
              this.snackBar.open('Enum deleted successfully', 'Close', { duration: 3000 });
            },
            error: (error) => {
              console.error('Failed to delete enum:', error);
              this.snackBar.open('Failed to delete enum', 'Close', { duration: 3000 });
            },
          })
        );
      }
    });
  }

  viewEnum(enumData: EnumDisplayData): void {
    this.dialog.open(ViewEnumCodeDialogComponent, {
      width: '700px',
      data: enumData,
    });
  }

  getValuesList(values: { name: string; value: number }[]): string {
    return values.map((v) => `${v.name} (${v.value})`).join(', ');
  }
}