import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ContractType } from '../../../../core/models/contract-type.model';
import { CompanyContextService } from '../../../../core/services/companyContext.service';
import { ContractTypeService } from '../../../../core/services/contract-type.service';
import { LegalContractTypeOption, LegalContractTypeService } from '../../../../core/services/legal-contract-type.service';
import { StateEmploymentProgramOption, StateEmploymentProgramService } from '../../../../core/services/state-employment-program.service';

@Component({
  selector: 'app-contract-type-tab',
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
    SelectModule,
    ToastModule,
    TagModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './contract-type-tab.component.html',
  styles: [`
    .entity-tab { display:flex; flex-direction:column; gap:var(--space-6); padding:var(--space-4) var(--space-6) var(--space-6); }
    .entity-tab-header { display:flex; align-items:flex-start; justify-content:space-between; gap:var(--space-4); }
    .entity-tab-title-row { display:flex; align-items:center; gap:var(--space-2); }
    .entity-tab-title { margin:0; color:var(--text-primary); font-size:var(--font-size-xl); font-weight:600; line-height:1.1; }
    .entity-tab-count-badge { border-radius:var(--radius-full); background:var(--info-light); color:var(--primary-500); font-size:var(--font-size-xs); font-weight:500; line-height:16px; padding:4px 8px; white-space:nowrap; }
    .entity-tab-subtitle { margin:var(--space-2) 0 0; color:var(--neutral-500); font-size:var(--font-size-sm); font-weight:500; }
    .entity-tab-card { border:1px solid var(--border-subtle); border-radius:var(--radius-xl); overflow:hidden; background:var(--bg-element); }
    .entity-tab-card-header { background:var(--bg-page); border-bottom:1px solid var(--border-subtle); padding:12px var(--space-4) 13px; display:flex; align-items:center; justify-content:space-between; gap:var(--space-4); }
    .entity-tab-card-title { margin:0; color:var(--neutral-800); font-size:var(--font-size-sm); font-weight:600; line-height:20px; }
    .entity-tab-card-description { margin:2px 0 0; color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .entity-tab-search-wrap { width:374px; max-width:100%; border:1px solid var(--neutral-200); border-radius:var(--radius-lg); background:var(--bg-element); display:flex; align-items:center; gap:var(--space-3); padding:0 var(--space-3); }
    .entity-tab-search-wrap i { color:var(--neutral-400); font-size:14px; }
    .entity-tab-search-input { width:100%; height:40px; border:0; outline:0; background:transparent; color:var(--text-primary); font-size:var(--font-size-sm); }
    .entity-tab-list { display:flex; flex-direction:column; }
    .entity-tab-row { display:flex; align-items:center; justify-content:space-between; gap:var(--space-3); padding:12px var(--space-4); border-bottom:1px solid var(--neutral-100); }
    .entity-tab-row-main { border:0; background:transparent; padding:0; width:100%; display:flex; align-items:center; gap:var(--space-4); text-align:left; }
    .entity-tab-row-icon { width:40px; height:40px; border-radius:var(--radius-lg); background:var(--primary-50); display:grid; place-items:center; color:var(--primary-500); }
    .entity-tab-row-title { margin:0; color:var(--neutral-800); font-size:var(--font-size-sm); font-weight:500; line-height:16px; }
    .entity-tab-row-subtitle { margin:2px 0 0; color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .entity-tab-row-lock { color:var(--text-secondary); font-size:12px; padding-right:8px; }
    .entity-tab-footer { background:var(--bg-page); border-top:1px solid var(--border-subtle); padding:var(--space-3) var(--space-6); display:flex; align-items:center; justify-content:space-between; gap:var(--space-3); color:var(--text-secondary); font-size:var(--font-size-xs); line-height:16px; }
    .entity-tab-footer-right { display:inline-flex; align-items:center; gap:var(--space-2); }
    .entity-tab-footer-right i { color:var(--primary-500); }
    .entity-tab-loading, .entity-tab-empty { min-height:180px; display:flex; flex-direction:column; align-items:center; justify-content:center; gap:var(--space-2); color:var(--text-secondary); }
    .entity-tab-loading i { font-size:24px; }
    .entity-tab-empty i { font-size:28px; color:var(--neutral-400); }
    .entity-tab-empty h3 { margin:0; color:var(--text-primary); font-size:var(--font-size-base); font-weight:600; }
    .entity-tab-empty p { margin:0; color:var(--text-secondary); font-size:var(--font-size-sm); text-align:center; }
    @media (max-width:900px){ .entity-tab{ padding:var(--space-4);} .entity-tab-header,.entity-tab-card-header{ flex-direction:column; align-items:stretch;} .entity-tab-footer{ flex-direction:column; align-items:flex-start; padding:var(--space-3) var(--space-4);} }
  `]
})
export class ContractTypeTabComponent implements OnInit {
  private fb = inject(FormBuilder);
  private contractTypeService = inject(ContractTypeService);
  private legalContractTypeService = inject(LegalContractTypeService);
  private stateEmploymentProgramService = inject(StateEmploymentProgramService);
  private contextService = inject(CompanyContextService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  // Signals
  predefinedContractTypes = signal<ContractType[]>([]);
  filteredContractTypes = signal<ContractType[]>([]);
  contractTypes = signal<ContractType[]>([]);
  searchTerm = signal('');
  legalContractTypeOptions = signal<LegalContractTypeOption[]>([]);
  stateEmploymentProgramOptions = signal<StateEmploymentProgramOption[]>([]);
  loading = signal(false);
  dialogVisible = signal(false);
  submitLoading = signal(false);
  displayedContractTypes = computed(() => {
    const query = this.searchTerm().trim().toLowerCase();
    if (!query) return this.contractTypes();
    return this.contractTypes().filter((c) => c.contractTypeName?.toLowerCase().includes(query));
  });

  // Form
  contractTypeForm!: FormGroup;
  isEditMode = false;
  currentContractTypeId: number | null = null;

  ngOnInit() {
    this.initForm();
    this.loadReferentialData();
    this.loadContractTypes();
    // Reload when company context changes
    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadContractTypes());
  }

