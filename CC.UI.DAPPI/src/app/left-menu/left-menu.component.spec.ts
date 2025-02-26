import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LeftMenu } from './left-menu.component';

describe('LeftMenu', () => {
  let component: LeftMenu;
  let fixture: ComponentFixture<LeftMenu>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeftMenu]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(LeftMenu);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
