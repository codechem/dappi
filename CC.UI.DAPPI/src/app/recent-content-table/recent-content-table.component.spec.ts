import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecentContentTableComponent } from './recent-content-table.component';

describe('RecentContentTableComponent', () => {
  let component: RecentContentTableComponent;
  let fixture: ComponentFixture<RecentContentTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecentContentTableComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(RecentContentTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
