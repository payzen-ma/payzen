import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icon/icon.component';

/**
 * Adapter component to render either an exported SVG icon (from /assets/icons)
 * or fall back to the original icon class (e.g. PrimeIcons `pi pi-...`).
 * Usage examples:
 * <app-iconify icon="pi pi-user" size="1rem"></app-iconify>
 */
@Component({
    selector: 'app-iconify',
    standalone: true,
    imports: [CommonModule, IconComponent],
    template: `
    <ng-container *ngIf="svgName; else fallback">
      <app-icon [name]="svgName" [size]="size" [attr.class]="classAttr" aria-hidden="true"></app-icon>
    </ng-container>
    <ng-template #fallback>
      <i [attr.class]="(icon ?? '') + ' ' + classAttr"></i>
    </ng-template>
  `,
    styles: [
        `:host{display:inline-block;vertical-align:middle;line-height:0} i{line-height:0}`
    ]
})
export class IconifyComponent {
    @Input() icon?: string; // e.g. "pi pi-user" or "pi-user"
    @Input() size = '1rem';
    @Input() classAttr = '';

    // compute svg file name from primeicons-like string
    get svgName(): string | null {
        const icon = this.icon ?? '';
        if (!icon) return null;
        // match patterns like 'pi pi-briefcase' or 'pi-briefcase' or 'pi:briefcase'
        const m = icon.match(/pi\s+pi-([a-z0-9-]+)/i) || icon.match(/pi-([a-z0-9-]+)/i) || icon.match(/pi[:_]?([a-z0-9-]+)/i);
        if (m && m[1]) return m[1].replace(/[^a-z0-9-_]/gi, '-').toLowerCase();
        return null;
    }
}
