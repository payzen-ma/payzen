import { Component, signal, computed, OnInit, inject, DestroyRef, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { CheckboxModule } from 'primeng/checkbox';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TagComponent } from '@app/shared/components/tag/tag.component';
import { TagVariant } from '@app/shared/components/tag/tag.types';
import { EmptyState } from '@app/shared/components/empty-state/empty-state';
import { LeaveService } from '@app/core/services/leave.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { LeaveTypePolicy, LeaveTypePolicyCreateDto, LeaveTypePolicyPatchDto, LeaveAccrualMethod, LeaveType, LeaveScope } from '@app/core/models';
import { MessageService, ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-leave-policies',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    ToastModule,
    ConfirmDialogModule,
    ToggleSwitchModule,
    CheckboxModule,
    DialogModule,
    SelectModule,
    TagComponent,
    EmptyState,
    IconFieldModule,
    InputIconModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './leave-policies.html',
  styleUrl: './leave-policies.css'
})
export class LeavePoliciesPage implements OnInit {
  private leaveService = inject(LeaveService);
  private contextService = inject(CompanyContextService);
  private destroyRef = inject(DestroyRef);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private fb = inject(FormBuilder);

  // Output events for parent component
  readonly onEditPolicy = output<LeaveTypePolicy>();
  readonly onConfigurePolicy = output<number>();

  // State
  readonly policies = signal<LeaveTypePolicy[]>([]);
  readonly leaveTypes = signal<LeaveType[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchQuery = signal('');

  // Dialog state
  readonly showDialog = signal(false);
  readonly isSaving = signal(false);
  readonly selectedPolicy = signal<LeaveTypePolicy | null>(null);
  readonly isEditMode = computed(() => this.selectedPolicy() !== null);

  // Form
  form: FormGroup = this.fb.group({
    leaveTypeId: [null, Validators.required],
    isEnabled: [true],
    accrualMethod: [LeaveAccrualMethod.Monthly],
    daysPerMonthAdult: [1.5, [Validators.required, Validators.min(0)]],
    daysPerMonthMinor: [2.0, [Validators.required, Validators.min(0)]],
    bonusDaysPerYearAfter5Years: [1.5, [Validators.min(0)]],
    requiresEligibility6Months: [false],
    requiresBalance: [true],
    annualCapDays: [30, [Validators.required, Validators.min(0)]],
    allowCarryover: [true],
    maxCarryoverYears: [2, [Validators.min(0)]],
    minConsecutiveDays: [12, [Validators.min(0)]],
    useWorkingCalendar: [true]
  });

  // Accrual method options for dropdown
  readonly accrualMethodOptions = computed(() => [
    { label: this.translate.instant('leave.accrual.none'), value: LeaveAccrualMethod.None },
    { label: this.translate.instant('leave.accrual.monthly'), value: LeaveAccrualMethod.Monthly },
    { label: this.translate.instant('leave.accrual.annual'), value: LeaveAccrualMethod.Annual },
    { label: this.translate.instant('leave.accrual.perPayPeriod'), value: LeaveAccrualMethod.PerPayPeriod },
    { label: this.translate.instant('leave.accrual.hoursWorked'), value: LeaveAccrualMethod.HoursWorked }
  ]);

  // Leave type options for dropdown (only show types without existing policy)
  readonly leaveTypeOptions = computed(() => {
    const existingPolicyTypeIds = this.policies().map(p => p.LeaveTypeId);
    return this.leaveTypes()
      .filter(lt => !existingPolicyTypeIds.includes(lt.Id) || this.selectedPolicy()?.LeaveTypeId === lt.Id)
      .map(lt => {
        const scopeLabel = lt.Scope === LeaveScope.Global
          ? this.translate.instant('leave.types.global')
          : this.translate.instant('leave.types.company');
        return {
          label: `${lt.LeaveName} (${lt.LeaveCode}) - ${scopeLabel}`,
          value: lt.Id
        };
      });
  });

  // Computed
  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  readonly filteredPolicies = computed(() => {
    let result = this.policies();

    if (this.searchQuery()) {
      const query = this.searchQuery().toLowerCase();
      result = result.filter(p =>
        String(p.LeaveTypeId).includes(query) ||
        this.getLeaveTypeName(p).toLowerCase().includes(query)
      );
    }

    return result;
  });

  readonly stats = computed(() => {
    const all = this.policies();
    return {
      total: all.length,
      enabled: all.filter(p => p.IsEnabled).length,
      disabled: all.filter(p => !p.IsEnabled).length
    };
  });

  readonly statCards = [
    {
      label: 'leave.policies.stats.total',
      accessor: (stats: any) => stats.total,
      icon: 'pi pi-list',
      iconColor: 'text-blue-500'
    },
    {
      label: 'leave.policies.stats.enabled',
      accessor: (stats: any) => stats.enabled,
      icon: 'pi pi-check-circle',
      iconColor: 'text-green-500'
    },
    {
      label: 'leave.policies.stats.disabled',
      accessor: (stats: any) => stats.disabled,
      icon: 'pi pi-times-circle',
      iconColor: 'text-red-500'
    }
  ];

  // Two-way binding
  get searchQueryModel(): string {
    return this.searchQuery();
  }

  set searchQueryModel(value: string) {
    this.searchQuery.set(value);
  }

  ngOnInit(): void {
    this.loadData();

    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadData();
      });

