import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LeftMenuComponent } from './left-menu/left-menu.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, LeftMenuComponent, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'CCUI.DAPPI';
}
