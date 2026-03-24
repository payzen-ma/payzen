import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { BreadcrumbService } from '@app/core/services/breadcrumb.service';

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterModule, BreadcrumbModule],
  template: `
    <div class="px-6 pt-4 pb-0" *ngIf="items().length > 1">
      <p-breadcrumb [model]="items()" [home]="home"></p-breadcrumb>
    </div>
  `,
  styles: [`
    :host ::ng-deep .p-breadcrumb {
      background: transparent;
      padding: 0;
      border: none;
    }
    :host ::ng-deep .p-breadcrumb ul li .p-menuitem-link .p-menuitem-text {
      font-size: 0.875rem;
    }
  `]
})
export class BreadcrumbComponent {
  private service = inject(BreadcrumbService);
  
  readonly items = this.service.items;
  readonly home = { icon: 'pi pi-home', routerLink: '/' };
}
