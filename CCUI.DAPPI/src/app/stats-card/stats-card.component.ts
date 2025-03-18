import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Input,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { AddCollectionTypeDialogComponent } from '../add-collection-type-dialog/add-collection-type-dialog.component';
import { HttpClient } from '@angular/common/http';
import { catchError, finalize, of, Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-stats-card',
  imports: [MatIconModule],
  templateUrl: './stats-card.component.html',
  styleUrl: './stats-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatsCardComponent implements OnDestroy, OnInit {
  @Input() icon!: string;
  @Input() value!: string;
  @Input() title!: string;
  isLoading = false;

  numberOfCollectionTypes: number = 0;
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private dialog: MatDialog,
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.fetchContentTypes();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  openPopup(): void {
    this.router.navigate(['/builder']).then(() => {
      const dialogRef = this.dialog.open(AddCollectionTypeDialogComponent, {
        width: '450px',
        panelClass: 'dark-theme-dialog',
        disableClose: true,
      });

      dialogRef.afterClosed().subscribe((result) => {
        if (result && result.success) {
        }
      });
    });
  }

  fetchContentTypes(): void {
    this.isLoading = true;

    this.http
      .get<string[]>('http://localhost:5101/api/models')
      .pipe(
        takeUntil(this.destroy$),
        catchError((error) => {
          console.error('Error fetching collection types:', error);
          return of([]);
        }),
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe((data) => {
        this.numberOfCollectionTypes = data.length;
        console.log(this.numberOfCollectionTypes);
        this.cdr.detectChanges();
        // this.collectionTypes = data;
        // this.filteredCollectionTypes = [...this.collectionTypes];

        // this.selectedContentService.currentSelectedType
        //   .pipe(takeUntil(this.destroy$))
        //   .subscribe((selectedType) => {
        //     if (this.collectionTypes.includes(selectedType)) {
        //       this.selectedType = selectedType;
        //       this.collectionTypeSelected.emit(selectedType);
        //     } else {
        //       this.selectCollectionType(this.collectionTypes[0]);
        //     }
        //   });
      });
  }
}
