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
    mode: [0, Validators.required]
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
    this.categoryForm.reset();
    this.showDialog.set(true);
  }

  openEditDialog(category: EmployeeCategory): void {
    this.isEditing.set(true);
    this.editingId.set(category.id);
    this.categoryForm.patchValue({
      name: category.name,
      mode: Number(category.mode ?? 0)
    });
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.categoryForm.reset();
  }

  save(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    const companyId = this.contextService.companyId();
    if (!companyId) return;

    const value = this.categoryForm.value;
    const payloadCreate = {
      name: String(value.name ?? ''),
      mode: Number(value.mode ?? 0),
      companyId: Number(companyId)
    };
    const payloadUpdate = {
      name: String(value.name ?? ''),
      mode: Number(value.mode ?? 0)
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
}
