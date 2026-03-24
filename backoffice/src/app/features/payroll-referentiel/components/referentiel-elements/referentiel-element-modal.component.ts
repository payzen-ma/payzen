import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../../../../shared/modal/modal.component';
import { LookupCacheService } from '../../../../services/payroll-referentiel/lookup-cache.service';
import {
  ReferentielElementListDto,
  ReferentielElementDto,
  CreateReferentielElementDto,
  UpdateReferentielElementDto,
  ElementCategoryDto,
  PaymentFrequency,
  ElementStatus
} from '../../../../models/payroll-referentiel';

/**
 * Referential Element Modal Component
 * Create/edit compensation elements
 */
@Component({
  selector: 'app-referentiel-element-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  template: `
    <app-modal [(visible)]="visible" [title]="modalTitle" (visibleChange)="onVisibleChange($event)">
      <div class="space-y-6">
        <!-- Error Message -->
        <div *ngIf="error" class="p-3 bg-red-50 border border-red-200 rounded-lg">
          <p class="text-sm text-red-600">{{ error }}</p>
        </div>

        <!-- Form Fields -->
        <div class="space-y-4">
          <!-- Step 1: Category (first) -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Catégorie <span class="text-red-500">*</span>
            </label>
            <div *ngIf="!showNewCategoryInput">
              <select
                [(ngModel)]="form.categoryId"
                (ngModelChange)="onCategoryChange($event)"
                class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
                <option [ngValue]="null">Sélectionner une catégorie</option>
                <option *ngFor="let cat of categories" [ngValue]="cat.id">{{ cat.name }}</option>
                <option [ngValue]="-1">+ Créer une nouvelle catégorie</option>
              </select>
            </div>
            <div *ngIf="showNewCategoryInput" class="space-y-2">
              <div class="flex gap-2">
                <input
                  type="text"
                  [(ngModel)]="newCategoryName"
                  class="flex-1 px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  placeholder="Nom de la nouvelle catégorie">
                <button
                  type="button"
                  (click)="onCreateCategory()"
                  [disabled]="!newCategoryName.trim() || creatingCategory"
                  class="px-3 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors">
                  {{ creatingCategory ? '...' : 'Créer' }}
                </button>
                <button
                  type="button"
                  (click)="onCancelNewCategory()"
                  class="px-3 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
                  Annuler
                </button>
              </div>
              <p *ngIf="newCategoryError" class="text-xs text-red-500">{{ newCategoryError }}</p>
            </div>
          </div>

          <!-- Code (optional) -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Code</label>
            <input
              type="text"
              [(ngModel)]="form.code"
              class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 font-mono"
              placeholder="ex: TRANSPORT_DOMICILE"
              maxlength="100">
            <p class="mt-1 text-xs text-gray-500">Code unique pour référencer cet élément (optionnel, max 100 caractères)</p>
          </div>

          <!-- Step 2: Name -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Nom <span class="text-red-500">*</span>
            </label>
            <input
              type="text"
              [(ngModel)]="form.name"
              class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="ex: Indemnité de transport">
          </div>

          <!-- Frequency -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Fréquence par défaut <span class="text-red-500">*</span>
            </label>
            <select
              [(ngModel)]="form.defaultFrequency"
              class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
              <option *ngFor="let freq of frequencies" [value]="freq.value">{{ freq.label }}</option>
            </select>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              [(ngModel)]="form.description"
              rows="3"
              class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Description détaillée de cet élément..."></textarea>
          </div>

          <!-- Status (edit mode only) -->
          <div *ngIf="mode === 'edit'" class="flex items-center gap-3">
            <label class="flex items-center gap-2">
              <input 
                type="checkbox" 
                [(ngModel)]="form.isActive"
                class="w-4 h-4 text-primary-500 border-gray-300 rounded focus:ring-primary-500">
              <span class="text-sm text-gray-700">Élément actif</span>
            </label>
            <p class="text-xs text-gray-400">Un élément inactif ne peut pas être utilisé dans les nouveaux packages</p>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex justify-end gap-3 pt-4 border-t">
          <button 
            type="button"
            (click)="onCancel()"
            class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
            Annuler
          </button>
          <button 
            type="button"
            (click)="onSubmit()"
            [disabled]="!isValid()"
            class="px-4 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors">
            {{ mode === 'create' ? 'Créer' : 'Enregistrer' }}
          </button>
        </div>
      </div>
    </app-modal>
  `
})
export class ReferentielElementModalComponent implements OnInit, OnChanges {
  @Input() visible = false;
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() element: ReferentielElementListDto | ReferentielElementDto | null = null;
  
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() save = new EventEmitter<CreateReferentielElementDto | { id: number; dto: UpdateReferentielElementDto }>();

  categories: ElementCategoryDto[] = [];
  showNewCategoryInput = false;
  newCategoryName = '';
  newCategoryError = '';
  creatingCategory = false;

  form = {
    code: '',
    name: '',
    categoryId: null as number | null,
    description: '',
    defaultFrequency: PaymentFrequency.MONTHLY,
    isActive: true
  };

  error = '';

  frequencies = [
    { value: PaymentFrequency.DAILY, label: 'Journalier' },
    { value: PaymentFrequency.MONTHLY, label: 'Mensuel' },
    { value: PaymentFrequency.QUARTERLY, label: 'Trimestriel' },
    { value: PaymentFrequency.ANNUAL, label: 'Annuel' },
    { value: PaymentFrequency.ONE_TIME, label: 'Ponctuel' }
  ];

  get modalTitle(): string {
    return this.mode === 'create' ? 'Nouvel élément de référentiel' : 'Modifier l\'élément';
  }

  constructor(private lookupCache: LookupCacheService) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.initForm();
    }
  }

  private loadCategories(): void {
    this.lookupCache.getCategories().subscribe({
      next: (cats) => this.categories = cats,
      error: (err) => console.error('Failed to load categories:', err)
    });
  }

  private initForm(): void {
    this.error = '';

    if (this.mode === 'edit' && this.element) {
      this.form = {
        code: this.element.code || '',
        name: this.element.name,
        categoryId: 'categoryId' in this.element ? this.element.categoryId : null,
        description: 'description' in this.element ? (this.element.description || '') : '',
        defaultFrequency: this.element.defaultFrequency,
        isActive: this.element.isActive
      };

      // If we only have list dto, try to find category id by name
      if (!this.form.categoryId && this.element.categoryName) {
        const cat = this.categories.find(c => c.name === this.element!.categoryName);
        if (cat) this.form.categoryId = cat.id;
      }
    } else {
      this.form = {
        code: '',
        name: '',
        categoryId: null,
        description: '',
        defaultFrequency: PaymentFrequency.MONTHLY,
        isActive: true
      };
    }
  }

  isValid(): boolean {
    return !!(
      this.form.name.trim() && 
      this.form.categoryId !== null
    );
  }

  setError(message: string): void {
    this.error = message;
  }

  onCategoryChange(value: number | null): void {
    if (value === -1) {
      this.showNewCategoryInput = true;
      this.form.categoryId = null;
      this.newCategoryName = '';
      this.newCategoryError = '';
    }
  }

  onCreateCategory(): void {
    if (!this.newCategoryName.trim() || this.creatingCategory) return;
    this.creatingCategory = true;
    this.newCategoryError = '';

    this.lookupCache.createCategory(this.newCategoryName).subscribe({
      next: (cat) => {
        this.categories = [...this.categories, cat];
        this.form.categoryId = cat.id;
        this.showNewCategoryInput = false;
        this.newCategoryName = '';
        this.creatingCategory = false;
      },
      error: (err) => {
        this.newCategoryError = err?.error?.Message || err?.error?.message || 'Erreur lors de la création.';
        this.creatingCategory = false;
      }
    });
  }

  onCancelNewCategory(): void {
    this.showNewCategoryInput = false;
    this.newCategoryName = '';
    this.newCategoryError = '';
    this.form.categoryId = null;
  }

  onSubmit(): void {
    if (!this.isValid()) return;

    if (this.mode === 'create') {
      const createDto: CreateReferentielElementDto = {
        code: this.form.code.trim() || undefined,
        name: this.form.name.trim(),
        categoryId: this.form.categoryId!,
        description: this.form.description.trim() || undefined,
        defaultFrequency: this.form.defaultFrequency
      };
      this.save.emit(createDto);
    } else {
      const updateDto: UpdateReferentielElementDto = {
        code: this.form.code.trim() || undefined,
        name: this.form.name.trim(),
        categoryId: this.form.categoryId!,
        description: this.form.description.trim() || undefined,
        defaultFrequency: this.form.defaultFrequency,
        isActive: this.form.isActive
      };
      this.save.emit({ id: this.element!.id, dto: updateDto });
    }
  }

  onCancel(): void {
    this.visibleChange.emit(false);
  }

  onVisibleChange(visible: boolean): void {
    this.visibleChange.emit(visible);
  }
}
