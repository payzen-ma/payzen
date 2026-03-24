import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators, FormArray } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CheckboxModule } from 'primeng/checkbox';
import { LeaveService } from '../../../../core/services/leave.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CompanyContextService } from '../../../../core/services/companyContext.service';
import { LeaveType, LeaveTypeCreateDto, LeaveTypePatchDto, LeaveTypePolicy, LeaveTypePolicyCreateDto, LeaveTypePolicyPatchDto, LeaveScope } from '../../../../core/models';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-leave-settings-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    TableModule,
    DialogModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    CheckboxModule
  ],
  providers: [MessageService, ConfirmationService],
  template: `
<div class="p-4 sm:p-6">
  <div class="max-w-6xl mx-auto space-y-6">
    <header class="flex items-start justify-between gap-4">
      <div class="flex items-start gap-4">
        <div class="shrink-0 size-12 rounded-xl bg-linear-to-br from-blue-500 to-blue-600 flex items-center justify-center shadow-sm">
          <i class="pi pi-briefcase text-white text-xl"></i>
        </div>
        <div>
          <h1 class="text-2xl font-semibold text-gray-900 leading-tight">Congés</h1>
          <p class="text-sm text-gray-500 mt-1">Gérer les types de congés et les politiques par entreprise</p>
        </div>
      </div>

      <div class="flex items-center gap-2">
        <button pButton type="button" icon="pi pi-plus" label="Create" (click)="openCreate()"></button>
        <button pButton type="button" icon="pi pi-cog" label="Create Policy" (click)="openCreatePolicy()"></button>
      </div>
    </header>

    <section class="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div class="px-6 py-4 border-b border-gray-100 bg-gray-50/50">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-4">
            <h2 class="text-lg font-medium">Leave Types in this company</h2>
            <button pButton type="button" icon="pi pi-file" label="Apply Morocco template" class="p-button-sm" (click)="applyTemplate()"></button>
          </div>
          <div>
            <button pButton type="button" icon="pi pi-plus" label="Create" (click)="openCreate()"></button>
          </div>
        </div>
      </div>

      <div class="p-4">
        <p-table [value]="leaveTypes()" [loading]="loading()">
          <ng-template pTemplate="header">
            <tr>
              <th>Name</th>
              <th>Code</th>
              <th>Scope</th>
              <th>Politique entreprise</th>
              <th>Règles légales</th>
              <th>Actions</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-lt>
            <tr>
              <td>{{ getLeaveTypeLabelFromObject(lt) }}</td>
              <td>{{ lt.leaveCode ?? lt.LeaveCode }}</td>
              <td>{{ lt.scope ?? lt.Scope }}</td>
              <td>
                <ng-container *ngIf="getCompanyPolicyForLeaveType(getLeaveTypeId(lt)) as cp; else noCp">
                  <div>Enabled: {{ cp.IsEnabled ? 'Yes' : 'No' }}</div>
                  <div>Accrual: {{ cp.AccrualMethod === 1 ? 'Monthly' : (cp.AccrualMethod === 2 ? 'Annual' : 'None') }}</div>
                  <div>Rate: {{ getAccrualRateLabel(cp) }}</div>
                  <div>Calendar: {{ cp.UseWorkingCalendar ? 'Working' : 'Calendar' }}</div>
                </ng-container>
                <ng-template #noCp>-</ng-template>
              </td>
              <td>
                <ng-container *ngIf="getRulesForLeaveType(getLeaveTypeId(lt)) as rules">
                  <div *ngIf="rules.length; else noRules">
                    <ul class="list-none p-0 m-0">
                      <li *ngFor="let r of rules; let i = index">
                        <span>{{ getRuleLabel(r) || ('Rule ' + (i+1)) }}</span>
                        <span *ngIf="i===1 && rules.length>2">…</span>
                      </li>
                    </ul>
                  </div>
                  <ng-template #noRules>-</ng-template>
                </ng-container>
              </td>
              <td>
                <button pButton type="button" label="Configure Company" (click)="openConfigure(lt)"></button>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </section>
  </div>
</div>

<p-dialog header="Leave Type" [visible]="dialogVisible()" (onHide)="dialogVisible.set(false)" [modal]="true" [closable]="true" [style]="{width: '500px'}">
  <form [formGroup]="form">
    <div class="p-fluid">
      <div class="p-field">
        <label>Code</label>
        <input pInputText formControlName="leaveCode" />
      </div>
      <div class="p-field">
        <label>Name</label>
        <input pInputText formControlName="leaveName" />
      </div>
      <div class="p-field">
        <label>Scope</label>
        <select formControlName="scope" class="p-inputtext p-component">
          <option value="Company">Company</option>
          <option value="Global">Global</option>
        </select>
      </div>
      <div class="p-field-checkbox">
        <p-checkbox formControlName="isPaid" [binary]="true"></p-checkbox>
        <label>Is Paid</label>
      </div>
      <div class="p-field-checkbox">
        <p-checkbox formControlName="requiresBalance" [binary]="true"></p-checkbox>
        <label>Requires Balance</label>
      </div>
      <div class="p-field-checkbox">
        <p-checkbox formControlName="requiresEligibility6Months" [binary]="true"></p-checkbox>
        <label>Requires Eligibility 6 Months</label>
      </div>
      <div class="p-field-checkbox">
        <p-checkbox formControlName="isActive" [binary]="true"></p-checkbox>
        <label>Is Active</label>
      </div>
    </div>
  </form>

  <p-footer>
    <button pButton type="button" label="Cancel" class="p-button-text" (click)="dialogVisible.set(false)"></button>
    <button pButton type="button" label="Save" (click)="save()" [disabled]="submitLoading()"></button>
  </p-footer>
</p-dialog>

<p-dialog [visible]="policyDialogVisible()" (onHide)="policyDialogVisible.set(false)" [modal]="true" [closable]="true" [style]="{width: '720px'}">
  <ng-template pTemplate="header">
    <div class="flex items-center justify-between w-full">
      <div class="flex items-center gap-3">
        <div class="size-10 rounded-lg bg-blue-50 flex items-center justify-center"><i class="pi pi-cog text-blue-600"></i></div>
        <div>
          <h3 class="text-lg font-semibold">{{ isPolicyEditMode ? 'Edit Policy' : 'Create Policy' }}</h3>
          <p class="text-xs text-gray-500">Définir les règles de congé pour ce type</p>
        </div>
      </div>
      <button type="button" class="p-button p-component p-button-text" (click)="policyDialogVisible.set(false)"><i class="pi pi-times"></i></button>
    </div>
  </ng-template>

  <form [formGroup]="policyForm">
    <div class="px-6 py-4 space-y-4">
      <!-- Section A: General -->
      <section class="border-b pb-4">
        <h4 class="font-semibold">General</h4>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-3">
          <div>
            <label class="block text-sm text-gray-700">Is Enabled</label>
            <p-checkbox formControlName="isEnabled" [binary]="true"></p-checkbox>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Paid?</label>
            <p-checkbox formControlName="isPaid" [binary]="true"></p-checkbox>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Requires Balance</label>
            <p-checkbox formControlName="requiresBalance" [binary]="true"></p-checkbox>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Use Working Calendar</label>
            <p-checkbox formControlName="useWorkingCalendar" [binary]="true"></p-checkbox>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Requires Approval Workflow</label>
            <p-checkbox formControlName="requiresApprovalWorkflow" [binary]="true"></p-checkbox>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Requires Attachment</label>
            <p-checkbox formControlName="requiresAttachment" [binary]="true"></p-checkbox>
          </div>
        </div>
      </section>

      <!-- Section B: Accrual & Balance (conditional) -->
      <section *ngIf="policyForm.value.accrualMethod !== 0" class="border-b pb-4">
        <h4 class="font-semibold">Accrual & Balance</h4>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-3">
          <div>
            <label class="block text-sm text-gray-700">Accrual Method</label>
            <select formControlName="accrualMethod" class="p-inputtext p-component w-full">
              <option [ngValue]="0">None</option>
              <option [ngValue]="1">Monthly</option>
              <option [ngValue]="2">Annual Grant</option>
            </select>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Days/Month (Adult)</label>
            <input pInputText type="number" formControlName="daysPerMonthAdult" class="w-full" />
          </div>
          <div>
            <label class="block text-sm text-gray-700">Days/Month (Minor)</label>
            <input pInputText type="number" formControlName="daysPerMonthMinor" class="w-full" />
          </div>
          <div>
            <label class="block text-sm text-gray-700">Bonus Days After 5 Years</label>
            <input pInputText type="number" formControlName="bonusDaysPerYearAfter5Years" class="w-full" />
          </div>
          <div>
            <label class="block text-sm text-gray-700">Requires Eligibility (6 months)</label>
            <p-checkbox formControlName="requiresEligibility6Months" [binary]="true"></p-checkbox>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Allow Negative Balance</label>
            <p-checkbox formControlName="allowNegativeBalance" [binary]="true"></p-checkbox>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Annual Cap Days</label>
            <input pInputText type="number" formControlName="annualCapDays" class="w-full" />
          </div>
        </div>
      </section>

      <!-- Section C: Carryover -->
      <section class="border-b pb-4">
        <h4 class="font-semibold">Carryover</h4>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-3">
          <div>
            <label class="block text-sm text-gray-700">Allow Carryover</label>
            <p-checkbox formControlName="allowCarryover" [binary]="true"></p-checkbox>
          </div>
          <div>
            <label class="block text-sm text-gray-700">Max Carryover Years</label>
            <input pInputText type="number" formControlName="maxCarryoverYears" class="w-full" />
          </div>
        </div>
      </section>
    </div>
  </form>

  <ng-template pTemplate="footer">
    <div class="flex items-center justify-end gap-2 px-6 py-3">
      <button pButton type="button" label="Cancel" class="p-button-text" (click)="policyDialogVisible.set(false)"></button>
      <button pButton type="button" label="Save" (click)="onSavePolicy()" [disabled]="submitLoading()"></button>
    </div>
  </ng-template>
</p-dialog>
`
})
export class LeaveSettingsTabComponent implements OnInit {
  private fb = inject(FormBuilder);
  private leaveService = inject(LeaveService);
  private contextService = inject(CompanyContextService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  leaveTypes = signal<LeaveType[]>([]);
  policies = signal<LeaveTypePolicy[]>([]);
  legalRules = signal<Record<number, any[]>>({});
  loading = signal(false);
  dialogVisible = signal(false);
  submitLoading = signal(false);
  policyDialogVisible = signal(false);

  form!: FormGroup;
  policyForm!: FormGroup;
  isEditMode = false;
  currentId: number | null = null;
  isPolicyEditMode = false;
  currentPolicyId: number | null = null;
  selectedLeaveTypeId: number | null = null;
  isGlobalEdit = false;

  // Helper pour convertir en boolean de manière sûre
  private toBooleanSafe(value: any): boolean {
    if (typeof value === 'boolean') return value;
    if (typeof value === 'string') return value.toLowerCase() === 'true';
    if (typeof value === 'number') return value === 1;
    return Boolean(value);
  }

  ngOnInit() {
    this.initForm();
    this.initPolicyForm();
    
    // Debug: vérifier les valeurs initiales
    Object.keys(this.policyForm.controls).forEach(key => {
      const control = this.policyForm.get(key);
      if (control) {
      }
    });
    
    this.load();
    this.loadPoliciesForCompany();
    this.loadLegalRules();
    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => { this.load(); this.loadPoliciesForCompany(); });
  }

