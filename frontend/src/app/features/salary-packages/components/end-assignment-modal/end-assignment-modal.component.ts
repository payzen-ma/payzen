import { Component, signal, computed, output, input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { SalaryPackageAssignment } from '@app/core/models/salary-package.model';

@Component({
  selector: 'app-end-assignment-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './end-assignment-modal.component.html'
})
export class EndAssignmentModalComponent {
  private readonly translate = inject(TranslateService);
  // Inputs
  assignment = input.required<SalaryPackageAssignment | null>();
  isOpen = input.required<boolean>();

  // Outputs
  close = output<void>();
  end = output<{ assignmentId: number; endDate: string }>();

  // State
  endDate = signal<string>(this.getTodayString());
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);

  canSubmit = computed(() =>
    this.assignment() !== null &&
    this.endDate() !== '' &&
    !this.isSubmitting()
  );

  onSubmit() {
    if (!this.canSubmit()) return;

    const assignment = this.assignment();
    if (!assignment) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.end.emit({
      assignmentId: assignment.id,
      endDate: this.endDate()
    });
  }

  setSubmitting(value: boolean) {
    this.isSubmitting.set(value);
  }

  setError(message: string) {
    this.errorMessage.set(message);
    this.isSubmitting.set(false);
  }

  onClose() {
    this.close.emit();
    this.resetForm();
  }

  resetForm() {
    this.endDate.set(this.getTodayString());
    this.isSubmitting.set(false);
    this.errorMessage.set(null);
  }

  getTodayString(): string {
    return new Date().toISOString().split('T')[0];
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('fr-MA', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  private t(key: string, params?: Record<string, unknown>): string {
    const translated = this.translate.instant(key, params);
    return typeof translated === 'string' ? translated : key;
  }
}
