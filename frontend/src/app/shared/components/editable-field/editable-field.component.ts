import { Component, EventEmitter, Input, Output, signal, inject, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { AutoCompleteModule } from 'primeng/autocomplete';

@Component({
  selector: 'app-editable-field',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    ButtonModule,
    TooltipModule,
    AutoCompleteModule
  ],
  template: `
    <div class="editable-field-wrapper">
      <!-- Label -->
      <label *ngIf="label" class="block mb-2 text-sm font-medium text-gray-700">
        {{ label }}
      </label>

      <div class="editable-field-container group relative flex items-center gap-2 min-h-[40px]">
        <!-- View Mode -->
        <div *ngIf="!isEditing()" 
             (click)="startEditing()"
             class="flex-1 py-2 px-3 rounded cursor-pointer hover:bg-gray-50 border border-transparent hover:border-gray-200 transition-colors flex items-center justify-between bg-white"
             [class.text-gray-400]="!value">
          <span class="truncate">
            {{ value || emptyPlaceholder }}
          </span>
          <i class="pi pi-pencil text-gray-400 opacity-0 group-hover:opacity-100 transition-opacity"></i>
        </div>

        <!-- Edit Mode -->
        <div *ngIf="isEditing()" class="flex-1 flex items-center gap-2 animate-fade-in w-full">
          <div class="flex-1 min-w-0">
            <ng-container [ngSwitch]="type">
              <!-- Autocomplete Input -->
              <p-autoComplete *ngSwitchCase="'autocomplete'"
                [(ngModel)]="tempValue"
                [suggestions]="suggestions"
                (completeMethod)="onSearch($event)"
                (keydown.enter)="onSave()"
                (keydown.escape)="onCancel()"
                [placeholder]="label"
                [forceSelection]="false"
                [dropdown]="false"
                [showEmptyMessage]="false"
                styleClass="w-full"
                inputStyleClass="w-full p-inputtext-sm"
                appendTo="body"
                >
              </p-autoComplete>

              <!-- Standard Input -->
              <input *ngSwitchDefault
                pInputText 
                [type]="type" 
                [(ngModel)]="tempValue" 
                (keydown.enter)="onSave()"
                (keydown.escape)="onCancel()"
                class="w-full p-inputtext-sm"
                [placeholder]="label"
                
              />
            </ng-container>
          </div>

          <div class="flex items-center gap-1 shrink-0">
            <button 
              pButton 
              icon="pi pi-check" 
              class="p-button-rounded p-button-text p-button-success p-button-sm w-8 h-8"
              (click)="onSave()"
              pTooltip="Save"
              tooltipPosition="top">
            </button>
            <button 
              pButton 
              icon="pi pi-times" 
              class="p-button-rounded p-button-text p-button-secondary p-button-sm w-8 h-8"
              (click)="onCancel()"
              pTooltip="Cancel"
              tooltipPosition="top">
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    .animate-fade-in {
      animation: fadeIn 0.2s ease-in-out;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(-2px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class EditableFieldComponent {
  @Input() value: string | number | null | undefined = '';
  @Input() label: string = '';
  @Input() type: string = 'text';
  @Input() emptyPlaceholder: string = '—';
  @Input() suggestions: any[] = [];
  
  @Output() save = new EventEmitter<string | number>();
  @Output() cancel = new EventEmitter<void>();
  @Output() search = new EventEmitter<any>();

  isEditing = signal(false);
  tempValue: string | number | null | undefined = '';
  private host = inject(ElementRef<HTMLElement>);

  startEditing() {
    this.tempValue = this.value;
    this.isEditing.set(true);
    // Focus the first input inside the component after it enters edit mode.
    setTimeout(() => {
      try {
        const root = this.host.nativeElement as HTMLElement;
        const input = root.querySelector('input, textarea, .p-inputtext');
        if (input && (input as HTMLInputElement).focus) {
          (input as HTMLInputElement).focus();
        }
      } catch (e) {
        // ignore focus errors
      }
    }, 0);
  }

  onSearch(event: any) {
    this.search.emit(event);
  }

  onSave() {
    if (this.tempValue !== this.value) {
      this.save.emit(this.tempValue ?? '');
    } else {
      this.cancel.emit();
    }
    this.isEditing.set(false);
  }

  onCancel() {
    this.isEditing.set(false);
    this.cancel.emit();
  }
}
