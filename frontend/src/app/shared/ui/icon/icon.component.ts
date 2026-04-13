import { Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';

/**
 * Simple Icon component that loads SVG from `/assets/icons/{name}.svg`.
 * Usage: <app-icon name="user" size="20px"></app-icon>
 */
@Component({
    selector: 'app-icon',
    standalone: true,
    imports: [CommonModule],
    template: `
    <span class="icon-root" [style.width]="size" [style.height]="size" [innerHTML]="svg"></span>
  `,
    styles: [
        `:host { display: inline-block; vertical-align: middle; line-height: 0; }
        .icon-root svg { width: 100%; height: 100%; display: block; }
        `
    ]
})
export class IconComponent implements OnChanges {
    @Input() name = '';
    @Input() size = '1rem';

    private http = inject(HttpClient);
    private sanitizer = inject(DomSanitizer);
    svg: SafeHtml | null = null;

    ngOnChanges(changes: SimpleChanges) {
        if (changes['name']) this.load();
    }

    private async load() {
        if (!this.name) {
            this.svg = null;
            return;
        }

        const url = `/assets/icons/${this.name}.svg`;
        try {
            const text = await firstValueFrom(this.http.get(url, { responseType: 'text' }));
            this.svg = this.sanitizer.bypassSecurityTrustHtml(text);
        } catch (err) {
            console.warn('Icon not found:', this.name, err instanceof Error ? err.message : err);
            this.svg = null;
        }
    }
}
