import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './modal.component.html'
})
export class ModalComponent {
  @Input() visible = false;
  @Input() title = '';
  @Output() visibleChange = new EventEmitter<boolean>();

  close() {
    this.visibleChange.emit(false);
  }

  /** Backdrop click: consume event and defer close so the click is not retargeted to elements underneath. */
  onBackdropClick(event: MouseEvent) {
    event.preventDefault();
    event.stopPropagation();
    setTimeout(() => this.close(), 0);
  }
}
