import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-select-input',
  standalone: true,
  templateUrl: './select-input.component.html',
})
export class SelectInputComponent {
  @Input() label!: string;
  @Input() options: string[] = [];
}
