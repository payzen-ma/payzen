import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ContractTypeService } from '../../../../core/services/contract-type.service';
import { LegalContractTypeService, LegalContractTypeOption } from '../../../../core/services/legal-contract-type.service';
import { StateEmploymentProgramService, StateEmploymentProgramOption } from '../../../../core/services/state-employment-program.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CompanyContextService } from '../../../../core/services/companyContext.service';
import { ContractType } from '../../../../core/models/contract-type.model';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

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
  legalContractTypeOptions = signal<LegalContractTypeOption[]>([]);
  stateEmploymentProgramOptions = signal<StateEmploymentProgramOption[]>([]);
  loading = signal(false);
  dialogVisible = signal(false);
  submitLoading = signal(false);
  
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
        console.error('[ContractTypeTab] Error loading legal types:', err);
        return of([]);
      })),
      programs: this.stateEmploymentProgramService.getOptions().pipe(catchError((err) => {
        console.error('[ContractTypeTab] Error loading programs:', err);
        return of([]);
      }))
    }).subscribe({
      next: ({ legalTypes, programs }) => {
        this.legalContractTypeOptions.set(legalTypes);
        this.stateEmploymentProgramOptions.set(programs);
      },
      error: (err) => {
        console.error('[ContractTypeTab] Error loading referential data', err);
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
        console.error('Error loading contract types', err);
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
