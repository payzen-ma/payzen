import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../../../../shared/modal/modal.component';
import {
  LegalParameterDto,
  CreateLegalParameterDto,
  UpdateLegalParameterDto,
  LegalParameterType,
  getLegalParameterTypeLabel
} from '../../../../models/payroll-referentiel';

/**
 * Form model for legal parameter
 */
interface LegalParameterFormModel {
  id?: number;
  name: string;
  description: string;
  value: number | null;
  unit: string;
  effectiveFrom: string;
  effectiveTo: string;
}

/**
 * Legal Parameter Modal Component
 * Form for creating and editing legal parameters
 */
@Component({
  selector: 'app-legal-parameter-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  template: `
    <app-modal 
      [(visible)]="visible" 
      [title]="mode === 'create' ? 'Ajouter un paramètre légal' : 'Modifier le paramètre'"
      (visibleChange)="onVisibleChange($event)">
      
      <form (ngSubmit)="onSubmit()" #paramForm="ngForm" class="space-y-4">
        <!-- Name -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            Nom <span class="text-red-500">*</span>
          </label>
          <input
            type="text"
            [(ngModel)]="formModel.name"
            name="name"
            required
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            placeholder="Ex: SMIG Horaire"
            [class.border-red-300]="nameInput?.invalid && nameInput?.touched"
            #nameInput="ngModel">
          <p *ngIf="nameInput?.invalid && nameInput?.touched" class="mt-1 text-xs text-red-500">
            Le nom est requis
          </p>
        </div>
        
        <!-- Description -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            Description
          </label>
          <textarea
            [(ngModel)]="formModel.description"
            name="description"
            rows="2"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 resize-none"
            placeholder="Description optionnelle du paramètre..."></textarea>
        </div>
        
        <!-- Value and Unit -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Valeur <span class="text-red-500">*</span>
            </label>
            <input
              type="number"
              [(ngModel)]="formModel.value"
              name="value"
              required
              step="any"
              min="0"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Ex: 17.10"
              [class.border-red-300]="valueInput?.invalid && valueInput?.touched"
              #valueInput="ngModel">
            <p *ngIf="valueInput?.invalid && valueInput?.touched" class="mt-1 text-xs text-red-500">
              La valeur est requise et doit être positive
            </p>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Unité <span class="text-red-500">*</span>
            </label>
            <input
              type="text"
              [(ngModel)]="formModel.unit"
              name="unit"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Ex: MAD/heure"
              [class.border-red-300]="unitInput?.invalid && unitInput?.touched"
              #unitInput="ngModel">
            <p *ngIf="unitInput?.invalid && unitInput?.touched" class="mt-1 text-xs text-red-500">
              L'unité est requise
            </p>
          </div>
        </div>
        
        <!-- Common units quick selection -->
        <div class="flex flex-wrap gap-2">
          <span class="text-xs text-gray-500">Unités courantes:</span>
          <button 
            type="button" 
            (click)="formModel.unit = 'MAD/heure'"
            class="px-2 py-0.5 text-xs bg-gray-100 hover:bg-gray-200 rounded transition-colors">
            MAD/heure
          </button>
          <button 
            type="button" 
            (click)="formModel.unit = 'MAD/mois'"
            class="px-2 py-0.5 text-xs bg-gray-100 hover:bg-gray-200 rounded transition-colors">
            MAD/mois
          </button>
          <button 
            type="button" 
            (click)="formModel.unit = 'MAD/jour'"
            class="px-2 py-0.5 text-xs bg-gray-100 hover:bg-gray-200 rounded transition-colors">
            MAD/jour
          </button>
          <button 
            type="button" 
            (click)="formModel.unit = '%'"
            class="px-2 py-0.5 text-xs bg-gray-100 hover:bg-gray-200 rounded transition-colors">
            %
          </button>
          <button 
            type="button" 
            (click)="formModel.unit = 'taux'"
            class="px-2 py-0.5 text-xs bg-gray-100 hover:bg-gray-200 rounded transition-colors">
            taux
          </button>
        </div>
        
        <!-- Effective Period -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Date d'effet <span class="text-red-500">*</span>
            </label>
            <input
              type="date"
              [(ngModel)]="formModel.effectiveFrom"
              name="effectiveFrom"
              required
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              [class.border-red-300]="effectiveFromInput?.invalid && effectiveFromInput?.touched"
              #effectiveFromInput="ngModel">
            <p *ngIf="effectiveFromInput?.invalid && effectiveFromInput?.touched" class="mt-1 text-xs text-red-500">
              La date d'effet est requise
            </p>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              Date de fin
              <span class="text-xs text-gray-400 ml-1">(optionnel)</span>
            </label>
            <input
              type="date"
              [(ngModel)]="formModel.effectiveTo"
              name="effectiveTo"
              [min]="formModel.effectiveFrom"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
            <p class="mt-1 text-xs text-gray-500">
              Laisser vide pour un paramètre en cours
            </p>
          </div>
        </div>
        
        <!-- Validation Errors -->
        <div *ngIf="validationErrors.length > 0" class="p-3 bg-red-50 border border-red-200 rounded-lg">
          <p class="text-sm font-medium text-red-800 mb-1">Erreurs de validation :</p>
          <ul class="text-sm text-red-700 list-disc list-inside">
            <li *ngFor="let error of validationErrors">{{ error }}</li>
          </ul>
        </div>
        
        <!-- Saving indicator -->
        <div *ngIf="saving" class="flex items-center gap-2 text-sm text-gray-600">
          <svg class="w-4 h-4 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
          </svg>
          Enregistrement en cours...
        </div>
        
        <!-- Actions -->
        <div class="flex justify-end gap-3 pt-4 border-t border-gray-200">
          <button
            type="button"
            (click)="onCancel()"
            [disabled]="saving"
            class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50">
            Annuler
          </button>
          <button
            type="submit"
            [disabled]="paramForm.invalid || saving"
            class="px-4 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed">
            {{ mode === 'create' ? 'Créer' : 'Enregistrer' }}
          </button>
        </div>
      </form>
    </app-modal>
  `
})
export class LegalParameterModalComponent implements OnChanges {
  @Input() visible = false;
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() parameter: LegalParameterDto | null = null;
  
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() save = new EventEmitter<CreateLegalParameterDto | { id: number; dto: UpdateLegalParameterDto }>();
  @Output() cancel = new EventEmitter<void>();
  
  // Form model
  formModel: LegalParameterFormModel = this.getEmptyFormModel();
  
  // State
  saving = false;
  validationErrors: string[] = [];
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.initForm();
    }
    if (changes['parameter'] && this.parameter) {
      this.initForm();
    }
  }
  
  /**
   * Initialize form with parameter data or empty
   */
  private initForm(): void {
    this.validationErrors = [];
    this.saving = false;
    
    if (this.mode === 'edit' && this.parameter) {
      this.formModel = {
        id: this.parameter.id,
        name: this.parameter.name,
        description: this.parameter.description || '',
        value: this.parameter.value,
        unit: this.parameter.unit,
        effectiveFrom: this.parameter.effectiveFrom,
        effectiveTo: this.parameter.effectiveTo || ''
      };
    } else {
      this.formModel = this.getEmptyFormModel();
    }
  }
  
  /**
   * Get empty form model
   */
  private getEmptyFormModel(): LegalParameterFormModel {
    // Default effective date to today
    const today = new Date().toISOString().split('T')[0];
    
    return {
      name: '',
      description: '',
      value: null,
      unit: '',
      effectiveFrom: today,
      effectiveTo: ''
    };
  }
  
  /**
   * Validate form data
   */
  private validate(): boolean {
    this.validationErrors = [];
    
    if (!this.formModel.name?.trim()) {
      this.validationErrors.push('Le nom est requis');
    }
    
    if (this.formModel.value === null || this.formModel.value === undefined) {
      this.validationErrors.push('La valeur est requise');
    } else if (this.formModel.value < 0) {
      this.validationErrors.push('La valeur doit être positive ou nulle');
    }
    
    if (!this.formModel.unit?.trim()) {
      this.validationErrors.push("L'unité est requise");
    }
    
    if (!this.formModel.effectiveFrom) {
      this.validationErrors.push("La date d'effet est requise");
    }
    
    if (this.formModel.effectiveTo && this.formModel.effectiveFrom) {
      const from = new Date(this.formModel.effectiveFrom);
      const to = new Date(this.formModel.effectiveTo);
      if (to <= from) {
        this.validationErrors.push("La date de fin doit être postérieure à la date d'effet");
      }
    }
    
    return this.validationErrors.length === 0;
  }
  
  /**
   * Submit form
   */
  onSubmit(): void {
    if (!this.validate()) {
      return;
    }
    
    this.saving = true;
    
    if (this.mode === 'create') {
      const createDto: CreateLegalParameterDto = {
        name: this.formModel.name.trim(),
        description: this.formModel.description?.trim() || undefined,
        value: this.formModel.value!,
        unit: this.formModel.unit.trim(),
        effectiveFrom: this.formModel.effectiveFrom,
        effectiveTo: this.formModel.effectiveTo || undefined
      };
      this.save.emit(createDto);
    } else {
      const updateDto: UpdateLegalParameterDto = {
        name: this.formModel.name.trim(),
        description: this.formModel.description?.trim() || undefined,
        value: this.formModel.value!,
        unit: this.formModel.unit.trim(),
        effectiveFrom: this.formModel.effectiveFrom,
        effectiveTo: this.formModel.effectiveTo || undefined
      };
      this.save.emit({ id: this.formModel.id!, dto: updateDto });
    }
  }
  
  /**
   * Cancel and close modal
   */
  onCancel(): void {
    this.visibleChange.emit(false);
    this.cancel.emit();
  }

  onVisibleChange(visible: boolean): void {
    this.visibleChange.emit(visible);
  }
  
  /**
   * Reset saving state (called by parent after API call completes)
   */
  resetSaving(): void {
    this.saving = false;
  }
  
  /**
   * Set error from API response
   */
  setError(error: string): void {
    this.saving = false;
    this.validationErrors = [error];
  }
}