    // Debug: surveiller les changements du formulaire
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => {
    });

    this.form.get('daysPerMonthAdult')?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => {
    });

    this.form.get('requiresEligibility6Months')?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => {
    });
  }

  private loadData(): void {
    this.loadPolicies();
    this.loadLeaveTypes();
  }

  private loadLeaveTypes(): void {
    const companyId = parseInt(this.contextService.companyId() || '0', 10);

    if (!companyId) {
      return;
    }

    // Load available leave types for this company (includes both global and company-specific)
    this.leaveService.getAvailableForCompany(companyId).subscribe({
      next: (types: LeaveType[]) => {
        // Filter only active leave types
        const activeTypes = types.filter(t => t.IsActive);
        this.leaveTypes.set(activeTypes);
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: 'Impossible de charger les types de congés'
        });
      }
    });
  }

  loadPolicies(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const companyId = parseInt(this.contextService.companyId() || '0', 10);

    // If no company context, clear policies and stop loading
    if (!companyId) {
      this.policies.set([]);
      this.isLoading.set(false);
      return;
    }

    this.leaveService.getPoliciesByCompany(companyId).subscribe({
      next: (policies: LeaveTypePolicy[]) => {
        this.policies.set(policies || []);
        this.isLoading.set(false);
      },
      error: (err: any) => {
        this.error.set(err?.error?.message || 'Unable to load leave policies');
        this.policies.set([]);
        this.isLoading.set(false);
      }
    });
  }

  getLeaveTypeName(policy: LeaveTypePolicy): string {
    const lt = this.leaveTypes().find(t => t.Id === policy.LeaveTypeId);
    if (lt) {
      return `${lt.LeaveName} (${lt.LeaveCode})`;
    }
    return `Type ID: ${policy.LeaveTypeId}`;
  }

  getAccrualMethodLabel(method: LeaveAccrualMethod): string {
    switch (method) {
      case LeaveAccrualMethod.None:
        return this.translate.instant('leave.accrual.none');
      case LeaveAccrualMethod.Monthly:
        return this.translate.instant('leave.accrual.monthly');
      case LeaveAccrualMethod.Annual:
        return this.translate.instant('leave.accrual.annual');
      case LeaveAccrualMethod.PerPayPeriod:
        return this.translate.instant('leave.accrual.perPayPeriod');
      case LeaveAccrualMethod.HoursWorked:
        return this.translate.instant('leave.accrual.hoursWorked');
      default:
        return String(method);
    }
  }

  getEnabledVariant(isEnabled: boolean): TagVariant {
    return isEnabled ? 'success' : 'danger';
  }

  // Dialog CRUD methods
  addPolicy(): void {
    this.selectedPolicy.set(null);
    this.form.reset({
      leaveTypeId: null,
      isEnabled: true,
      accrualMethod: LeaveAccrualMethod.Monthly,
      daysPerMonthAdult: 1.5,
      daysPerMonthMinor: 2.0,
      bonusDaysPerYearAfter5Years: 1.5,
      requiresEligibility6Months: false,
      annualCapDays: 30,
      allowCarryover: true,
      maxCarryoverYears: 2,
      minConsecutiveDays: 12,
      useWorkingCalendar: true
    });
    this.form.enable();
    this.showDialog.set(true);
  }

  openEditPolicy(policy: LeaveTypePolicy, event?: Event): void {
    event?.stopPropagation();
    this.selectedPolicy.set(policy);

    // Debug logging

    // Ensure all controls are enabled first
    this.form.enable();

    this.patchForm(policy);

    // Only disable leave type selection on edit
    this.form.get('leaveTypeId')?.disable();

    // Debug form values after patch

    this.showDialog.set(true);
  }

  private patchForm(policy: LeaveTypePolicy): void {
    // Log pour debugging

    this.form.patchValue({
      leaveTypeId: policy.LeaveTypeId,
      isEnabled: policy.IsEnabled,
      accrualMethod: policy.AccrualMethod,
      daysPerMonthAdult: policy.DaysPerMonthAdult,
      daysPerMonthMinor: policy.DaysPerMonthMinor,
      bonusDaysPerYearAfter5Years: policy.BonusDaysPerYearAfter5Years,
      requiresEligibility6Months: policy.RequiresEligibility6Months,
      requiresBalance: policy.RequiresBalance,
      annualCapDays: policy.AnnualCapDays,
      allowCarryover: policy.AllowCarryover,
      maxCarryoverYears: policy.MaxCarryoverYears,
      minConsecutiveDays: policy.MinConsecutiveDays,
      useWorkingCalendar: policy.UseWorkingCalendar
    });

    // Forcer la mise à jour des validators et de l'état
    this.form.updateValueAndValidity();
    this.form.markAsPristine();
    this.form.markAsUntouched();

    // Log des valeurs finales
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.selectedPolicy.set(null);
    this.form.reset();
  }

  savePolicy(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);

    if (this.isEditMode() && this.selectedPolicy()) {
      this.updatePolicy();
    } else {
      this.createPolicy();
    }
  }

  private createPolicy(): void {
    const companyId = parseInt(this.contextService.companyId() || '0', 10);
    const request: LeaveTypePolicyCreateDto = {
      CompanyId: companyId || null,
      LeaveTypeId: this.form.value.leaveTypeId,
      IsEnabled: this.form.value.isEnabled,
      AccrualMethod: this.form.value.accrualMethod,
      DaysPerMonthAdult: this.form.value.daysPerMonthAdult,
      DaysPerMonthMinor: this.form.value.daysPerMonthMinor,
      BonusDaysPerYearAfter5Years: this.form.value.bonusDaysPerYearAfter5Years,
      RequiresEligibility6Months: this.form.value.requiresEligibility6Months,
      RequiresBalance: this.form.value.requiresBalance,
      AnnualCapDays: this.form.value.annualCapDays,
      AllowCarryover: this.form.value.allowCarryover,
      MaxCarryoverYears: this.form.value.maxCarryoverYears,
      MinConsecutiveDays: this.form.value.minConsecutiveDays,
      UseWorkingCalendar: this.form.value.useWorkingCalendar
    };

    this.leaveService.createPolicy(request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Politique créée avec succès'
        });
        this.isSaving.set(false);
        this.closeDialog();
        this.loadPolicies();
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
    const policy = this.selectedPolicy();
    if (!policy) return;

    const request: LeaveTypePolicyPatchDto = {
      IsEnabled: this.form.value.isEnabled,
      AccrualMethod: this.form.value.accrualMethod,
      DaysPerMonthAdult: this.form.value.daysPerMonthAdult,
      DaysPerMonthMinor: this.form.value.daysPerMonthMinor,
      BonusDaysPerYearAfter5Years: this.form.value.bonusDaysPerYearAfter5Years,
      RequiresEligibility6Months: this.form.value.requiresEligibility6Months,
      RequiresBalance: this.form.value.requiresBalance,
      AnnualCapDays: this.form.value.annualCapDays,
      AllowCarryover: this.form.value.allowCarryover,
      MaxCarryoverYears: this.form.value.maxCarryoverYears,
      MinConsecutiveDays: this.form.value.minConsecutiveDays,
      UseWorkingCalendar: this.form.value.useWorkingCalendar
    };

    this.leaveService.updatePolicy(policy.Id, request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Politique mise à jour'
        });
        this.isSaving.set(false);
        this.closeDialog();
        this.loadPolicies();
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

  toggleEnabled(policy: LeaveTypePolicy): void {
    const newState = !policy.IsEnabled;

    this.leaveService.updatePolicy(policy.Id, { IsEnabled: newState }).subscribe({
      next: (updated: any) => {
        // Update local state
        const current = this.policies();
        const index = current.findIndex(p => p.Id === policy.Id);
        if (index !== -1) {
          const updatedList = [...current];
          updatedList[index] = { ...updatedList[index], IsEnabled: newState };
          this.policies.set(updatedList);
        }

        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: newState ? 'Politique activée' : 'Politique désactivée'
        });
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err.error?.message || 'Échec de la mise à jour'
        });
      }
    });
  }

  editPolicy(policy: LeaveTypePolicy, event?: Event): void {
    event?.stopPropagation();
    this.openEditPolicy(policy, event);
  }

  configurePolicy(leaveTypeId: number): void {
    this.onConfigurePolicy.emit(leaveTypeId);
  }

  deletePolicy(policy: LeaveTypePolicy, event: Event): void {
    event.stopPropagation();

    this.confirmationService.confirm({
      message: `Êtes-vous sûr de vouloir supprimer cette politique pour "${this.getLeaveTypeName(policy)}" ?`,
      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'btn btn-danger',
      rejectButtonStyleClass: 'btn btn-secondary',
      accept: () => {
        this.leaveService.deletePolicy(policy.Id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Politique supprimée'
            });
            this.loadPolicies();
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail: err.error?.message || 'Échec de la suppression'
            });
          }
        });
      }
    });
  }

  viewLeaveType(policy: LeaveTypePolicy): void {
    if (policy.LeaveTypeId) {
      this.onConfigurePolicy.emit(policy.LeaveTypeId);
    }
  }

  clearSearch(): void {
    this.searchQuery.set('');
  }

  onEligibilityChange(event: any): void {
  }
}
