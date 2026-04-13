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
import {
  LeaveType,
  LeaveTypePolicy,
  LeaveAccrualMethod,
  LeaveTypePolicyCreateDto,
  LeaveTypePolicyPatchDto
} from '@app/core/models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-leave-policy-form',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonModule,
    ToastModule,
    CheckboxModule,
    InputFieldComponent,
    SelectFieldComponent
  ],
  providers: [MessageService],
  templateUrl: './leave-policy-form.html',
  styleUrl: './leave-policy-form.css'
})
export class LeavePolicyFormPage implements OnInit {
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
  readonly policyId = signal<number | null>(null);
  readonly leaveTypeId = signal<number | null>(null);
  readonly policy = signal<LeaveTypePolicy | null>(null);
  readonly leaveType = signal<LeaveType | null>(null);

  // Computed
  readonly isEditMode = computed(() => this.policyId() !== null);
  readonly isConfigureMode = computed(() => this.leaveTypeId() !== null && !this.isEditMode());
  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');
  readonly pageTitle = computed(() => {
    if (this.isEditMode()) {
      return this.translate.instant('leave.policy.editTitle');
    }
    return this.translate.instant('leave.policy.configureTitle');
  });

  // Form
  form!: FormGroup;

  // Options
  readonly accrualMethodOptions = [
    { label: 'Mensuel', value: LeaveAccrualMethod.Monthly },
    { label: 'Annuel', value: LeaveAccrualMethod.Annual },
    { label: 'Aucun', value: LeaveAccrualMethod.None }
  ];

  ngOnInit(): void {
    this.initForm();

    // Check route params
    const id = this.route.snapshot.paramMap.get('id');
    const configureLeaveTypeId = this.route.snapshot.paramMap.get('leaveTypeId');

    if (id && id !== 'configure') {
      // Edit mode: load existing policy
      this.policyId.set(parseInt(id, 10));
      this.loadPolicy();
    } else if (configureLeaveTypeId) {
      // Configure mode: create policy for a leave type
      this.leaveTypeId.set(parseInt(configureLeaveTypeId, 10));
      this.loadLeaveType();
      this.checkExistingPolicy();
    }
  }

  private initForm(): void {
    this.form = this.fb.group({
      isEnabled: [true],
      accrualMethod: [LeaveAccrualMethod.Monthly, Validators.required],
      daysPerMonth: [1.5, [Validators.required, Validators.min(0), Validators.max(31)]],
      daysPerService5Years: [1.5, [Validators.required, Validators.min(0)]],
      requiresBalance: [true],
      annualCapDays: [30, [Validators.required, Validators.min(1), Validators.max(365)]],
      allowCarryover: [true],
      maxCarryoverYears: [2, [Validators.min(0), Validators.max(10)]],
      minConsecutiveDays: [12, [Validators.required, Validators.min(0)]],
      useWorkingCalendar: [true]
    });
  }

