import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddFieldDialogComponent } from './add-field-dialog.component';

describe('AddFieldDialogComponent', () => {
  let component: AddFieldDialogComponent;
  let fixture: ComponentFixture<AddFieldDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddFieldDialogComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AddFieldDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
