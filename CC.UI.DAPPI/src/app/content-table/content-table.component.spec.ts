import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ContentTableComponent } from './content-table.component';

describe('ContentTableComponent', () => {
  let component: ContentTableComponent;
  let fixture: ComponentFixture<ContentTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContentTableComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ContentTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
