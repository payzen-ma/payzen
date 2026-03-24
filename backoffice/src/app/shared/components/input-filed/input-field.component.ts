import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-input-field',
  standalone: true,
  templateUrl: './input-field.component.html',
})
export class InputFieldComponent {
  @Input() label!: string; // Label du champ
  @Input() placeholder: string = '';
  @Input() type: string = 'text';
  @Input() error?: string; // Message d'erreur
}
