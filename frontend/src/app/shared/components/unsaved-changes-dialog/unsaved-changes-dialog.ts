import { Component, signal, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-unsaved-changes-dialog',
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
      [header]="'common.unsavedChanges.title' | translate"
      [modal]="true"
      [closable]="false"
      [draggable]="false"
      styleClass="app-dialog app-dialog--medium unsaved-changes-dialog"
    >
      <div class="space-y-4">
        <div class="flex items-start gap-3">
          <i class="pi pi-exclamation-triangle text-yellow-600 text-2xl mt-1"></i>
          <div>
            <p class="text-gray-900 mb-2">
              {{ 'common.unsavedChanges.message' | translate }}
            </p>
            <p class="text-sm text-gray-600">
              {{ 'common.unsavedChanges.description' | translate: { count: changeCount() } }}
            </p>
          </div>
        </div>
      </div>

      <ng-template pTemplate="footer">
        <div class="flex justify-end gap-2">
          <p-button
            class="btn btn-secondary"
            [label]="'common.unsavedChanges.continueEditing' | translate"
            
            (onClick)="onContinueEditing()"
          />
          <p-button
            class="btn btn-danger"
            [label]="'common.unsavedChanges.discard' | translate"
            severity="danger"
            [outlined]="true"
            (onClick)="onDiscard()"
          />
          <p-button
            class="btn btn-success"
            [label]="'common.unsavedChanges.save' | translate"
            severity="success"
            (onClick)="onSave()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `
})
export class UnsavedChangesDialog {
  visible = input.required<boolean>();
  changeCount = input<number>(0);

  continueEditing = output<void>();
  discard = output<void>();
  save = output<void>();

  onContinueEditing(): void {
    this.continueEditing.emit();
  }

  onDiscard(): void {
    this.discard.emit();
  }

  onSave(): void {
    this.save.emit();
  }
}