  private initForm() {
    this.form = this.fb.group({
      leaveCode: ['', [Validators.required, Validators.maxLength(50)]],
      leaveName: ['', [Validators.required, Validators.maxLength(200)]],
      scope: ['Company', Validators.required],
      isPaid: [false],
      requiresBalance: [false],
      requiresEligibility6Months: [false],
      isActive: [true]
    });
  }

  private initPolicyForm() {
    this.policyForm = this.fb.group({
      leaveTypeId: [null, Validators.required],
      isEnabled: [true],
      accrualMethod: [1], // Monthly by default
      daysPerMonthAdult: [1.5],
      daysPerMonthMinor: [2.0],
      bonusDaysPerYearAfter5Years: [1.5],
      requiresEligibility6Months: [false],
      annualCapDays: [30],
      allowCarryover: [true],
      maxCarryoverYears: [2],
      minConsecutiveDays: [1],
      useWorkingCalendar: [true],
      requiresApprovalWorkflow: [false],
      requiresAttachment: [false],
      allowNegativeBalance: [false]
    });
  }

  // Helper pour s'assurer que les valeurs boolean sont strictes
  private setBooleanSafe(controlName: string, value: any) {
    const control = this.policyForm.get(controlName);
    if (control) {
      const boolValue = Boolean(value);
      control.setValue(boolValue, { emitEvent: false });
      control.markAsPristine();
      control.markAsUntouched();
    } else {
      console.error(`Control ${controlName} not found in policyForm`);
    }
  }

