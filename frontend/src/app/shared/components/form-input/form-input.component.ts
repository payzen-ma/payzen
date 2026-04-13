import { CommonModule } from '@angular/common';
import { Component, EventEmitter, forwardRef, Input, Output } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';

export type InputState = 'default' | 'focus' | 'error' | 'disabled';
export type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'date';

/**
 * Payzen Form Input Component
 * Implements design system input styling
 *
 * @example
 * <app-form-input
 *   [(ngModel)]="value"
 *   placeholder="Enter value..."
 *   [errorMessage]="error">
 * </app-form-input>
 */
@Component({
    selector: 'app-form-input',
    standalone: true,
    imports: [CommonModule, FormsModule],
    template: `
    <div class="flex flex-col gap-1.5">
      <input
        #inputEl
        [type]="type"
        [class]="inputClasses"
        [placeholder]="placeholder"
        [disabled]="disabled"
        [value]="value"
        [(ngModel)]="value"
        (change)="onChange($event)"
        (input)="onInput($event)"
        (blur)="onBlur()"
        (focus)="onFocus()"
      />
      <p *ngIf="errorMessage && state === 'error'" class="text-danger text-xs">
        ⚠ {{ errorMessage }}
      </p>
    </div>
  `,
    styles: [`
    input {
      font-family: var(--font-family-base);
      font-size: var(--font-size-base);
      transition: all 0.2s ease-in-out;
    }

    input:disabled {
      cursor: not-allowed;
    }
  `],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => FormInputComponent),
            multi: true,
        },
    ],
})
export class FormInputComponent implements ControlValueAccessor {
    @Input() type: InputType = 'text';
    @Input() placeholder = '';
    @Input() disabled = false;
    @Input() errorMessage = '';
    @Input() value: string | null = null;
    @Output() valueChange = new EventEmitter<string>();

    state: InputState = 'default';
    private isFocused = false;

    get inputClasses(): string {
        const baseClass = 'w-full h-10 px-3 py-2.5 rounded-md transition-all duration-200 outline-none font-normal text-base';

        const stateClasses: Record<InputState, string> = {
            'default': 'border border-[var(--border-medium)] bg-white text-[var(--text-primary)] placeholder-gray-400',
            'focus': 'border-2 border-[var(--primary-500)] bg-white text-[var(--text-primary)] shadow-sm',
            'error': 'border border-[var(--danger)] bg-[#fef2f2] text-[var(--danger)] placeholder-red-300',
            'disabled': 'border border-[var(--border-subtle)] bg-[var(--bg-disabled)] text-[var(--text-muted)] placeholder-gray-300'
        };

        const currentState = this.getInputState();
        return `${baseClass} ${stateClasses[currentState]}`;
    }

    private getInputState(): InputState {
        if (this.disabled) return 'disabled';
        if (this.errorMessage) return 'error';
        if (this.isFocused) return 'focus';
        return 'default';
    }

    onChange(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.value = target.value;
        this.valueChange.emit(this.value);
        this.propagateChange(this.value);
    }

    onInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.value = target.value;
    }

    onFocus(): void {
        this.isFocused = true;
        this.state = 'focus';
    }

    onBlur(): void {
        this.isFocused = false;
        if (!this.errorMessage) {
            this.state = 'default';
        }
    }

    // ControlValueAccessor implementation
    writeValue(value: any): void {
        this.value = value;
    }

    registerOnChange(fn: any): void {
        this.propagateChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.propagateTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    private propagateChange = (_: any) => { };
    private propagateTouched = () => { };
}
