import { Component } from '@angular/core';

@Component({
  selector: 'app-sidebar-group',
  standalone: true,
  template: `
    <div class="sidebar-group mb-4">
      <ng-content></ng-content>
    </div>
  `,
  styles: [`
    .sidebar-group {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
  `]
})
export class SidebarGroupComponent {}
