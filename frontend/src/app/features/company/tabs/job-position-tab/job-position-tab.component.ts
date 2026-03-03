import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { JobPositionService } from '../../../../core/services/job-position.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CompanyContextService } from '../../../../core/services/companyContext.service';
import { JobPosition } from '../../../../core/models/job-position.model';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

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
    AutoCompleteModule,
    ToastModule,
    ConfirmDialogModule,
    TagModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './job-position-tab.component.html',
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
  predefinedJobPositions = signal<JobPosition[]>([]);
  filteredJobPositions = signal<JobPosition[]>([]);
  loading = signal(false);
  dialogVisible = signal(false);
  submitLoading = signal(false);
  
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
    this.predefinedJobPositions.set([]);
    this.jobPositionService.getByCompany(Number(companyId)).subscribe({
      next: (company) => {
        this.jobPositions.set([...company]);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading job positions', err);
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

  searchJobPositions(event: any) {
    const query = event.query.toLowerCase();
    const filtered = this.predefinedJobPositions().filter(p => 
      p.name.toLowerCase().includes(query)
    );
    this.filteredJobPositions.set(filtered);
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
