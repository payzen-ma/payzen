import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ReferenceDataService } from '../../../../core/services/reference-data.service';
import { EducationLevel } from '../../../../core/models/reference-data.model';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-reference-data-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonModule,
    TableModule,
    DialogModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './reference-data-tab.component.html',
})
export class ReferenceDataTabComponent implements OnInit {
  private fb = inject(FormBuilder);
  private referenceDataService = inject(ReferenceDataService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);

  // Signals for data
  educationLevels = signal<EducationLevel[]>([]);
  
  // Loading states
  loadingEducationLevels = signal(false);
  
  // Dialog states
  dialogVisible = signal(false);
  submitLoading = signal(false);
  
  // Form
  itemForm!: FormGroup;
  isEditMode = false;
  currentItemId: number | null = null;

  ngOnInit() {
    this.initForm();
    this.loadAllData();
  }

  private initForm() {
    this.itemForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]]
    });
  }

  private loadAllData() {
    this.loadEducationLevels();
  }

  loadEducationLevels() {
    this.loadingEducationLevels.set(true);
    this.referenceDataService.getEducationLevels().subscribe({
      next: (data) => {
        this.educationLevels.set(data);
        this.loadingEducationLevels.set(false);
      },
      error: (err) => {
        console.error('Error loading education levels', err);
        this.loadingEducationLevels.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Could not load education levels' });
      }
    });
  }

  // Dialog operations
  openCreateDialog() {
    this.isEditMode = false;
    this.currentItemId = null;
    this.itemForm.reset();
    this.dialogVisible.set(true);
  }

  openEditDialog(item: EducationLevel) {
    this.isEditMode = true;
    this.currentItemId = item.id;
    this.itemForm.patchValue({ name: item.educationLevelName });
    this.dialogVisible.set(true);
  }

  getDialogTitle(): string {
    const action = this.isEditMode ? 'edit' : 'create';
    return this.translate.instant(`company.referenceData.educationLevels.${action}`);
  }

  getFieldLabel(): string {
    return this.translate.instant('company.referenceData.educationLevels.name');
  }

  getPlaceholder(): string {
    return this.translate.instant('company.referenceData.educationLevels.placeholder');
  }

  saveItem() {
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    this.submitLoading.set(true);
    const name = this.itemForm.value.name;

    if (this.isEditMode && this.currentItemId) {
      this.updateItem(name);
    } else {
      this.createItem(name);
    }
  }

  private createItem(name: string) {
    this.referenceDataService.createEducationLevel({ EducationLevelName: name }).subscribe({
      next: () => {
        this.submitLoading.set(false);
        this.dialogVisible.set(false);
        this.loadEducationLevels();
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Item created successfully' });
      },
      error: (err: HttpErrorResponse) => {
        this.submitLoading.set(false);
        this.handleError(err);
      }
    });
  }

  private updateItem(name: string) {
    if (!this.currentItemId) return;

    this.referenceDataService.updateEducationLevel(this.currentItemId, { EducationLevelName: name }).subscribe({
      next: () => {
        this.submitLoading.set(false);
        this.dialogVisible.set(false);
        this.loadEducationLevels();
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Item updated successfully' });
      },
      error: (err: HttpErrorResponse) => {
        this.submitLoading.set(false);
        this.handleError(err);
      }
    });
  }

  confirmDelete(item: EducationLevel) {
    this.confirmationService.confirm({
      message: this.translate.instant('company.referenceData.deleteConfirm'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteItem(item.id);
      }
    });
  }

  private deleteItem(id: number) {
    this.referenceDataService.deleteEducationLevel(id).subscribe({
      next: () => {
        this.loadEducationLevels();
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Item deleted successfully' });
      },
      error: (err: HttpErrorResponse) => {
        this.handleError(err);
      }
    });
  }

  private handleError(err: HttpErrorResponse) {
    let detail = 'An error occurred';
    
    if (err.status === 409) {
      detail = 'This name already exists';
    } else if (err.status === 400) {
      detail = err.error?.message || err.error?.title || 'Invalid request or item is in use';
    } else if (err.status === 403) {
      detail = 'Access denied';
    } else if (err.status === 404) {
      detail = 'Resource not found';
    }

    this.messageService.add({ severity: 'error', summary: 'Error', detail });
  }
}
