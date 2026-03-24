import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-primary-button',
  standalone: true,
  template: `
    <button
      [disabled]="disabled"
      class="w-full py-2 rounded-md font-medium text-white
             bg-primary hover:bg-blue-700 transition
             disabled:opacity-50 disabled:cursor-not-allowed">
      {{ label }}
    </button>
  `
})
export class PrimaryButtonComponent {
  @Input() label!: string;
  @Input() disabled = false;
}
