import { Component, OnInit, inject, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { JobPositionService } from '../../../../core/services/job-position.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CompanyContextService } from '../../../../core/services/companyContext.service';
import { JobPosition } from '../../../../core/models/job-position.model';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-job-position-tab',
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
    ConfirmDialogModule,
    TagModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './job-position-tab.component.html',
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
    .position-dialog__header { align-items:center; display:flex; width:100%; }
    .position-dialog__header-main { align-items:center; display:flex; gap:12px; min-width:0; }
    .position-dialog__header-icon { align-items:center; background:var(--status-info-light, #dbeafe); border-radius:var(--radius-lg, 8px); color:var(--primary-500); display:flex; flex-shrink:0; height:40px; justify-content:center; width:40px; }
    .position-dialog__header-text { min-width:0; }
    .position-dialog__title { color:var(--text-primary); font-size:var(--font-size-xl); font-weight:600; line-height:1.1; margin:0; }
    .position-dialog__subtitle { color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; margin:4px 0 0; }
    .position-dialog__body { display:flex; flex-direction:column; gap:16px; padding:24px; }
    .position-dialog__field { display:flex; flex-direction:column; gap:8px; }
    .position-dialog__label { color:var(--surface-700, #344155); font-size:var(--font-size-sm); font-weight:500; line-height:20px; }
    .position-dialog__input-wrap { position:relative; }
    .position-dialog__input { border:1px solid var(--surface-200, #e2e8f0); border-radius:var(--radius-lg, 8px); color:var(--text-primary); font-size:13px; height:38px; padding:8px 12px; width:100%; }
    .position-dialog__input::placeholder { color:var(--text-secondary); opacity:1; }
    .position-dialog__input:focus { border-color:var(--primary-500); box-shadow:0 0 0 3px color-mix(in srgb, var(--primary-500) 16%, transparent); outline:none; }
    .position-dialog__input--invalid { border-color:var(--danger, #ef4444); }
    .position-dialog__error { align-items:center; color:var(--danger, #ef4444); display:inline-flex; font-size:12px; gap:6px; line-height:16px; }
    .position-dialog__hint { color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .position-dialog__examples { background:var(--primary-50, #ebf5ff); border:1px solid var(--status-info-light, #dbeafe); border-radius:var(--radius-xl, 12px); padding:12px; }
    .position-dialog__examples-row { align-items:flex-start; display:flex; gap:12px; }
    .position-dialog__examples-icon { color:var(--primary-500); font-size:16px; margin-top:1px; }
    .position-dialog__examples-content { flex:1; min-width:0; }
    .position-dialog__examples-title { color:var(--primary-700, #0f4187); font-size:var(--font-size-xs); font-weight:500; line-height:16px; margin:0 0 8px; }
    .position-dialog__examples-list { display:flex; flex-wrap:wrap; gap:8px; }
    .position-dialog__example-chip { align-items:center; background:var(--background-element, #fff); border:1px solid var(--status-info-light, #dbeafe); border-radius:var(--radius-lg, 8px); color:var(--primary-500); cursor:pointer; display:inline-flex; font-size:13px; justify-content:center; line-height:1.1; padding:8px 12px; transition:background-color .15s ease, border-color .15s ease; }
    .position-dialog__example-chip:hover { background:var(--primary-50, #ebf5ff); border-color:var(--primary-200, #bfdbfe); }
    .position-dialog__footer { align-items:center; background:var(--background-page, #f8fafc); border-top:1px solid var(--border-subtle, #e5e7eb); display:flex; justify-content:space-between; padding:16px 20px; width:100%; }
    .position-dialog__footer-info { align-items:center; color:var(--text-secondary); display:flex; font-size:var(--font-size-xs); gap:6px; line-height:16px; min-width:0; }
    .position-dialog__footer-info i { color:var(--primary-500); }
    .position-dialog__footer-actions { display:flex; gap:16px; }
    :host ::ng-deep .app-position-dialog .p-dialog-header { background:var(--background-page, #f8fafc); border-bottom:1px solid var(--border-subtle, #e5e7eb); padding:16px 20px; }
    :host ::ng-deep .app-position-dialog .p-dialog-content { padding:0; }
    :host ::ng-deep .app-position-dialog .p-dialog-footer { padding:0; }
    :host ::ng-deep .app-position-dialog .position-dialog__btn-cancel.p-button { border-radius:9px; font-size:14px; height:40px; padding:0 13px; }
    :host ::ng-deep .app-position-dialog .position-dialog__btn-primary.p-button { border-radius:var(--radius-md, 6px); font-size:13px; height:40px; padding:0 12px; }
    @media (max-width:900px){ .entity-tab{ padding:var(--space-4);} .entity-tab-header,.entity-tab-card-header{ flex-direction:column; align-items:stretch;} .entity-tab-footer{ flex-direction:column; align-items:flex-start; padding:var(--space-3) var(--space-4);} .position-dialog__body{ padding:16px;} .position-dialog__footer{ align-items:flex-start; flex-direction:column; gap:12px;} .position-dialog__footer-actions{ width:100%; } :host ::ng-deep .app-position-dialog .position-dialog__btn-cancel.p-button, :host ::ng-deep .app-position-dialog .position-dialog__btn-primary.p-button { flex:1; justify-content:center; width:100%; } }
  `]
})
export class JobPositionTabComponent implements OnInit {
  private fb = inject(FormBuilder);
  private jobPositionService = inject(JobPositionService);
  private contextService = inject(CompanyContextService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  // Signals
  jobPositions = signal<JobPosition[]>([]);
  searchTerm = signal('');
  loading = signal(false);
  dialogVisible = signal(false);
  submitLoading = signal(false);
  displayedJobPositions = computed(() => {
    const query = this.searchTerm().trim().toLowerCase();
    if (!query) return this.jobPositions();
    return this.jobPositions().filter((j) => j.name?.toLowerCase().includes(query));
  });

  // Form
  jobPositionForm!: FormGroup;
  isEditMode = false;
  currentJobPositionId: number | null = null;

  ngOnInit() {
    this.initForm();
    this.loadJobPositions();
    // Reload when company context changes
    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadJobPositions());
  }

  private initForm() {
    this.jobPositionForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]]
    });
  }

  loadJobPositions() {
    const companyId = this.contextService.companyId();
    if (!companyId) return;
    this.loading.set(true);
    this.jobPositionService.getByCompany(Number(companyId)).subscribe({
      next: (company) => {
        this.jobPositions.set([...company]);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('company.jobPositions.messages.loadError')
        });
      }
    });
  }

  openCreateDialog() {
    this.isEditMode = false;
    this.currentJobPositionId = null;
    this.jobPositionForm.reset();
    this.dialogVisible.set(true);
  }

  onSearchList(value: string) {
    this.searchTerm.set(value ?? '');
  }

  openEditDialog(jobPosition: JobPosition) {
    this.isEditMode = true;
    this.currentJobPositionId = jobPosition.id;
    this.jobPositionForm.patchValue({
      name: jobPosition.name
    });
    this.dialogVisible.set(true);
  }

  saveJobPosition() {
    if (this.jobPositionForm.invalid) {
      this.jobPositionForm.markAllAsTouched();
      return;
    }

    const companyId = this.contextService.companyId();
    if (!companyId) {
      this.messageService.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('company.jobPositions.messages.companyIdMissing')
      });
      return;
    }

    this.submitLoading.set(true);

    const formValue = this.jobPositionForm.value.name;
    const name = typeof formValue === 'string' ? formValue : formValue?.name;

    const payload = {
      Name: name,
      CompanyId: this.isEditMode ? undefined : Number(companyId)
    };

    const request = this.isEditMode && this.currentJobPositionId
      ? this.jobPositionService.update(this.currentJobPositionId, payload)
      : this.jobPositionService.create({ ...payload, CompanyId: Number(companyId) });

    request.subscribe({
      next: (res) => {
        this.submitLoading.set(false);
        this.dialogVisible.set(false);
        this.loadJobPositions();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.isEditMode
            ? this.translate.instant('company.jobPositions.messages.updateSuccess')
            : this.translate.instant('company.jobPositions.messages.createSuccess')
        });
      },
      error: (err: HttpErrorResponse) => {
        this.submitLoading.set(false);
        this.handleError(err);
      }
    });
  }

  confirmDelete(jobPosition: JobPosition) {
    this.confirmationService.confirm({
      message: this.translate.instant('company.jobPositions.deleteConfirm'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteJobPosition(jobPosition.id);
      }
    });
  }

  private deleteJobPosition(id: number) {
    this.jobPositionService.delete(id).subscribe({
      next: () => {
        this.loadJobPositions();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('company.jobPositions.messages.deleteSuccess')
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
      detail = this.translate.instant('company.jobPositions.messages.alreadyExists');
    } else if (err.status === 400) {
      detail = err.error?.title || err.error?.message || this.translate.instant('company.jobPositions.messages.invalidRequest');
    } else if (err.status === 403) {
      detail = this.translate.instant('company.jobPositions.messages.accessDenied');
    } else if (err.status === 404) {
      detail = this.translate.instant('company.jobPositions.messages.notFound');
    }

    this.messageService.add({
      severity: 'error',
      summary: this.translate.instant('common.error'),
      detail
    });
  }
}
