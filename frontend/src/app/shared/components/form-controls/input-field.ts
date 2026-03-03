import { CommonModule } from '@angular/common';
import { Component, Input, Optional, Self } from '@angular/core';
import { AbstractControl, ControlValueAccessor, FormsModule, NgControl, ReactiveFormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

export type ErrorMessageResolver = string | ((error: any) => string);

@Component({
  selector: 'app-input-field',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, InputTextModule, IconFieldModule, InputIconModule],
  templateUrl: './input-field.html',
  styleUrls: ['./input-field.css'],
})
export class InputFieldComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() hint?: string;
  @Input() description?: string;
  @Input() requiredMark = false;
  @Input() hideLabel = false;
  @Input() type: string = 'text';
  @Input() disabled = false;
  @Input() showErrors = true;
  @Input() errorMessages?: Record<string, ErrorMessageResolver>;
  @Input() autocomplete?: string;
  @Input() inputId?: string;
  @Input() ariaLabel?: string;
  @Input() icon?: string;
  @Input() iconPosition: 'left' | 'right' = 'left';

  value: any = '';
  private readonly uid = `input-${Math.random().toString(36).slice(2, 8)}`;

  private onChange: (value: any) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(@Optional() @Self() public ngControl: NgControl) {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  private readonly defaultErrorMap: Record<string, (error: any) => string> = {
    required: () => 'This field is required',
    email: () => 'Enter a valid email address',
    minlength: (error) => `Minimum ${error?.requiredLength} characters`,
    maxlength: (error) => `Maximum ${error?.requiredLength} characters`,
    min: (error) => `Value must be at least ${error?.min}`,
    max: (error) => `Value must be at most ${error?.max}`,
    pattern: () => 'Value does not match the required pattern',
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
    this.value = value ?? '';
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

  handleInput(raw: string): void {
    const parsed = this.type === 'number' ? (raw === '' ? null : Number(raw)) : raw;
    this.value = parsed ?? '';
    this.onChange(parsed);
  }

  handleBlur(): void {
    this.onTouched();
  }

  get resolvedInputId(): string {
    return this.inputId || this.uid;
  }
}
