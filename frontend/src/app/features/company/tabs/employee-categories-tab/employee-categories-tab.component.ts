import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { EmployeeCategoryService, EmployeeCategory } from '@app/core/services/employee-category.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';

@Component({
  selector: 'app-employee-categories-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonModule,
    InputTextModule,
    DialogModule,
    ToastModule,
    TooltipModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './employee-categories-tab.component.html',
  styles: [`
    .entity-tab { display:flex; flex-direction:column; gap:var(--space-6); padding:var(--space-4) var(--space-6) var(--space-6); }
    .entity-tab-header { display:flex; align-items:flex-start; justify-content:space-between; gap:var(--space-4); }
    .entity-tab-title-row { display:flex; align-items:center; gap:var(--space-2); }
    .entity-tab-title { margin:0; color:var(--text-primary); font-size:var(--font-size-xl); font-weight:600; line-height:1.1; }
    .entity-tab-count-badge { border-radius:var(--radius-full); background:var(--info-light); color:var(--primary-500); font-size:var(--font-size-xs); font-weight:500; line-height:16px; padding:4px 8px; white-space:nowrap; }
    .entity-tab-subtitle { margin:var(--space-2) 0 0; color:var(--neutral-500); font-size:var(--font-size-sm); font-weight:500; }
    .entity-tab-add-btn { height:36px; border:0; border-radius:var(--radius-md); background:var(--primary-500); color:var(--text-inverse); display:inline-flex; align-items:center; gap:var(--space-2); padding:0 var(--space-3); font-size:13px; font-weight:500; cursor:pointer; }
    .entity-tab-add-btn:hover { background:var(--primary-600); }
    .entity-tab-card { border:1px solid var(--border-subtle); border-radius:var(--radius-xl); overflow:hidden; background:var(--bg-element); }
    .entity-tab-card-header { background:var(--bg-page); border-bottom:1px solid var(--border-subtle); padding:12px var(--space-4) 13px; display:flex; align-items:center; justify-content:space-between; gap:var(--space-4); }
    .entity-tab-card-title { margin:0; color:var(--neutral-800); font-size:var(--font-size-sm); font-weight:600; line-height:20px; }
    .entity-tab-card-description { margin:2px 0 0; color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .entity-tab-search-wrap { width:374px; max-width:100%; border:1px solid var(--neutral-200); border-radius:var(--radius-lg); background:var(--bg-element); display:flex; align-items:center; gap:var(--space-3); padding:0 var(--space-3); }
    .entity-tab-search-wrap i { color:var(--neutral-400); font-size:14px; }
    .entity-tab-search-input { width:100%; height:40px; border:0; outline:0; background:transparent; color:var(--text-primary); font-size:var(--font-size-sm); }
    .entity-tab-list { display:flex; flex-direction:column; }
    .entity-tab-row { display:flex; align-items:center; justify-content:space-between; gap:var(--space-3); padding:12px var(--space-4); border-bottom:1px solid var(--neutral-100); }
    .entity-tab-row-main { border:0; background:transparent; padding:0; width:100%; display:flex; align-items:center; gap:var(--space-4); text-align:left; cursor:pointer; }
    .entity-tab-row-icon { width:40px; height:40px; border-radius:var(--radius-lg); background:var(--primary-50); display:grid; place-items:center; color:var(--primary-500); }
    .entity-tab-row-title { margin:0; color:var(--neutral-800); font-size:var(--font-size-sm); font-weight:500; line-height:16px; }
    .entity-tab-row-subtitle { margin:2px 0 0; color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .entity-tab-row-delete { width:28px; height:28px; border-radius:var(--radius-md); border:0; background:transparent; color:var(--text-secondary); cursor:pointer; }
    .entity-tab-row-delete:hover { background:var(--danger-light); color:var(--danger); }
    .entity-tab-footer { background:var(--bg-page); border-top:1px solid var(--border-subtle); padding:var(--space-3) var(--space-6); display:flex; align-items:center; justify-content:space-between; gap:var(--space-3); color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .entity-tab-footer-right { display:inline-flex; align-items:center; gap:var(--space-2); }
    .entity-tab-footer-right i { color:var(--primary-500); }
    .entity-tab-loading, .entity-tab-empty { min-height:180px; display:flex; flex-direction:column; align-items:center; justify-content:center; gap:var(--space-2); color:var(--text-secondary); }
    .entity-tab-loading i { font-size:24px; }
    .entity-tab-empty i { font-size:28px; color:var(--neutral-400); }
    .entity-tab-empty h3 { margin:0; color:var(--text-primary); font-size:var(--font-size-base); font-weight:600; }
    .entity-tab-empty p { margin:0; color:var(--text-secondary); font-size:var(--font-size-sm); text-align:center; }
    .category-dialog__header { align-items:center; display:flex; width:100%; }
    .category-dialog__header-main { align-items:center; display:flex; gap:12px; min-width:0; }
    .category-dialog__header-icon { align-items:center; background:var(--status-info-light, #dbeafe); border-radius:var(--radius-lg, 8px); color:var(--primary-500); display:flex; flex-shrink:0; height:40px; justify-content:center; width:40px; }
    .category-dialog__header-text { min-width:0; }
    .category-dialog__title { color:var(--text-primary); font-size:var(--font-size-xl); font-weight:600; line-height:1.1; margin:0; }
    .category-dialog__subtitle { color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; margin:4px 0 0; }
    .category-dialog__body { display:flex; flex-direction:column; gap:16px; padding:24px; }
    .category-dialog__field { display:flex; flex-direction:column; gap:8px; }
    .category-dialog__label { color:var(--surface-700, #344155); font-size:var(--font-size-sm); font-weight:500; line-height:20px; }
    .category-dialog__input-wrap { position:relative; }
    .category-dialog__input-icon { color:var(--text-secondary); font-size:14px; left:12px; pointer-events:none; position:absolute; top:50%; transform:translateY(-50%); z-index:1; }
    .category-dialog__input { border:1px solid var(--surface-200, #e2e8f0); border-radius:var(--radius-lg, 8px); color:var(--text-primary); font-size:13px; height:38px; padding:8px 12px 8px 36px; width:100%; }
    .category-dialog__input::placeholder { color:var(--text-secondary); opacity:1; }
    .category-dialog__input:focus { border-color:var(--primary-500); box-shadow:0 0 0 3px color-mix(in srgb, var(--primary-500) 16%, transparent); outline:none; }
    .category-dialog__select { appearance:none; cursor:pointer; padding-right:34px; }
    .category-dialog__chevron { color:var(--text-secondary); font-size:11px; pointer-events:none; position:absolute; right:12px; top:50%; transform:translateY(-50%); }
    .category-dialog__input--invalid { border-color:var(--danger, #ef4444); }
    .category-dialog__error { align-items:center; color:var(--danger, #ef4444); display:inline-flex; font-size:12px; gap:6px; line-height:16px; }
    .category-dialog__hint { color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .category-dialog__examples { background:var(--primary-50, #ebf5ff); border:1px solid var(--status-info-light, #dbeafe); border-radius:var(--radius-xl, 12px); padding:12px; }
    .category-dialog__examples-row { align-items:flex-start; display:flex; gap:12px; }
    .category-dialog__examples-icon { color:var(--primary-500); font-size:16px; margin-top:1px; }
    .category-dialog__examples-content { flex:1; min-width:0; }
    .category-dialog__examples-title { color:var(--primary-700, #0f4187); font-size:var(--font-size-xs); font-weight:500; line-height:16px; margin:0 0 8px; }
    .category-dialog__examples-list { display:flex; flex-wrap:wrap; gap:8px; }
    .category-dialog__example-chip { align-items:center; background:var(--background-element, #fff); border:1px solid var(--status-info-light, #dbeafe); border-radius:var(--radius-lg, 8px); color:var(--primary-500); cursor:pointer; display:inline-flex; font-size:13px; justify-content:center; line-height:1.1; padding:8px 12px; transition:background-color .15s ease, border-color .15s ease; }
    .category-dialog__example-chip:hover { background:var(--primary-50, #ebf5ff); border-color:var(--primary-200, #bfdbfe); }
    .category-dialog__footer { align-items:center; background:var(--background-page, #f8fafc); border-top:1px solid var(--border-subtle, #e5e7eb); display:flex; justify-content:space-between; padding:16px 20px; width:100%; }
    .category-dialog__footer-info { align-items:center; color:var(--text-secondary); display:flex; font-size:var(--font-size-xs); gap:6px; line-height:16px; min-width:0; }
    .category-dialog__footer-info i { color:var(--primary-500); }
    .category-dialog__footer-actions { display:flex; gap:16px; }
    :host ::ng-deep .app-category-dialog .p-dialog-header { background:var(--background-page, #f8fafc); border-bottom:1px solid var(--border-subtle, #e5e7eb); padding:16px 20px; }
    :host ::ng-deep .app-category-dialog .p-dialog-content { padding:0; }
    :host ::ng-deep .app-category-dialog .p-dialog-footer { padding:0; }
    :host ::ng-deep .app-category-dialog .category-dialog__btn-cancel.p-button { border-radius:9px; font-size:14px; height:40px; padding:0 13px; }
    :host ::ng-deep .app-category-dialog .category-dialog__btn-primary.p-button { border-radius:var(--radius-md, 6px); font-size:13px; height:40px; padding:0 12px; }
    @media (max-width:900px){ .entity-tab{ padding:var(--space-4);} .entity-tab-header,.entity-tab-card-header{ flex-direction:column; align-items:stretch;} .entity-tab-footer{ flex-direction:column; align-items:flex-start; padding:var(--space-3) var(--space-4);} .category-dialog__body{ padding:16px;} .category-dialog__footer{ align-items:flex-start; flex-direction:column; gap:12px;} .category-dialog__footer-actions{ width:100%; } :host ::ng-deep .app-category-dialog .category-dialog__btn-cancel.p-button, :host ::ng-deep .app-category-dialog .category-dialog__btn-primary.p-button { flex:1; justify-content:center; width:100%; } }
  `]
})
export class EmployeeCategoriesTabComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(EmployeeCategoryService);
  private readonly contextService = inject(CompanyContextService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  readonly categories = signal<EmployeeCategory[]>([]);
  readonly isLoading = signal(false);
  readonly showDialog = signal(false);
  readonly isEditing = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly searchTerm = signal('');
  readonly displayedCategories = computed(() => {
    const query = this.searchTerm().trim().toLowerCase();
    if (!query) return this.categories();
    return this.categories().filter((c) => c.name?.toLowerCase().includes(query));
  });

  readonly categoryForm = this.fb.group({
    name: ['', Validators.required],
    mode: ['1', Validators.required],
    payrollPeriodicity: ['Mensuelle', Validators.required]
  });

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    const companyId = this.contextService.companyId();
    if (!companyId) return;

    this.isLoading.set(true);
    this.categoryService.getByCompany(Number(companyId)).subscribe({
      next: (data) => {
        this.categories.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('company.employeeCategories.messages.loadError')
        });
        this.isLoading.set(false);
      }
    });
  }

  openCreateDialog(): void {
    this.isEditing.set(false);
    this.editingId.set(null);
    this.showDialog.set(true);
    // Apply reset after dialog becomes visible to avoid possible re-render issues
    setTimeout(() => {
      this.categoryForm.reset({
        name: '',
        mode: '1',
        payrollPeriodicity: 'Mensuelle'
      });
    }, 0);
  }

  openEditDialog(category: EmployeeCategory): void {
    this.isEditing.set(true);
    this.editingId.set(category.id);
    this.showDialog.set(true);
    // Patch values after dialog is visible to ensure the native select reflects the new value
    setTimeout(() => {
        this.categoryForm.patchValue({
          name: category.name,
          mode: String(this.normalizeModeValue(category.mode)),
          payrollPeriodicity: this.normalizePayrollPeriodicity(category.payrollPeriodicity)
        });
    }, 0);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.categoryForm.reset({
      name: '',
      mode: '1',
      payrollPeriodicity: 'Mensuelle'
    });
  }

  save(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    const companyId = this.contextService.companyId();
    if (!companyId) return;

    const value = this.categoryForm.value;
    const normalizedMode = this.normalizeModeValue(value.mode);
    const payloadCreate = {
      name: String(value.name ?? ''),
      mode: Number(normalizedMode),
      payrollPeriodicity: this.normalizePayrollPeriodicity(value.payrollPeriodicity),
      companyId: Number(companyId)
    };
    const payloadUpdate = {
      name: String(value.name ?? ''),
      mode: Number(normalizedMode),
      payrollPeriodicity: this.normalizePayrollPeriodicity(value.payrollPeriodicity)
    };

    if (this.isEditing()) {
      const id = this.editingId();
      if (!id) return;

      this.categoryService.update(id, payloadUpdate).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('company.employeeCategories.messages.updateSuccess')
          });
          this.loadCategories();
          this.closeDialog();
        },
        error: (err) => {
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: err?.error?.message || this.translate.instant('company.employeeCategories.messages.updateError')
          });
        }
      });
    } else {
      this.categoryService.create(payloadCreate).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('company.employeeCategories.messages.createSuccess')
          });
          this.loadCategories();
          this.closeDialog();
        },
        error: (err) => {
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: err?.error?.message || this.translate.instant('company.employeeCategories.messages.createError')
          });
        }
      });
    }
  }

  deleteCategory(category: EmployeeCategory): void {
    if (!confirm(this.translate.instant('company.employeeCategories.deleteConfirm'))) {
      return;
    }

    this.categoryService.delete(category.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('company.employeeCategories.messages.deleteSuccess')
        });
        this.loadCategories();
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: err?.error?.message || this.translate.instant('company.employeeCategories.messages.deleteError')
        });
      }
    });
  }

  onSearchList(value: string): void {
    this.searchTerm.set(value ?? '');
  }

  applyCategoryExample(exampleKey: 'Manager' | 'Executive' | 'Technician' | 'Supervisor' | 'Intern'): void {
    const translated = this.translate.instant(`company.employeeCategories.examplesList.${exampleKey}`);
    this.categoryForm.patchValue({ name: translated });
  }

  getPayrollPeriodicityLabel(periodicity?: string): string {
    const norm = this.normalizePayrollPeriodicity(periodicity);
    if (norm === 'Bimensuelle') {
      return this.translate.instant('company.employeeCategories.form.payrollPeriodicityOptions.bimonthly');
    }

    return this.translate.instant('company.employeeCategories.form.payrollPeriodicityOptions.monthly');
  }

  private normalizeModeValue(input?: string | number | null): '1' | '2' {
    if (input === null || input === undefined) return '1';
    const v = typeof input === 'number' ? String(input) : String(input).trim().toLowerCase();
    if (v === '1' || v === 'attendance' || v === 'presence' || v.includes('attend') || v.includes('presenc')) {
      return '1';
    }
    if (v === '2' || v === 'absence' || v.includes('absenc')) {
      return '2';
    }
    return '1';
  }

  getModeLabel(mode: string | number | undefined | null, modeDescription?: string): string {
    if (modeDescription) return modeDescription;
    const norm = this.normalizeModeValue(mode);
    return norm === '1'
      ? this.translate.instant('company.employeeCategories.form.modeOptions.attendance')
      : this.translate.instant('company.employeeCategories.form.modeOptions.absence');
  }

  private normalizePayrollPeriodicity(input?: string | null): 'Mensuelle' | 'Bimensuelle' {
    if (!input) return 'Mensuelle';
    const v = String(input).trim().toLowerCase();
    if (v === 'bimensuelle' || v === 'bimonthly' || v.includes('bimen') || v.includes('bimon')) {
      return 'Bimensuelle';
    }
    return 'Mensuelle';
  }
}
