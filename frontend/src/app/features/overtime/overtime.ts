import { Component, signal, computed, inject, OnInit, effect, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { OvertimeService } from '@app/core/services/overtime.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { AuthService } from '@app/core/services/auth.service';
import { 
  Overtime, 
  OvertimeStatus,
  OvertimeType,
  CreateOvertimeRequest, 
  UpdateOvertimeRequest,
  OvertimeFilters 
} from '@app/core/models/overtime.model';

@Component({
  selector: 'app-overtime',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    DatePickerModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
    TooltipModule,
    TranslateModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './overtime.html',
  styleUrls: ['./overtime.css']
})
export class OvertimeComponent implements OnInit {
  private readonly overtimeService = inject(OvertimeService);
  private readonly employeeService = inject(EmployeeService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);

  // Signals
  readonly overtimes = signal<Overtime[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly showDialog = signal<boolean>(false);
  readonly isEditMode = signal<boolean>(false);
  readonly selectedOvertime = signal<Overtime | null>(null);
  readonly currentUserId = signal<number | null>(null);
  readonly selectedOvertimeType = signal<OvertimeType>(OvertimeType.Hourly);

  // Expose enum for template
  readonly OvertimeType = OvertimeType;
  readonly OvertimeStatus = OvertimeStatus;

  // Form
  overtimeForm!: FormGroup;

  // Type options
  readonly typeOptions = computed(() => [
    { label: this.translate.instant('overtime.type.hourly'), value: OvertimeType.Hourly },
    { label: this.translate.instant('overtime.type.holiday'), value: OvertimeType.Holiday }
  ]);

  // Status options
  readonly statusOptions = computed(() => [
    { label: this.translate.instant('overtime.status.submitted'), value: OvertimeStatus.Submitted },
    { label: this.translate.instant('overtime.status.approved'), value: OvertimeStatus.Approved },
    { label: this.translate.instant('overtime.status.rejected'), value: OvertimeStatus.Rejected },
    { label: this.translate.instant('overtime.status.cancelled'), value: OvertimeStatus.Cancelled }
  ]);

  constructor() {
    // Load current user info using effect
    effect(() => {
      const user = this.authService.currentUser();
      if (user?.employee_id) {
        this.currentUserId.set(Number(user.employee_id));
      }
    });
  }

  ngOnInit(): void {
    this.initForm();
    this.loadOvertimes();
  }

  /**
   * Initialize the form
   */
  private initForm(): void {
    this.overtimeForm = this.fb.group({
      overtimeDate: [null, Validators.required],
      overtimeType: [OvertimeType.Hourly, Validators.required],
      startTime: [''],
      endTime: [''],
      reason: ['']
    });

    // Watch overtimeType changes to update validators
    this.overtimeForm.get('overtimeType')?.valueChanges.subscribe(type => {
      this.selectedOvertimeType.set(type);
      const startTimeControl = this.overtimeForm.get('startTime');
      const endTimeControl = this.overtimeForm.get('endTime');
      
      if (type === OvertimeType.Hourly) {
        startTimeControl?.setValidators([Validators.required]);
        endTimeControl?.setValidators([Validators.required]);
      } else {
        startTimeControl?.clearValidators();
        endTimeControl?.clearValidators();
      }
      
      startTimeControl?.updateValueAndValidity();
      endTimeControl?.updateValueAndValidity();
    });
  }

  /**
   * Load all overtimes for current employee
   */
  loadOvertimes(): void {
    const employeeId = this.currentUserId();
    if (!employeeId) return;

    this.isLoading.set(true);

    const filters: OvertimeFilters = {
      employeeId: employeeId,
      pageSize: 100
    };

    this.overtimeService.getOvertimes(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.overtimes.set(response.data);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading overtimes:', error);
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: this.translate.instant('overtime.errors.loadFailed')
          });
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Open dialog to create new overtime
   */
  openCreateDialog(): void {
    this.isEditMode.set(false);
    this.selectedOvertime.set(null);
    this.overtimeForm.reset({
      overtimeType: OvertimeType.Hourly,
      startTime: '',
      endTime: '',
      reason: ''
    });
    this.selectedOvertimeType.set(OvertimeType.Hourly);
    this.showDialog.set(true);
  }

  /**
   * Open dialog to edit overtime
   */
  openEditDialog(overtime: Overtime): void {
    this.isEditMode.set(true);
    this.selectedOvertime.set(overtime);
    
    // Parse date and time
    const overtimeDate = new Date(overtime.overtimeDate);
    
    this.overtimeForm.patchValue({
      overtimeDate: overtimeDate,
      overtimeType: overtime.overtimeType,
      startTime: overtime.startTime || '',
      endTime: overtime.endTime || '',
      reason: overtime.reason || ''
    });
    
    this.selectedOvertimeType.set(overtime.overtimeType);
    this.showDialog.set(true);
  }

  /**
   * Close dialog
   */
  closeDialog(): void {
    this.showDialog.set(false);
    this.overtimeForm.reset();
    this.selectedOvertime.set(null);
  }

  /**
   * Save overtime (create or update)
   */
  saveOvertime(): void {
    if (this.overtimeForm.invalid) {
      this.overtimeForm.markAllAsTouched();
      return;
    }

    const formValue = this.overtimeForm.value;
    const employeeId = this.currentUserId();
    
    if (!employeeId) {
      this.messageService.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('overtime.errors.noEmployee')
      });
      return;
    }

    // Format date to YYYY-MM-DD
    const overtimeDate = new Date(formValue.overtimeDate);
    const formattedDate = overtimeDate.toISOString().split('T')[0];

    if (this.isEditMode()) {
      this.updateOvertime(formattedDate, formValue);
    } else {
      this.createOvertime(employeeId, formattedDate, formValue);
    }
  }

  /**
   * Create new overtime
   */
  private createOvertime(employeeId: number, overtimeDate: string, formValue: any): void {
    const request: CreateOvertimeRequest = {
      employeeId: employeeId,
      overtimeDate: overtimeDate,
      overtimeType: formValue.overtimeType,
      startTime: formValue.overtimeType === OvertimeType.Hourly ? formValue.startTime : undefined,
      endTime: formValue.overtimeType === OvertimeType.Hourly ? formValue.endTime : undefined,
      reason: formValue.reason || undefined
    };

    this.overtimeService.createOvertime(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('overtime.messages.createSuccess')
          });
          this.closeDialog();
          this.loadOvertimes();
        },
        error: (error) => {
          console.error('Error creating overtime:', error);
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: this.translate.instant('overtime.errors.createFailed')
          });
        }
      });
  }

  /**
   * Update existing overtime
   */
  private updateOvertime(overtimeDate: string, formValue: any): void {
    const overtime = this.selectedOvertime();
    if (!overtime?.id) return;

    const request: UpdateOvertimeRequest = {
      overtimeDate: overtimeDate,
      overtimeType: formValue.overtimeType,
      startTime: formValue.overtimeType === OvertimeType.Hourly ? formValue.startTime : undefined,
      endTime: formValue.overtimeType === OvertimeType.Hourly ? formValue.endTime : undefined,
      reason: formValue.reason || undefined
    };

    this.overtimeService.updateOvertime(overtime.id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('overtime.messages.updateSuccess')
          });
          this.closeDialog();
          this.loadOvertimes();
        },
        error: (error) => {
          console.error('Error updating overtime:', error);
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: this.translate.instant('overtime.errors.updateFailed')
          });
        }
      });
  }

  /**
   * Delete overtime
   */
  deleteOvertime(overtime: Overtime): void {
    if (!overtime.id) return;

    this.confirmationService.confirm({
      message: this.translate.instant('overtime.confirmDelete'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: this.translate.instant('common.yes'),
      rejectLabel: this.translate.instant('common.no'),
      accept: () => {
        this.overtimeService.deleteOvertime(overtime.id!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: this.translate.instant('common.success'),
                detail: this.translate.instant('overtime.messages.deleteSuccess')
              });
              this.loadOvertimes();
            },
            error: (error) => {
              console.error('Error deleting overtime:', error);
              this.messageService.add({
                severity: 'error',
                summary: this.translate.instant('common.error'),
                detail: this.translate.instant('overtime.errors.deleteFailed')
              });
            }
          });
      }
    });
  }

  /**
   * Cancel overtime
   */
  cancelOvertime(overtime: Overtime): void {
    if (!overtime.id) return;

    this.confirmationService.confirm({
      message: this.translate.instant('overtime.confirmCancel'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: this.translate.instant('common.yes'),
      rejectLabel: this.translate.instant('common.no'),
      accept: () => {
        this.overtimeService.cancelOvertime(overtime.id!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: this.translate.instant('common.success'),
                detail: this.translate.instant('overtime.messages.cancelSuccess')
              });
              this.loadOvertimes();
            },
            error: (error) => {
              console.error('Error cancelling overtime:', error);
              this.messageService.add({
                severity: 'error',
                summary: this.translate.instant('common.error'),
                detail: this.translate.instant('overtime.errors.cancelFailed')
              });
            }
          });
      }
    });
  }

  /**
   * Get status severity for tag
   */
  getStatusSeverity(status: OvertimeStatus): 'success' | 'info' | 'warn' | 'danger' {
    switch (status) {
      case OvertimeStatus.Approved:
        return 'success';
      case OvertimeStatus.Submitted:
        return 'info';
      case OvertimeStatus.Cancelled:
        return 'warn';
      case OvertimeStatus.Rejected:
        return 'danger';
      default:
        return 'info';
    }
  }

  /**
   * Get type label
   */
  getTypeLabel(type: OvertimeType): string {
    return type === OvertimeType.Hourly 
      ? this.translate.instant('overtime.type.hourly')
      : this.translate.instant('overtime.type.holiday');
  }

  /**
   * Check if overtime can be edited
   */
  canEdit(overtime: Overtime): boolean {
    return overtime.status === OvertimeStatus.Submitted;
  }

  /**
   * Check if overtime can be cancelled
   */
  canCancel(overtime: Overtime): boolean {
    return overtime.status === OvertimeStatus.Submitted || overtime.status === OvertimeStatus.Approved;
  }
}