  // Helper pour s'assurer que les valeurs boolean sont strictes dans le formulaire principal
  private setBooleanSafeForm(controlName: string, value: any) {
    const control = this.form.get(controlName);
    if (control) {
      const boolValue = Boolean(value);
      control.setValue(boolValue, { emitEvent: false });
      control.markAsPristine();
      control.markAsUntouched();
    } else {
      console.error(`Control ${controlName} not found in main form`);
    }
  }

  // Reset policy form to default values
  private resetPolicyForm() {
    this.policyForm.patchValue({
      leaveTypeId: this.selectedLeaveTypeId || null,
      isEnabled: true,
      accrualMethod: 1, // Monthly by default
      daysPerMonthAdult: 1.5,
      daysPerMonthMinor: 2.0,
      bonusDaysPerYearAfter5Years: 1.5,
      requiresEligibility6Months: false,
      annualCapDays: 30,
      allowCarryover: true,
      maxCarryoverYears: 2,
      minConsecutiveDays: 1,
      useWorkingCalendar: true,
      requiresApprovalWorkflow: false,
      requiresAttachment: false,
      allowNegativeBalance: false
    });
    this.policyForm.markAsPristine();
    this.policyForm.markAsUntouched();
  }

  // Populate policy form with existing data
  private populatePolicyForm(policy: LeaveTypePolicy) {
    this.policyForm.patchValue({
      leaveTypeId: policy.LeaveTypeId,
      isEnabled: Boolean(policy.IsEnabled),
      accrualMethod: policy.AccrualMethod || 1,
      daysPerMonthAdult: policy.DaysPerMonthAdult || 1.5,
      daysPerMonthMinor: policy.DaysPerMonthMinor || 2.0,
      bonusDaysPerYearAfter5Years: policy.BonusDaysPerYearAfter5Years || 1.5,
      requiresEligibility6Months: Boolean(policy.RequiresEligibility6Months),
      annualCapDays: policy.AnnualCapDays || 30,
      allowCarryover: Boolean(policy.AllowCarryover),
      maxCarryoverYears: policy.MaxCarryoverYears || 2,
      minConsecutiveDays: policy.MinConsecutiveDays || 1,
      useWorkingCalendar: Boolean(policy.UseWorkingCalendar)
    });
  }

