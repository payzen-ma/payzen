import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { FieldChange } from '@app/core/utils/change-tracker.util';

@Component({
  selector: 'app-change-confirmation-dialog',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    DialogModule,
    ButtonModule
  ],
  template: `
    <p-dialog
      [visible]="visible()"
      [header]="'employees.profile.confirmChanges.title' | translate"
      [modal]="true"
      [closable]="true"
      [draggable]="false"
      styleClass="app-dialog app-dialog--medium"
      (visibleChange)="onVisibleChange($event)"
    >
      <div class="space-y-4">
        <div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <p class="text-sm text-blue-900">
            {{ 'employees.profile.confirmChanges.summary' | translate: { count: changes().length } }}
          </p>
          <p class="text-sm text-gray-700 mt-2">
            {{ 'employees.profile.confirmChanges.confirmMessage' | translate }}
          </p>
        </div>
      </div>

      <ng-template pTemplate="footer">
        <div class="flex justify-end gap-2">
          <p-button
            class="btn btn-danger-outline"
            [label]="'common.cancel' | translate"
            severity="secondary"
            [outlined]="true"
            (onClick)="onCancel()"
            [disabled]="isSaving()"
          />
          <p-button
            class="btn btn-success"
            [label]="'common.save' | translate"
            severity="success"
            (onClick)="onConfirm()"
            [loading]="isSaving()"
            [disabled]="isSaving()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class ChangeConfirmationDialog {
  visible = input.required<boolean>();
  changes = input.required<FieldChange[]>();
  isSaving = input<boolean>(false);

  visibleChange = output<boolean>();
  confirm = output<void>();
  cancel = output<void>();

  onVisibleChange(visible: boolean): void {
    this.visibleChange.emit(visible);
  }

  onConfirm(): void {
    this.confirm.emit();
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
