import { Component } from '@angular/core';

@Component({
  selector: 'app-sidebar-group-label',
  standalone: true,
  template: `
    <div class="sidebar-group-label mb-2 px-3 text-xs font-semibold uppercase text-gray-500 tracking-wide">
      <ng-content></ng-content>
    </div>
  `
})
export class SidebarGroupLabelComponent {}
