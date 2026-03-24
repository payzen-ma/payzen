import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, OnInit, Optional, Self, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { AbstractControl, ControlValueAccessor, FormsModule, NgControl, ReactiveFormsModule } from '@angular/forms';
import { AutoCompleteModule, AutoCompleteSelectEvent } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';
import { ErrorMessageResolver } from './input-field';

@Component({
  selector: 'app-select-field',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AutoCompleteModule, SelectModule],
  templateUrl: './select-field.html',
  styleUrls: ['./select-field.css'],
})
export class SelectFieldComponent implements ControlValueAccessor, OnInit, OnChanges {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() hint?: string;
  @Input() description?: string;
  @Input() requiredMark = false;
  @Input() hideLabel = false;
  @Input() variant: 'select' | 'autocomplete' = 'select';
  @Input() filter = false;
  @Input() showClear = false;
  @Input() creatable = false; // Allow creating new options
  @Input() createLabel = 'Create'; // Label for create button
  @Input() appendTo: any = 'body';
  @Input() optionLabel = 'label';
  @Input() optionValue?: string;
  @Input() optionDisabled?: string;
  @Input() options: any[] = [];
  @Input() disabled = false;
  @Input() showErrors = true;
  @Input() errorMessages?: Record<string, ErrorMessageResolver>;
  @Input() inputId?: string;
  @Input() ariaLabel?: string;

  @Output() create = new EventEmitter<string>(); // Emit when user wants to create new option
  @Output() searchQuery = new EventEmitter<string>(); // Emit search query for dynamic loading

  value: any = null;
  filteredOptions: any[] = [];
  currentQuery = '';
  showCreateOption = false;
  private readonly uid = `select-${Math.random().toString(36).slice(2, 8)}`;

  private onChange: (value: any) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(@Optional() @Self() public ngControl: NgControl) {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  ngOnInit() {
    this.filteredOptions = [...this.options];
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['options']) {
      this.filteredOptions = [...this.options];
      // Re-evaluate value if options changed and we have a value
      if (this.value && this.optionValue) {
        this.updateValueFromOptions();
      }
    }
  }

  private updateValueFromOptions() {
    // If we have a value (ID) and optionValue is set, we need to find the full object
    // But wait, this.value might already be the object if set via UI
    // We need to check if this.value is the ID or the Object
    
    // This is tricky. writeValue sets this.value to the model value (ID).
    // So we should try to find the object matching this ID.
    if (this.optionValue && this.value !== null && typeof this.value !== 'object') {
       const found = this.options.find(opt => opt[this.optionValue!] === this.value);
       if (found) {
         this.value = found;
       }
    }
  }

  search(event: any) {
    const query = event.query.toLowerCase().trim();
    this.currentQuery = event.query.trim();
    
    // Emit search query for parent to handle dynamic loading
    this.searchQuery.emit(this.currentQuery);
    
    // Filter existing options
    const filtered = this.options.filter((option) => {
      const label = option[this.optionLabel];
      return label && label.toString().toLowerCase().includes(query);
    });
    
    this.filteredOptions = filtered;
    
    // Show create option if creatable and no exact match found
    if (this.creatable && query && filtered.length === 0) {
      this.showCreateOption = true;
      // Add a special "create" option
      this.filteredOptions = [{
        [this.optionLabel]: `${this.createLabel} "${this.currentQuery}"`,
        __isCreateOption: true,
        __createValue: this.currentQuery
      }];
    } else if (this.creatable && query) {
      // Check if there's an exact match
      const exactMatch = filtered.some(opt => 
        opt[this.optionLabel].toLowerCase() === query
      );
      
      if (!exactMatch) {
        // Add create option at the end
        this.filteredOptions.push({
          [this.optionLabel]: `${this.createLabel} "${this.currentQuery}"`,
          __isCreateOption: true,
          __createValue: this.currentQuery
        });
      }
    }
  }

  onSelect(event: AutoCompleteSelectEvent) {
    const selected = event.value;
    
    // Check if this is a "create new" option
    if (selected.__isCreateOption) {
      this.create.emit(selected.__createValue);
      // Clear the input after emitting create event
      this.value = null;
      return;
    }
    
    // Normal selection
    let val = selected;
    if (this.optionValue) {
      val = selected[this.optionValue];
    }
    this.onChange(val);
  }
  
  onClear() {
    this.value = null;
    this.onChange(null);
  }

  private readonly defaultErrorMap: Record<string, (error: any) => string> = {
    required: () => 'This field is required',
  };

  get control(): AbstractControl | null {
    return this.ngControl?.control ?? null;
  }

  get invalid(): boolean {
    const control = this.control;
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  get ariaInvalid(): string | null {
    return this.invalid ? 'true' : null;
  }

  get errorList(): string[] {
    if (!this.showErrors || !this.invalid) {
      return [];
    }

    const control = this.control;
    if (!control?.errors) {
      return [];
    }

    const merged = { ...this.defaultErrorMap };
    if (this.errorMessages) {
      Object.entries(this.errorMessages).forEach(([key, resolver]) => {
        merged[key] = typeof resolver === 'function' ? resolver : () => resolver;
      });
    }

    return Object.keys(control.errors).map((key) => {
      const resolver = merged[key];
      if (resolver) {
        return resolver(control.errors?.[key]);
      }
      return 'Invalid value';
    });
  }

  writeValue(value: any): void {
    this.value = value ?? null;
    if (this.optionValue && this.value !== null) {
      this.updateValueFromOptions();
    }
  }

  registerOnChange(fn: (value: any) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  handleChange(value: any): void {
    this.value = value;
    this.onChange(value);
  }

  handleBlur(): void {
    this.onTouched();
  }

  get resolvedInputId(): string {
    return this.inputId || this.uid;
  }
}