  // Check if accrual method is enabled
  isAccrualEnabled(): boolean {
    return this.policyForm.get('accrualMethod')?.value !== 0;
  }

  // Check if carryover is enabled
  isCarryoverEnabled(): boolean {
    return Boolean(this.policyForm.get('allowCarryover')?.value);
  }

  // Check if form is invalid
  isFormInvalid(): boolean {
    return this.policyForm.invalid;
  }

  // Close policy dialog
  closePolicyDialog() {
    this.policyDialogVisible.set(false);
    this.isPolicyEditMode = false;
    this.currentPolicyId = null;
  }

  // Handle policy save
  onSavePolicy() {
    if (this.policyForm.invalid) {
      this.policyForm.markAllAsTouched();
      this.messageService.add({ 
        severity: 'error', 
        summary: 'Erreur de validation', 
        detail: 'Veuillez corriger les erreurs dans le formulaire' 
      });
      return;
    }

    const validation = this.validatePolicyForm();
    if (validation.errors.length > 0) {
      this.messageService.add({ 
        severity: 'error', 
        summary: 'Erreur de validation', 
        detail: validation.errors.join('; ') 
      });
      return;
    }

    this.submitPolicy();
  }

  // Submit policy to backend
  private submitPolicy() {
    this.submitLoading.set(true);
    const v = this.policyForm.value;
    const companyId = Number(this.contextService.companyId());
    
    const dto: LeaveTypePolicyCreateDto | LeaveTypePolicyPatchDto = {
      CompanyId: companyId,
      LeaveTypeId: Number(v.leaveTypeId),
      IsEnabled: Boolean(v.isEnabled),
      AccrualMethod: v.accrualMethod,
      DaysPerMonthAdult: v.daysPerMonthAdult,
      DaysPerMonthMinor: v.daysPerMonthMinor,
      BonusDaysPerYearAfter5Years: v.bonusDaysPerYearAfter5Years,
      RequiresEligibility6Months: Boolean(v.requiresEligibility6Months),
      AnnualCapDays: v.annualCapDays,
      AllowCarryover: Boolean(v.allowCarryover),
      MaxCarryoverYears: v.maxCarryoverYears,
      MinConsecutiveDays: v.minConsecutiveDays,
      UseWorkingCalendar: Boolean(v.useWorkingCalendar)
    } as any;

    const request = this.isPolicyEditMode && this.currentPolicyId
      ? this.leaveService.updatePolicy(this.currentPolicyId, dto as LeaveTypePolicyPatchDto)
      : this.leaveService.createPolicy(dto as LeaveTypePolicyCreateDto);

    request.subscribe({
      next: () => {
        this.submitLoading.set(false);
        this.closePolicyDialog();
        this.loadPoliciesForCompany();
        this.messageService.add({ 
          severity: 'success', 
          summary: 'Succès', 
          detail: this.isPolicyEditMode ? 'Politique mise à jour avec succès' : 'Politique créée avec succès' 
        });
      },
      error: (err) => {
        this.submitLoading.set(false);
        this.handleError(err);
      }
    });
  }

