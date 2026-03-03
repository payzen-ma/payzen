import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { CheckboxModule } from 'primeng/checkbox';
import { InputFieldComponent } from '@app/shared/components/form-controls/input-field';
import { SelectFieldComponent } from '@app/shared/components/form-controls/select-field';
import { LeaveService } from '@app/core/services/leave.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { LeaveType, LeaveTypeCreateDto, LeaveTypePatchDto, LeaveScope } from '@app/core/models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-leave-type-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonModule,
    ToastModule,
    CheckboxModule,
    InputFieldComponent
  ],
  providers: [MessageService],
  templateUrl: './leave-type-form.html',
  styleUrl: './leave-type-form.css'
})
export class LeaveTypeFormPage implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private leaveService = inject(LeaveService);
  private contextService = inject(CompanyContextService);
  private messageService = inject(MessageService);
  private translate = inject(TranslateService);

  // State
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly leaveTypeId = signal<number | null>(null);
  readonly leaveType = signal<LeaveType | null>(null);

  // Computed
  readonly isEditMode = computed(() => this.leaveTypeId() !== null);
  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');
  readonly pageTitle = computed(() => 
    this.isEditMode() 
      ? this.translate.instant('leave.types.edit') 
      : this.translate.instant('leave.types.create')
  );

  // Form
  form!: FormGroup;

  // Options
  readonly scopeOptions = [
    { label: 'Entreprise', value: 'Company' }
    // Global scope is not available for creation - it's system-defined
  ];

  ngOnInit(): void {
    this.initForm();

    // Check if we're in edit mode
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'create') {
      this.leaveTypeId.set(parseInt(id, 10));
      this.loadLeaveType();
    }
  }

  private initForm(): void {
    this.form = this.fb.group({
      leaveCode: ['', [Validators.required, Validators.maxLength(50)]],
      leaveNameFr: ['', [Validators.required, Validators.maxLength(100)]],
      leaveNameEn: ['', [Validators.required, Validators.maxLength(100)]],
      leaveNameAr: ['', [Validators.required, Validators.maxLength(100)]],
      leaveDescription: ['', [Validators.required, Validators.maxLength(500)]],
      scope: ['Company'],
      isPaid: [true],
      requiresBalance: [false],
      requiresEligibility6Months: [false],
      isActive: [true]
    });
  }

  private loadLeaveType(): void {
    const id = this.leaveTypeId();
    if (!id) return;

    this.isLoading.set(true);
    this.leaveService.getById(id).subscribe({
      next: (leaveType: LeaveType) => {
        this.leaveType.set(leaveType);
        this.patchForm(leaveType);
        this.isLoading.set(false);

        // Disable form if it's a global type
        if (leaveType.Scope === LeaveScope.Global) {
          this.form.disable();
        }
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger le type de congé'
        });
        this.isLoading.set(false);
        this.goBack();
      }
    });
  }

  private patchForm(leaveType: LeaveType): void {
    this.form.patchValue({
      leaveCode: leaveType.LeaveCode,
      leaveName: leaveType.LeaveName,
      leaveDescription: leaveType.LeaveDescription,
      isActive: leaveType.IsActive
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);

    if (this.isEditMode()) {
      this.updateLeaveType();
    } else {
      this.createLeaveType();
    }
  }

  private createLeaveType(): void {
    const request: LeaveTypeCreateDto = {
      LeaveCode: this.form.value.leaveCode,
      LeaveName: this.form.value.leaveName,
      LeaveDescription: this.form.value.leaveDescription,
      Scope: LeaveScope.Company,
      IsActive: this.form.value.isActive
    };

    this.leaveService.create(request).subscribe({
      next: (leaveType: LeaveType) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Type de congé créé avec succès'
        });
        this.isSaving.set(false);
        this.router.navigate([`${this.routePrefix()}/leave/types`, leaveType.Id]);
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err.error?.message || 'Échec de la création'
        });
        this.isSaving.set(false);
      }
    });
  }

  private updateLeaveType(): void {
    const id = this.leaveTypeId();
    if (!id) return;

    const request: LeaveTypePatchDto = {
      LeaveCode: this.form.value.leaveCode,
      LeaveName: this.form.value.leaveName,
      LeaveDescription: this.form.value.leaveDescription,
      IsActive: this.form.value.isActive
    };

    this.leaveService.update(id, request).subscribe({
      next: (leaveType: LeaveType) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Type de congé mis à jour'
        });
        this.isSaving.set(false);
        this.router.navigate([`${this.routePrefix()}/leave/types`, leaveType.Id]);
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err.error?.message || 'Échec de la mise à jour'
        });
        this.isSaving.set(false);
      }
    });
  }

  goBack(): void {
    this.router.navigate([`${this.routePrefix()}/leave/types`]);
  }

  isGlobalType(): boolean {
    return this.leaveType()?.Scope === LeaveScope.Global;
  }
}