  private loadPolicy(): void {
    const id = this.policyId();
    if (!id) return;

    this.isLoading.set(true);
    this.leaveService.getPolicyById(id).subscribe({
      next: (policy: any) => {
        this.policy.set(policy);
        // leaveType needs to be fetched separately by leaveTypeId if needed
        if (policy.LeaveTypeId) {
          this.leaveTypeId.set(policy.LeaveTypeId);
          this.loadLeaveType(policy.LeaveTypeId);
        }
        this.patchForm(policy);
        this.isLoading.set(false);
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger la politique'
        });
        this.isLoading.set(false);
        this.goBack();
      }
    });
  }

  private loadLeaveType(id?: number): void {
    const leaveTypeIdToLoad = id || this.leaveTypeId();
    if (!leaveTypeIdToLoad) return;

    this.leaveService.getById(leaveTypeIdToLoad).subscribe({
      next: (leaveType: LeaveType) => {
        this.leaveType.set(leaveType);
      },
      error: (err: any) => {
      }
    });
  }

  private checkExistingPolicy(): void {
    const leaveTypeId = this.leaveTypeId();
    if (!leaveTypeId) return;

    this.leaveService.getPoliciesByLeaveType(leaveTypeId).subscribe({
      next: (policies: any[]) => {
        const policy = policies.length > 0 ? policies[0] : null;
        if (policy) {
          // Policy already exists, switch to edit mode
          this.policyId.set(policy.Id);
          this.policy.set(policy);
          this.patchForm(policy);
        }
      },
      error: () => {
        // No existing policy, continue with create mode
      }
    });
  }

  private patchForm(policy: LeaveTypePolicy): void {
    this.form.patchValue({
      isEnabled: policy.IsEnabled,
      accrualMethod: policy.AccrualMethod,
      daysPerMonth: policy.DaysPerMonthAdult,
      daysPerService5Years: policy.BonusDaysPerYearAfter5Years,
      requiresBalance: policy.RequiresBalance,
      annualCapDays: policy.AnnualCapDays,
      allowCarryover: policy.AllowCarryover,
      maxCarryoverYears: policy.MaxCarryoverYears,
      minConsecutiveDays: policy.MinConsecutiveDays,
      useWorkingCalendar: policy.UseWorkingCalendar
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);

    if (this.isEditMode() || this.policy()) {
      this.updatePolicy();
    } else {
      this.createPolicy();
    }
  }

  private createPolicy(): void {
    const leaveTypeId = this.leaveTypeId();
    if (!leaveTypeId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'Type de congé non spécifié'
      });
      this.isSaving.set(false);
      return;
    }

    const request: LeaveTypePolicyCreateDto = {
      LeaveTypeId: leaveTypeId,
      CompanyId: parseInt(this.contextService.companyId() || '0', 10),
      IsEnabled: this.form.value.isEnabled,
      AccrualMethod: this.form.value.accrualMethod,
      DaysPerMonthAdult: this.form.value.daysPerMonth,
      DaysPerMonthMinor: this.form.value.daysPerMonth,
      BonusDaysPerYearAfter5Years: this.form.value.daysPerService5Years,
      RequiresEligibility6Months: false,
      RequiresBalance: this.form.value.requiresBalance,
      AnnualCapDays: this.form.value.annualCapDays,
      AllowCarryover: this.form.value.allowCarryover,
      MaxCarryoverYears: this.form.value.maxCarryoverYears,
      MinConsecutiveDays: this.form.value.minConsecutiveDays,
      UseWorkingCalendar: this.form.value.useWorkingCalendar
    };

    this.leaveService.createPolicy(request).subscribe({
      next: (policy: any) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Politique créée avec succès'
        });
        this.isSaving.set(false);
        this.router.navigate([`${this.routePrefix()}/leave/types`, leaveTypeId]);
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err.error?.message || 'Échec de la création'
        });
        this.isSaving.set(false);
      }
    });
  }

  private updatePolicy(): void {
    const id = this.policyId() || this.policy()?.Id;
    if (!id) return;

    const request: LeaveTypePolicyPatchDto = {
      IsEnabled: this.form.value.isEnabled,
      AccrualMethod: this.form.value.accrualMethod,
      DaysPerMonthAdult: this.form.value.daysPerMonth,
      DaysPerMonthMinor: this.form.value.daysPerMonth,
      BonusDaysPerYearAfter5Years: this.form.value.daysPerService5Years,
      RequiresEligibility6Months: false,
      RequiresBalance: this.form.value.requiresBalance,
      AnnualCapDays: this.form.value.annualCapDays,
      AllowCarryover: this.form.value.allowCarryover,
      MaxCarryoverYears: this.form.value.maxCarryoverYears,
      MinConsecutiveDays: this.form.value.minConsecutiveDays,
      UseWorkingCalendar: this.form.value.useWorkingCalendar
    };

    this.leaveService.updatePolicy(id, request).subscribe({
      next: (policy) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Politique mise à jour'
        });
        this.isSaving.set(false);

        const leaveTypeId = this.leaveTypeId() || policy.LeaveTypeId;
        this.router.navigate([`${this.routePrefix()}/leave/types`, leaveTypeId]);
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err.error?.message || 'Échec de la mise à jour'
        });
        this.isSaving.set(false);
      }
    });
  }

  getLeaveTypeName(): string {
    const leaveType = this.leaveType();
    if (!leaveType) return '';
    // Return the leave name
    return leaveType.LeaveName || '';
  }

  goBack(): void {
    const leaveTypeId = this.leaveTypeId() || this.policy()?.LeaveTypeId;
    if (leaveTypeId) {
      this.router.navigate([`${this.routePrefix()}/leave/types`, leaveTypeId]);
    } else {
      this.router.navigate([`${this.routePrefix()}/leave/policies`]);
    }
  }
}