  load() {
    this.loading.set(true);
    this.leaveService.getAll().subscribe({
      next: (res) => {
        this.leaveTypes.set(res || []);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading leave types', err);
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Could not load leave types' });
      }
    });
  }

  loadPoliciesForCompany() {
    const companyId = this.contextService.companyId();
    this.loading.set(true);
    this.leaveService.getPoliciesByCompany(companyId ? Number(companyId) : undefined).subscribe({
      error: (err) => { console.error('Error loading policies', err); this.loading.set(false); this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Could not load policies' }); }
    });
  }

  loadLegalRules() {
    this.leaveService.getLeaveTypeLegalRules().subscribe({
      next: (res) => {
        const map: Record<number, any[]> = {};
        (res || []).forEach((r: any) => {
          const ltId = r.leaveTypeId ?? r.LeaveTypeId ?? r.LeaveTypeID ?? r.LeaveType ?? null;
          if (ltId == null) return;
          const key = Number(ltId);
          map[key] = map[key] || [];
          map[key].push(r);
        });
        this.legalRules.set(map);
      },
      error: (err) => { console.error('Error loading legal rules', err); this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Could not load leave-type legal rules' }); }
    });
  }

  loadPoliciesByLeaveType(leaveTypeId: number) {
    this.loading.set(true);
    this.leaveService.getPoliciesByLeaveType(leaveTypeId).subscribe({
      next: (res) => { this.policies.set(res || []); this.loading.set(false); },
      error: (err) => { console.error('Error loading policies by leave type', err); this.loading.set(false); this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Could not load policies for this leave type' }); }
    });
  }

  openCreate() {
    this.isEditMode = false;
    this.currentId = null;
    this.form.reset({ scope: 'Company', isActive: true });
    this.dialogVisible.set(true);
  }

  openCreatePolicy() {
    this.isPolicyEditMode = false;
    this.currentPolicyId = null;
    this.resetPolicyForm();
    this.policyDialogVisible.set(true);
  }

  openEditPolicy(policy: LeaveTypePolicy) {
    this.isPolicyEditMode = true;
    this.currentPolicyId = policy.Id;
    this.populatePolicyForm(policy);
    this.policyDialogVisible.set(true);
  }

  validatePolicyForm(): { errors: string[]; warnings: string[] } {
    const v = this.policyForm.value;
    const errors: string[] = [];
    const warnings: string[] = [];

    // LeaveTypeId is required
    if (!v.leaveTypeId) {
      errors.push('Please select a leave type');
    }

    // If enabled + Monthly accrual but zero rate
    if (v.isEnabled && v.accrualMethod === 1 && (!v.daysPerMonthAdult || v.daysPerMonthAdult <= 0)) {
      warnings.push('Policy is enabled but monthly accrual rate is 0 or missing');
    }

    // Carryover validation
    if (v.allowCarryover && (!v.maxCarryoverYears || v.maxCarryoverYears <= 0)) {
      warnings.push('Carryover enabled but Max Carryover Years is 0 or missing');
    }

    // Min consecutive days validation
    if (v.minConsecutiveDays && v.minConsecutiveDays < 1) {
      warnings.push('Minimum consecutive days should be at least 1');
    }

    // Annual cap validation
    if (v.annualCapDays && v.annualCapDays <= 0) {
      warnings.push('Annual cap days should be greater than 0');
    }

    return { errors, warnings };
  }

  togglePolicyEnabled(policy: LeaveTypePolicy) {
    const dto: LeaveTypePolicyPatchDto = { IsEnabled: !policy.IsEnabled };
    this.leaveService.updatePolicy(policy.Id, dto).subscribe({ next: (res) => { this.loadPoliciesForCompany(); this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Policy updated' }); }, error: (err) => this.handleError(err) });
  }

  confirmDeletePolicy(policy: LeaveTypePolicy) {
    this.confirmationService.confirm({ message: this.translate.instant('Are you sure you want to delete this policy?'), header: this.translate.instant('Confirmation'), icon: 'pi pi-exclamation-triangle', accept: () => this.deletePolicy(policy.Id) });
  }

  private deletePolicy(id: number) {
    this.leaveService.deletePolicy(id).subscribe({ next: () => { this.loadPoliciesForCompany(); this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Policy deleted' }); }, error: (err) => this.handleError(err) });
  }

  // Helpers for template display
  getLeaveTypeDisplayName(leaveTypeId: number): string {
    const lt = this.leaveTypes().find(x => x.Id === leaveTypeId) as any;
    if (!lt) return String(leaveTypeId);
    return `${this.getLeaveTypeLabelFromObject(lt)} (${lt.LeaveCode ?? lt.leaveCode ?? ''})`;
  }

  getLeaveTypeLabelFromObject(lt: any): string {
    if (!lt) return '';
    return (lt.LeaveName || lt.leaveName || lt.LeaveCode || lt.leaveCode || '').toString();
  }

  getLeaveTypeId(lt: any): number | null {
    if (!lt) return null;
    return (lt.Id ?? lt.id) ?? null;
  }

  getRulesForLeaveType(leaveTypeId: number | null): any[] {
    if (!leaveTypeId) return [];
    return this.legalRules()[leaveTypeId] || [];
  }

  getRuleLabel(rule: any): string {
    if (!rule) return '';
    return rule.ruleName ?? rule.RuleName ?? rule.leaveTypeRuleName ?? rule.LeaveTypeRuleName ?? rule.ruleCode ?? rule.RuleCode ?? rule.EventCaseCode ?? rule.EventCaseCode ?? '';
  }

  getPolicyCompanyDisplay(policy: LeaveTypePolicy): string {
    if (policy.CompanyId == null) return 'Global';
    // try to find company name from the leave type if available
    const lt = this.leaveTypes().find(x => x.Id === policy.LeaveTypeId);
    if (lt && lt.CompanyId === policy.CompanyId && lt.CompanyName) return lt.CompanyName;
    return `Company ${policy.CompanyId}`;
  }

  // Return policy for a given leave type if present
  getPolicyForLeaveType(leaveTypeId: number): LeaveTypePolicy | undefined {
    return this.policies().find(p => p.LeaveTypeId === leaveTypeId);
  }

  // Return policy for a given leave type (using ID from object)
  getPolicyForLeaveTypeObject(lt: any): LeaveTypePolicy | undefined {
    const id = this.getLeaveTypeId(lt);
    return id ? this.getPolicyForLeaveType(id) : undefined;
  }

  getAccrualRateLabel(policy?: LeaveTypePolicy | null): string {
    if (!policy) return '-';
    if (policy.DaysPerMonthAdult && policy.DaysPerMonthAdult > 0) return `${policy.DaysPerMonthAdult} / month`;
    if (policy.AnnualCapDays && policy.AnnualCapDays > 0) return `${policy.AnnualCapDays} / year`;
    return '-';
  }

  openConfigure(lt: any) {
    const id = this.getLeaveTypeId(lt);
    const companyPolicy = this.getCompanyPolicyForLeaveType(id);
    this.isGlobalEdit = false;
    if (companyPolicy) this.openEditPolicy(companyPolicy);
    else {
      this.selectedLeaveTypeId = id;
      this.openCreatePolicy();
    }
  }

  // Return all policies applicable to a leave type (company + global)
  getPoliciesForLeaveType(id: number | null): LeaveTypePolicy[] {
    if (!id) return [];
    return (this.policies() || []).filter(p => p.LeaveTypeId === id || p.CompanyId == null);
  }

  getCompanyPolicyForLeaveType(id: number | null): LeaveTypePolicy | undefined {
    if (!id) return undefined;
    const companyId = this.contextService.companyId();
    return (this.policies() || []).find(p => p.LeaveTypeId === id && p.CompanyId === Number(companyId));
  }

  getGlobalPolicyForLeaveType(id: number | null): LeaveTypePolicy | undefined {
    if (!id) return undefined;
    return (this.policies() || []).find(p => p.LeaveTypeId === id && (p.CompanyId == null));
  }

  openEdit(item: LeaveType) {
    this.isEditMode = true;
    this.currentId = item.Id;
    this.form.patchValue({
      leaveCode: item.LeaveCode,
      leaveName: item.LeaveName,
      scope: item.Scope === LeaveScope.Company ? 'Company' : 'Global'
    });
    
    // Set boolean values safely
    this.setBooleanSafeForm('isActive', item.IsActive);
    
    this.dialogVisible.set(true);
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const companyId = this.contextService.companyId();
    if (!companyId) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Company ID not found' });
      return;
    }

    this.submitLoading.set(true);
    const v = this.form.value;
    const dto: LeaveTypeCreateDto | LeaveTypePatchDto = {
      LeaveCode: v.leaveCode,
      LeaveName: v.leaveName,
      LeaveDescription: v.leaveDescription,
      IsActive: v.isActive
    } as any;

    const req = this.isEditMode && this.currentId
      ? this.leaveService.update(this.currentId, dto as LeaveTypePatchDto)
      : this.leaveService.create({ LeaveCode: v.leaveCode, LeaveName: v.leaveName, LeaveDescription: v.leaveDescription, Scope: LeaveScope.Company, IsActive: v.isActive } as LeaveTypeCreateDto);

    req.subscribe({
      next: () => {
        this.submitLoading.set(false);
        this.dialogVisible.set(false);
        this.load();
        this.messageService.add({ severity: 'success', summary: 'Success', detail: this.isEditMode ? 'Leave type updated' : 'Leave type created' });
      },
      error: (err: HttpErrorResponse) => {
        this.submitLoading.set(false);
        this.handleError(err);
      }
    });
  }

  confirmDelete(item: LeaveType) {
    this.confirmationService.confirm({
      message: this.translate.instant('Are you sure you want to delete this leave type?'),
      header: this.translate.instant('Confirmation'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.delete(item.Id)
    });
  }

  private delete(id: number) {
    this.leaveService.delete(id).subscribe({
      next: () => {
        this.load();
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Leave type deleted' });
      },
      error: (err: HttpErrorResponse) => this.handleError(err)
    });
  }

  private handleError(err: HttpErrorResponse) {
    let detail = 'An error occurred';
    if (err.status === 409) detail = 'Code already exists';
    else if (err.status === 400) detail = err.error?.title || err.error?.message || 'Invalid request';
    else if (err.status === 403) detail = 'Access denied';
    else if (err.status === 404) detail = 'Resource not found';
    this.messageService.add({ severity: 'error', summary: 'Error', detail });
  }

  // Apply default template (stub) — will populate form with sensible defaults or create defaults
  applyTemplate() {
    const defaults = {
      isEnabled: true,
      accrualMethod: 1, // Monthly
      daysPerMonthAdult: 1.5,
      daysPerMonthMinor: 2.0,
      bonusDaysPerYearAfter5Years: 1.5,
      annualCapDays: 30,
      allowCarryover: true,
      maxCarryoverYears: 2,
      minConsecutiveDays: 1,
      useWorkingCalendar: true
    };
    this.policyForm.patchValue(defaults);
    this.messageService.add({ severity: 'info', summary: 'Template applied', detail: 'Morocco template applied to the policy form (preview). Save to persist.' });
  }
}
