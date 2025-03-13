import { Component, HostListener, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { NewRecordFormComponent } from '../new-record-form/new-record-form.component';
import { Location } from '@angular/common';
import { MenuComponent } from '../menu/menu.component';
import { SidebarComponent } from '../sidebar/sidebar.component';

@Component({
  selector: 'app-new-entry',
  standalone: true,
  imports: [
    SidebarComponent,
    MatIconModule,
    MatButtonModule,
    CommonModule,
    NewRecordFormComponent,
    MenuComponent,
  ],
  templateUrl: './new-entry.component.html',
  styleUrl: './new-entry.component.scss',
})
export class NewEntryComponent {
  disabled: boolean = true;
  dateCreated: string = 'yesterday';
  typeName: string = 'Type Name';
  showMenu: boolean = false;
  menuPosition = { top: 0, left: 0 };

  constructor(private location: Location) {}

  goBack() {
    this.location.back();
  }

  toggleMenu(event: MouseEvent): void {
    event.stopPropagation();

    if (this.showMenu) {
      this.showMenu = false;
    } else {
      const buttonRect = (
        event.currentTarget as HTMLElement
      ).getBoundingClientRect();
      this.menuPosition = {
        top: buttonRect.bottom + window.scrollY,
        left: buttonRect.right - 150 + window.scrollX,
      };

      this.showMenu = true;
    }
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.showMenu) {
      this.closeMenu();
    }
  }

  @HostListener('document:keydown.escape')
  onEscapePress(): void {
    if (this.showMenu) {
      this.closeMenu();
    }
  }

  onEdit(item: any): void {
    console.log('Edit item:', item);
    this.closeMenu();
  }

  onDelete(item: any): void {
    console.log('Delete item:', item);
    this.closeMenu();
  }

  closeMenu(): void {
    this.showMenu = false;
  }

  onFormValidityChange(isValid: boolean): void {
    this.disabled = !isValid;
  }
}
