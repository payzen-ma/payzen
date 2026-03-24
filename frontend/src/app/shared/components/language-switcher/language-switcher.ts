import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { LanguageService } from '../../utils/language.service';
import { Language } from '../../utils/translation.config';

interface LanguageOption {
  label: string;
  value: Language;
  flag: string;
}

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule, Select, FormsModule],
  template: `
    <p-select
      [options]="languages"
      [(ngModel)]="selectedLanguage"
      optionLabel="label"
      optionValue="value"
      (onChange)="onLanguageChange($event)"
      [style]="{'min-width': '150px'}"
      styleClass="language-dropdown"
    >
      <ng-template pTemplate="selectedItem" let-option>
        <div class="flex items-center gap-2" *ngIf="option">
          <span>{{ option.flag }}</span>
          <span>{{ option.label }}</span>
        </div>
      </ng-template>
      <ng-template pTemplate="item" let-option>
        <div class="flex items-center gap-2">
          <span>{{ option.flag }}</span>
          <span>{{ option.label }}</span>
        </div>
      </ng-template>
    </p-select>
  `,
  styles: []
})
export class LanguageSwitcher {
  private languageService = inject(LanguageService);

  selectedLanguage: Language = this.languageService.getCurrentLanguage();

  languages: LanguageOption[] = [
    { label: 'English', value: 'en', flag: '🇬🇧' },
    { label: 'العربية', value: 'ar', flag: '🇲🇦' },
    { label: 'Français', value: 'fr', flag: '🇫🇷' }
  ];

  onLanguageChange(event: any): void {
    const newLang = event.value as Language;
    this.languageService.setLanguage(newLang);
    this.selectedLanguage = newLang;
  }
}

