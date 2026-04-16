import { Component, OnInit, inject, signal, computed, DestroyRef } from '@angular/core';
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
  styles: [`
    .department-tab {
      display: flex;
      flex-direction: column;
      gap: var(--space-6);
      padding: var(--space-4) var(--space-6) var(--space-6);
    }

    .department-tab-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: var(--space-4);
    }

    .department-tab-title-row {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }

    .department-tab-title {
      margin: 0;
      color: var(--text-primary);
      font-size: var(--font-size-xl);
      font-weight: 600;
      line-height: 1.1;
    }

    .department-tab-count-badge {
      border-radius: var(--radius-full);
      background: var(--info-light);
      color: var(--primary-500);
      font-size: var(--font-size-xs);
      font-weight: 500;
      line-height: 16px;
      padding: 4px 8px;
      white-space: nowrap;
    }

    .department-tab-subtitle {
      margin: var(--space-2) 0 0;
      color: var(--neutral-500);
      font-size: var(--font-size-sm);
      font-weight: 500;
    }

    .department-tab-add-btn {
      height: 36px;
      border: 0;
      border-radius: var(--radius-md);
      background: var(--primary-500);
      color: var(--text-inverse);
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
      padding: 0 var(--space-3);
      font-size: 13px;
      font-weight: 500;
      cursor: pointer;
    }

    .department-tab-add-btn:hover {
      background: var(--primary-600);
    }

    .department-tab-card {
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-xl);
      overflow: hidden;
      background: var(--bg-element);
    }

    .department-tab-card-header {
      background: var(--bg-page);
      border-bottom: 1px solid var(--border-subtle);
      padding: 12px var(--space-4) 13px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-4);
    }

    .department-tab-card-title {
      margin: 0;
      color: var(--neutral-800);
      font-size: var(--font-size-sm);
      font-weight: 600;
      line-height: 20px;
    }

    .department-tab-card-description {
      margin: 2px 0 0;
      color: var(--text-secondary);
      font-size: var(--font-size-xs);
      line-height: 16px;
    }

    .department-tab-search-wrap {
      width: 374px;
      max-width: 100%;
      border: 1px solid var(--neutral-200);
      border-radius: var(--radius-lg);
      background: var(--bg-element);
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: 0 var(--space-3);
    }

    .department-tab-search-wrap i {
      color: var(--neutral-400);
      font-size: 14px;
    }

    .department-tab-search-input {
      width: 100%;
      height: 40px;
      border: 0;
      outline: 0;
      background: transparent;
      color: var(--text-primary);
      font-size: var(--font-size-sm);
    }

    .department-tab-list {
      display: flex;
      flex-direction: column;
    }

    .department-tab-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      padding: 12px var(--space-4);
      border-bottom: 1px solid var(--neutral-100);
    }

    .department-tab-row-main {
      border: 0;
      background: transparent;
      padding: 0;
      width: 100%;
      display: flex;
      align-items: center;
      gap: var(--space-4);
      text-align: left;
      cursor: pointer;
    }

    .department-tab-row-icon {
      width: 40px;
      height: 40px;
      border-radius: var(--radius-lg);
      background: var(--primary-50);
      display: grid;
      place-items: center;
      color: var(--primary-500);
    }

    .department-tab-row-title {
      margin: 0;
      color: var(--neutral-800);
      font-size: var(--font-size-sm);
      font-weight: 500;
      line-height: 16px;
    }

    .department-tab-row-subtitle {
      margin: 2px 0 0;
      color: var(--text-secondary);
      font-size: var(--font-size-xs);
      line-height: 16px;
    }

    .department-tab-row-delete {
      width: 28px;
      height: 28px;
      border-radius: var(--radius-md);
      border: 0;
      background: transparent;
      color: var(--text-secondary);
      cursor: pointer;
    }

    .department-tab-row-delete:hover {
      background: var(--danger-light);
      color: var(--danger);
    }

    .department-tab-footer {
      background: var(--bg-page);
      border-top: 1px solid var(--border-subtle);
      padding: var(--space-3) var(--space-6);
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      color: var(--text-secondary);
      font-size: var(--font-size-xs);
      line-height: 16px;
    }

    .department-tab-footer-right {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
    }

    .department-tab-footer-right i {
      color: var(--primary-500);
    }

    .department-tab-loading,
    .department-tab-empty {
      min-height: 180px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: var(--space-2);
      color: var(--text-secondary);
    }

    .department-tab-loading i {
      font-size: 24px;
    }

    .department-tab-empty i {
      font-size: 28px;
      color: var(--neutral-400);
    }

    .department-tab-empty h3 {
      margin: 0;
      color: var(--text-primary);
      font-size: var(--font-size-base);
      font-weight: 600;
    }

    .department-tab-empty p {
      margin: 0;
      color: var(--text-secondary);
      font-size: var(--font-size-sm);
    }

    .department-dialog__header { align-items:center; display:flex; width:100%; }
    .department-dialog__header-main { align-items:center; display:flex; gap:12px; min-width:0; }
    .department-dialog__header-icon { align-items:center; background:var(--status-info-light, #dbeafe); border-radius:var(--radius-lg, 8px); color:var(--primary-500); display:flex; flex-shrink:0; height:40px; justify-content:center; width:40px; }
    .department-dialog__header-text { min-width:0; }
    .department-dialog__title { color:var(--text-primary); font-size:var(--font-size-xl); font-weight:600; line-height:1.1; margin:0; }
    .department-dialog__subtitle { color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; margin:4px 0 0; }
    .department-dialog__body { display:flex; flex-direction:column; gap:16px; padding:24px; }
    .department-dialog__field { display:flex; flex-direction:column; gap:8px; }
    .department-dialog__label { color:var(--surface-700, #344155); font-size:var(--font-size-sm); font-weight:500; line-height:20px; }
    .department-dialog__input-wrap { position:relative; }
    .department-dialog__input-icon { color:var(--text-secondary); font-size:14px; left:12px; pointer-events:none; position:absolute; top:50%; transform:translateY(-50%); z-index:1; }
    .department-dialog__input { border:1px solid var(--surface-200, #e2e8f0); border-radius:var(--radius-lg, 8px); color:var(--text-primary); font-size:13px; height:38px; padding:8px 12px 8px 36px; width:100%; }
    .department-dialog__input::placeholder { color:var(--text-secondary); opacity:1; }
    .department-dialog__input:focus { border-color:var(--primary-500); box-shadow:0 0 0 3px color-mix(in srgb, var(--primary-500) 16%, transparent); outline:none; }
    .department-dialog__input--invalid { border-color:var(--danger, #ef4444); }
    .department-dialog__error { align-items:center; color:var(--danger, #ef4444); display:inline-flex; font-size:12px; gap:6px; line-height:16px; }
    .department-dialog__hint { color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .department-dialog__examples { background:var(--primary-50, #ebf5ff); border:1px solid var(--status-info-light, #dbeafe); border-radius:var(--radius-xl, 12px); padding:12px; }
    .department-dialog__examples-row { align-items:flex-start; display:flex; gap:12px; }
    .department-dialog__examples-icon { color:var(--primary-500); font-size:16px; margin-top:1px; }
    .department-dialog__examples-content { flex:1; min-width:0; }
    .department-dialog__examples-title { color:var(--primary-700, #0f4187); font-size:var(--font-size-xs); font-weight:500; line-height:16px; margin:0 0 8px; }
    .department-dialog__examples-list { display:flex; flex-wrap:wrap; gap:8px; }
    .department-dialog__example-chip { align-items:center; background:var(--background-element, #fff); border:1px solid var(--status-info-light, #dbeafe); border-radius:var(--radius-lg, 8px); color:var(--primary-500); cursor:pointer; display:inline-flex; font-size:13px; justify-content:center; line-height:1.1; padding:8px 12px; transition:background-color .15s ease, border-color .15s ease; }
    .department-dialog__example-chip:hover { background:var(--primary-50, #ebf5ff); border-color:var(--primary-200, #bfdbfe); }
    .department-dialog__footer { align-items:center; background:var(--background-page, #f8fafc); border-top:1px solid var(--border-subtle, #e5e7eb); display:flex; justify-content:space-between; padding:16px 20px; width:100%; }
    .department-dialog__footer-info { align-items:center; color:var(--text-secondary); display:flex; font-size:var(--font-size-xs); gap:6px; line-height:16px; min-width:0; }
    .department-dialog__footer-info i { color:var(--primary-500); }
    .department-dialog__footer-actions { display:flex; gap:16px; }
    :host ::ng-deep .app-department-dialog .p-dialog-header { background:var(--background-page, #f8fafc); border-bottom:1px solid var(--border-subtle, #e5e7eb); padding:16px 20px; }
    :host ::ng-deep .app-department-dialog .p-dialog-content { padding:0; }
    :host ::ng-deep .app-department-dialog .p-dialog-footer { padding:0; }
    :host ::ng-deep .app-department-dialog .department-dialog__btn-cancel.p-button { border-radius:9px; font-size:14px; height:40px; padding:0 13px; }
    :host ::ng-deep .app-department-dialog .department-dialog__btn-primary.p-button { border-radius:var(--radius-md, 6px); font-size:13px; height:40px; padding:0 12px; }

    @media (max-width: 900px) {
      .department-tab {
        padding: var(--space-4);
      }

      .department-tab-header,
      .department-tab-card-header {
        flex-direction: column;
        align-items: stretch;
      }

      .department-tab-footer {
        flex-direction: column;
        align-items: flex-start;
        padding: var(--space-3) var(--space-4);
      }

      .department-dialog__body {
        padding: 16px;
      }

      .department-dialog__footer {
        align-items: flex-start;
        flex-direction: column;
        gap: 12px;
      }

      .department-dialog__footer-actions {
        width: 100%;
      }

      :host ::ng-deep .app-department-dialog .department-dialog__btn-cancel.p-button,
      :host ::ng-deep .app-department-dialog .department-dialog__btn-primary.p-button {
        flex: 1;
        justify-content: center;
        width: 100%;
      }
    }
  `]
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
  searchTerm = signal('');
  loading = signal(false);
  dialogVisible = signal(false);
  submitLoading = signal(false);
  filteredDepartments = computed(() => {
    const query = this.searchTerm().trim().toLowerCase();
    if (!query) {
      return this.departments();
    }
    return this.departments().filter((department) =>
      department.departementName?.toLowerCase().includes(query)
    );
  });

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

  onSearchDepartments(value: string) {
    this.searchTerm.set(value ?? '');
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
