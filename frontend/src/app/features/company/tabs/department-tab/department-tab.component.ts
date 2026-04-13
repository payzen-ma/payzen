import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import { DepartmentService } from '../../../../core/services/department.service';
import { CompanyContextService } from '../../../../core/services/companyContext.service';
import { Department } from '../../../../core/models/department.model';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-department-tab',
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
  templateUrl: './department-tab.component.html',
})
export class DepartmentTabComponent implements OnInit {
  private fb = inject(FormBuilder);
  private departmentService = inject(DepartmentService);
  private contextService = inject(CompanyContextService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  // Signals
  departments = signal<Department[]>([]);
  loading = signal(false);
  dialogVisible = signal(false);
  submitLoading = signal(false);

  // Form
  departmentForm!: FormGroup;
  isEditMode = false;
  currentDepartmentId: number | null = null;

  ngOnInit() {
    this.initForm();
    this.loadDepartments();

    // Reload when company context changes
    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.departments.set([]);
        this.loadDepartments();
      });
  }

  private initForm() {
    this.departmentForm = this.fb.group({
      departementName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(500)]]
    });
  }

  loadDepartments() {
    const companyId = this.contextService.companyId();
    if (!companyId) return;

    this.loading.set(true);
    this.departmentService.getByCompany(Number(companyId)).subscribe({
      next: (data) => {
        this.departments.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('company.departments.messages.loadError')
        });
      }
    });
  }

  openCreateDialog() {
    this.isEditMode = false;
    this.currentDepartmentId = null;
    this.departmentForm.reset();
    this.dialogVisible.set(true);
  }

  openEditDialog(department: Department) {
    this.isEditMode = true;
    this.currentDepartmentId = department.id;
    this.departmentForm.patchValue({
      departementName: department.departementName
    });
    this.dialogVisible.set(true);
  }

  saveDepartment() {
    if (this.departmentForm.invalid) {
      this.departmentForm.markAllAsTouched();
      return;
    }

    const companyId = this.contextService.companyId();
    if (!companyId) {
      this.messageService.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('company.departments.messages.companyIdMissing')
      });
      return;
    }

    this.submitLoading.set(true);
    const payload = {
      DepartementName: this.departmentForm.value.departementName,
      CompanyId: this.isEditMode ? undefined : Number(companyId) // Only required for create
    };

    const request = this.isEditMode && this.currentDepartmentId
      ? this.departmentService.update(this.currentDepartmentId, payload)
      : this.departmentService.create({ ...payload, CompanyId: Number(companyId) });

    request.subscribe({
      next: (res) => {
        this.submitLoading.set(false);
        this.dialogVisible.set(false);
        this.loadDepartments();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.isEditMode
            ? this.translate.instant('company.departments.messages.updateSuccess')
            : this.translate.instant('company.departments.messages.createSuccess')
        });
      },
      error: (err: HttpErrorResponse) => {
        this.submitLoading.set(false);
        this.handleError(err);
      }
    });
  }

  confirmDelete(department: Department) {
    this.confirmationService.confirm({
      message: this.translate.instant('company.departments.deleteConfirm'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteDepartment(department.id);
      }
    });
  }

  private deleteDepartment(id: number) {
    this.departmentService.delete(id).subscribe({
      next: () => {
        this.loadDepartments();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('company.departments.messages.deleteSuccess')
        });
      },
      error: (err: HttpErrorResponse) => {
        this.handleError(err);
      }
    });
  }

  private handleError(err: HttpErrorResponse) {
    let detail = this.translate.instant('common.error');

    if (err.status === 409) {
      detail = this.translate.instant('company.departments.messages.alreadyExists');
    } else if (err.status === 400) {
      // Often used for validation or "cannot delete because used"
      detail = err.error?.title || err.error?.message || this.translate.instant('company.departments.messages.invalidRequest');
    } else if (err.status === 403) {
      detail = this.translate.instant('company.departments.messages.accessDenied');
    } else if (err.status === 404) {
      detail = this.translate.instant('company.departments.messages.notFound');
    }

    this.messageService.add({
      severity: 'error',
      summary: this.translate.instant('common.error'),
      detail
    });
  }
}
