import { Component, OnInit, signal, inject } from '@angular/core';
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
  styleUrls: ['./employee-categories-tab.component.css']
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
        console.error('Failed to load categories', err);
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
    console.debug('Opening edit dialog for category', category);
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
          console.error('Failed to update category', err);
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
          console.error('Failed to create category', err);
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
        console.error('Failed to delete category', err);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: err?.error?.message || this.translate.instant('company.employeeCategories.messages.deleteError')
        });
      }
    });
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