  private initForm() {
    this.contractTypeForm = this.fb.group({
      contractTypeName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      legalContractTypeId: [null],
      stateEmploymentProgramId: [null]
    });
  }

  private loadReferentialData() {
    forkJoin({
      legalTypes: this.legalContractTypeService.getOptions().pipe(catchError((err) => {
        return of([]);
      })),
      programs: this.stateEmploymentProgramService.getOptions().pipe(catchError((err) => {
        return of([]);
      }))
    }).subscribe({
      next: ({ legalTypes, programs }) => {
        this.legalContractTypeOptions.set(legalTypes);
        this.stateEmploymentProgramOptions.set(programs);
      },
      error: (err) => {
        alert('Failed to load referential data');
      }
    });
  }

  loadContractTypes() {
    const companyId = this.contextService.companyId();
    if (!companyId) return;

    this.loading.set(true);
    this.predefinedContractTypes.set([]);
    this.contractTypeService.getByCompany(Number(companyId)).subscribe({
      next: (company) => {
        this.contractTypes.set([...company]);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('company.contractTypes.messages.loadError')
        });
      }
    });
  }

  openCreateDialog() {
    this.isEditMode = false;
    this.currentContractTypeId = null;
    this.contractTypeForm.reset();
    this.dialogVisible.set(true);
  }

  searchContractTypes(event: any) {
    const query = event.query.toLowerCase();
    const filtered = this.predefinedContractTypes().filter(c =>
      c.contractTypeName.toLowerCase().includes(query)
    );
    this.filteredContractTypes.set(filtered);
  }

  onSearchList(value: string) {
    this.searchTerm.set(value ?? '');
  }

  openEditDialog(contractType: ContractType) {
    this.isEditMode = true;
    this.currentContractTypeId = contractType.id;
    this.contractTypeForm.patchValue({
      contractTypeName: contractType.contractTypeName,
      legalContractTypeId: contractType.legalContractTypeId,
      stateEmploymentProgramId: contractType.stateEmploymentProgramId
    });
    this.dialogVisible.set(true);
  }

  saveContractType() {
    if (this.contractTypeForm.invalid) {
      this.contractTypeForm.markAllAsTouched();
      return;
    }

    const companyId = this.contextService.companyId();
    if (!companyId) {
      this.messageService.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('company.contractTypes.messages.companyIdMissing')
      });
      return;
    }

    this.submitLoading.set(true);

    const formValue = this.contractTypeForm.value.contractTypeName;
    const name = typeof formValue === 'string' ? formValue : formValue?.contractTypeName;

    if (this.isEditMode && this.currentContractTypeId) {
      // Update only needs the name
      const updatePayload = {
        ContractTypeName: name,
        LegalContractTypeId: this.contractTypeForm.value.legalContractTypeId,
        StateEmploymentProgramId: this.contractTypeForm.value.stateEmploymentProgramId
      };

      this.contractTypeService.update(this.currentContractTypeId, updatePayload).subscribe({
        next: () => {
          this.submitLoading.set(false);
          this.dialogVisible.set(false);
          this.loadContractTypes();
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('company.contractTypes.messages.updateSuccess')
          });
        },
        error: (err: HttpErrorResponse) => {
          this.submitLoading.set(false);
          this.handleError(err);
        }
      });
    } else {
      // Create needs name and company ID
      const createPayload = {
        ContractTypeName: name,
        CompanyId: Number(companyId),
        LegalContractTypeId: this.contractTypeForm.value.legalContractTypeId,
        StateEmploymentProgramId: this.contractTypeForm.value.stateEmploymentProgramId
      };

      this.contractTypeService.create(createPayload).subscribe({
        next: () => {
          this.submitLoading.set(false);
          this.dialogVisible.set(false);
          this.loadContractTypes();
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('company.contractTypes.messages.createSuccess')
          });
        },
        error: (err: HttpErrorResponse) => {
          this.submitLoading.set(false);
          this.handleError(err);
        }
      });
    }
  }

  confirmDelete(contractType: ContractType) {
    this.confirmationService.confirm({
      message: this.translate.instant('company.contractTypes.deleteConfirm'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteContractType(contractType.id);
      }
    });
  }

  private deleteContractType(id: number) {
    this.contractTypeService.delete(id).subscribe({
      next: () => {
        this.loadContractTypes();
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('company.contractTypes.messages.deleteSuccess')
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
      detail = this.translate.instant('company.contractTypes.messages.alreadyExists');
    } else if (err.status === 400) {
      // Often used for validation or "cannot delete because used"
      detail = err.error?.message || err.error?.title || this.translate.instant('company.contractTypes.messages.invalidRequest');
    } else if (err.status === 403) {
      detail = this.translate.instant('company.contractTypes.messages.accessDenied');
    } else if (err.status === 404) {
      detail = this.translate.instant('company.contractTypes.messages.notFound');
    }

    this.messageService.add({
      severity: 'error',
      summary: this.translate.instant('common.error'),
      detail
    });
  }
}
