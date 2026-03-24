import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { InputTextModule } from 'primeng/inputtext';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-readonly-field',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    InputTextModule,
    InputGroupModule,
    InputGroupAddonModule,
    TooltipModule
  ],
  template: `
    <div>
      <label 
        *ngIf="label" 
        [for]="fieldId" 
        class="block mb-2 text-sm font-medium text-gray-700">
        {{ label }}
      </label>
      <p-inputGroup>
        <input 
          pInputText 
          [id]="fieldId" 
          [value]="value || emptyPlaceholder" 
          [readonly]="true" 
          class="input disabled w-full bg-gray-50 text-gray-600"
          [class.text-gray-400]="!value" />
        <p-inputGroupAddon *ngIf="showLockIcon" class="bg-gray-50 border-l-0">
          <i 
            class="pi pi-lock text-gray-400 cursor-help" 
            [pTooltip]="tooltipText"
            tooltipPosition="top">
          </i>
        </p-inputGroupAddon>
      </p-inputGroup>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ReadonlyFieldComponent {
  /** Unique ID for the input field (for accessibility) */
  @Input() fieldId: string = '';
  
  /** Label text displayed above the field */
  @Input() label: string = '';
  
  /** Current value to display */
  @Input() value: string | null | undefined = '';
  
  /** Placeholder text when value is empty */
  @Input() emptyPlaceholder: string = '—';
  
  /** Whether to show the lock icon addon */
  @Input() showLockIcon: boolean = true;
  
  /** Tooltip text for the lock icon */
  @Input() tooltipText: string = 'Managed by backoffice';
}
