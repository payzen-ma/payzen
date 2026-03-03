import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SalaryPackageStatus, TemplateType, RegulationVersion } from '../../../../../models/salary-package.model';

@Component({
  selector: 'app-template-badges',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-wrap items-center gap-2">
      <!-- Template Type Badge (Official/Company) -->
      <span *ngIf="showType" 
            class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold"
            [ngClass]="templateType === 'OFFICIAL' 
              ? 'bg-blue-50 text-blue-700 border border-blue-200' 
              : 'bg-slate-50 text-slate-600 border border-slate-200'">
        <svg *ngIf="templateType === 'OFFICIAL'" class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd" />
        </svg>
        <svg *ngIf="templateType === 'COMPANY'" class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M4 4a2 2 0 012-2h8a2 2 0 012 2v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4zm3 1h6v4H7V5zm6 6H7v2h6v-2z" clip-rule="evenodd" />
        </svg>
        {{ templateType === 'OFFICIAL' ? 'Officiel' : 'Entreprise' }}
      </span>

      <!-- Status Badge -->
      <span *ngIf="showStatus"
            class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border"
            [ngClass]="getStatusClasses()">
        <span class="w-1.5 h-1.5 rounded-full" [ngClass]="getStatusDotClass()"></span>
        {{ getStatusLabel() }}
      </span>

      <!-- Regulation Badge -->
      <span *ngIf="showRegulation"
            class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-purple-50 text-purple-700 border border-purple-200">
        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        {{ getRegulationLabel() }}
      </span>

      <!-- Locked Badge -->
      <span *ngIf="isLocked"
            class="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-600 border border-gray-200">
        <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd" />
        </svg>
        Verrouillé
      </span>

      <!-- Version Badge -->
      <span *ngIf="showVersion && version > 1"
            class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-600 border border-gray-200">
        v{{ version }}
      </span>
    </div>
  `
})
export class TemplateBadgesComponent {
  @Input() templateType: TemplateType = 'OFFICIAL';
  @Input() status: SalaryPackageStatus = 'draft';
  @Input() regulationVersion: RegulationVersion = 'MA_2025';
  @Input() isLocked = false;
  @Input() version = 1;
  @Input() showType = true;
  @Input() showStatus = true;
  @Input() showRegulation = true;
  @Input() showVersion = true;

  getStatusClasses(): string {
    switch (this.status) {
      case 'published':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'deprecated':
        return 'bg-amber-50 text-amber-700 border-amber-200';
      default:
        return 'bg-slate-50 text-slate-600 border-slate-200';
    }
  }

  getStatusDotClass(): string {
    switch (this.status) {
      case 'published':
        return 'bg-emerald-500';
      case 'deprecated':
        return 'bg-amber-500';
      default:
        return 'bg-slate-400';
    }
  }

  getStatusLabel(): string {
    switch (this.status) {
      case 'published':
        return 'Publié';
      case 'deprecated':
        return 'Obsolète';
      default:
        return 'Brouillon';
    }
  }

  getRegulationLabel(): string {
    switch (this.regulationVersion) {
      case 'MA_2025':
        return 'Maroc 2025';
      default:
        return this.regulationVersion;
    }
  }
}
