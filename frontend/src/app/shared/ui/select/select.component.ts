import { Component, Input, Output, EventEmitter, computed, signal, forwardRef, inject, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';

export interface SelectOption {
  label: string;
  value: any;
  disabled?: boolean;
}

@Component({
  selector: 'app-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectComponent),
      multi: true
    }
  ],
  template: `
    <div class="flex flex-col gap-1.5">
      @if (label) {
        <label [for]="id" class="text-sm font-medium text-gray-700">
          {{ label }}
          @if (required) {
            <span class="text-red-500 ml-0.5">*</span>
          }
        </label>
      }

      <div class="relative">
        @if (searchable) {
          <!-- Searchable dropdown -->
          <div class="relative">
            <button
              type="button"
              [id]="id"
              [disabled]="disabled"
              [class]="selectClasses()"
              (click)="toggleDropdown()">
              <span class="block truncate text-left">
                {{ selectedLabel() || placeholder }}
              </span>
              <svg class="absolute right-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400 pointer-events-none"
                   fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
              </svg>
            </button>

            @if (isOpen()) {
              <div class="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-lg shadow-lg max-h-60 overflow-auto">
                @if (searchable) {
                  <div class="p-2 border-b border-gray-200">
                    <input
                      type="text"
                      class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-200 focus:border-indigo-500"
                      placeholder="Rechercher..."
                      [(ngModel)]="searchTerm"
                      (click)="$event.stopPropagation()"
                    />
                  </div>
                }

                <div class="py-1">
                  @for (option of filteredOptions(); track option.value) {
                    <button
                      type="button"
                      [disabled]="option.disabled"
                      class="w-full px-3 py-2 text-left hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed transition"
                      [class.bg-indigo-50]="option.value === value"
                      [class.text-indigo-700]="option.value === value"
                      [class.font-medium]="option.value === value"
                      (click)="selectOption(option)">
                      {{ option.label }}
                    </button>
                  } @empty {
                    <div class="px-3 py-2 text-sm text-gray-500">
                      Aucun résultat trouvé
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        } @else {
          <!-- Native select -->
          <select
            [id]="id"
            [disabled]="disabled"
            [required]="required"
            [class]="selectClasses()"
            [(ngModel)]="value"
            (ngModelChange)="onValueChange($event)"
            (blur)="onBlur()">
            @if (placeholder) {
              <option [value]="null" disabled selected>{{ placeholder }}</option>
            }
            @for (option of options; track option.value) {
              <option [value]="option.value" [disabled]="option.disabled">
                {{ option.label }}
              </option>
            }
          </select>
          <svg class="absolute right-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400 pointer-events-none"
               fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
          </svg>
        }
      </div>

      @if (hint && !error) {
        <p class="text-sm text-gray-500">{{ hint }}</p>
      }

      @if (error) {
        <p class="text-sm text-red-600 flex items-center gap-1">
          <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
          </svg>
          {{ error }}
        </p>
      }
    </div>
  `,
  host: {
    '(document:click)': 'onClickOutside($event)'
  }
})
export class SelectComponent implements ControlValueAccessor {
  @Input() id = `select-${Math.random().toString(36).substr(2, 9)}`;
  @Input() label?: string;
  @Input() placeholder = 'Sélectionner...';
  @Input() set options(value: SelectOption[]) {
    this._options.set(value);
  }
  get options(): SelectOption[] {
    return this._options();
  }
  private _options = signal<SelectOption[]>([]);
  
  @Input() hint?: string;
  @Input() error?: string;
  @Input() required = false;
  @Input() disabled = false;
  @Input() searchable = false;

  @Input() set value(val: any) {
    this._value.set(val);
  }
  get value(): any {
    return this._value();
  }
  private _value = signal<any>(null);

  @Output() valueChange = new EventEmitter<any>();
  @Output() blurred = new EventEmitter<void>();
  searchTerm = '';
  readonly isOpen = signal(false);

  private onChange: (value: any) => void = () => {};
  private onTouched: () => void = () => {};
  private elementRef = inject(ElementRef);

  readonly selectClasses = computed(() => {
    const base = 'w-full px-3 py-2 pr-10 border rounded-lg focus:outline-none focus:ring-2 transition disabled:bg-gray-50 disabled:cursor-not-allowed disabled:text-gray-500 appearance-none';
    const states = this.error
      ? 'border-red-300 focus:border-red-500 focus:ring-red-200'
      : 'border-gray-300 focus:border-indigo-500 focus:ring-indigo-200';
    return `${base} ${states}`;
  });

  readonly selectedLabel = computed(() => {
    const currentValue = this._value();
    const selected = this._options().find(opt => opt.value === currentValue);
    return selected?.label || '';
  });

  readonly filteredOptions = computed(() => {
    if (!this.searchTerm.trim()) {
      return this._options();
    }
    const term = this.searchTerm.toLowerCase();
    return this._options().filter(opt =>
      opt.label.toLowerCase().includes(term)
    );
  });

  toggleDropdown(): void {
    if (!this.disabled) {
      this.isOpen.set(!this.isOpen());
    }
  }

  selectOption(option: SelectOption): void {
    if (!option.disabled) {
      this.value = option.value;
      this.onValueChange(option.value);
      this.isOpen.set(false);
      this.searchTerm = '';
    }
  }

  onClickOutside(event: Event): void {
    // Vérifier si le clic est à l'extérieur du composant
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  onValueChange(newValue: any): void {
    this.value = newValue;
    this.onChange(newValue);
    this.valueChange.emit(newValue);
  }

  onBlur(): void {
    this.onTouched();
    this.blurred.emit();
  }

  // ControlValueAccessor implementation
  writeValue(value: any): void {
    this.value = value;
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
