import { Component, OnInit, OnDestroy, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

// Angular Material imports
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';

// PrimeNG imports
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { MessageService, ConfirmationService } from 'primeng/api';

// Translation
import { TranslateModule, TranslateService } from '@ngx-translate/core';

// Services and models
import { LeaveRequestService } from '../../../core/services/leave-request.service';
import { LeaveService } from '../../../core/services/leave.service';
import { CompanyContextService } from '../../../core/services/companyContext.service';
import { EmployeeService } from '../../../core/services/employee.service';
import { LeaveRequest, LeaveRequestCreateDto, LeaveRequestCreateForEmployeeDto, LeaveRequestStatus, LeaveType, ApprovalDto } from '../../../core/models/leave.model';
import { Employee } from '../../../core/models';

@Component({
  selector: 'app-hr-leave-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ButtonModule,
    CardModule,
    TableModule,
    DialogModule,
    SelectModule,
    DatePickerModule,
    InputTextModule,
    TextareaModule,
    TagModule,
    ConfirmDialogModule,
    ToastModule,
    TooltipModule,
    IconFieldModule,
    InputIconModule,
    TranslateModule,
    MatFormFieldModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatInputModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './hr-leave-management.component.html',
  styleUrls: ['./hr-leave-management.component.css']
})
export class HrLeaveManagementComponent implements OnInit, OnDestroy {
  // Signals for reactive state management
  allLeaveRequests = signal<LeaveRequest[]>([]);
  filteredRequests = signal<LeaveRequest[]>([]);
  employees = signal<Employee[]>([]);
  availableLeaveTypes = signal<LeaveType[]>([]);
  loading = signal<boolean>(false);

  // Dialog states
  showApprovalDialog = signal<boolean>(false);
  showGrantLeaveDialog = signal<boolean>(false);
  selectedRequest = signal<LeaveRequest | null>(null);

  // Forms
  approvalForm!: FormGroup;
  grantLeaveForm!: FormGroup;

  // Filters
  selectedEmployee = signal<number | null>(null);
  selectedStatus = signal<LeaveRequestStatus | null>(null);
  selectedLeaveType = signal<number | null>(null);
  employeeTableSearch = signal<string>('');
  requestSearch = signal<string>('');

  filteredEmployeesForTable = computed(() => {
    const list = this.employees();
    const q = this.employeeTableSearch().toLowerCase().trim();
    if (!q) return list;
    return list.filter(emp => {
      const firstName = (emp.firstName || '').toLowerCase();
      const lastName = (emp.lastName || '').toLowerCase();
      const full = `${firstName} ${lastName}`.trim();
      return full.includes(q) || firstName.includes(q) || lastName.includes(q);
    });
  });

  selectedEmployeeForGrant = computed(() => {
    const employeeId = this.grantLeaveForm?.value?.employeeId;
    if (!employeeId) return null;
    return this.employees().find(emp => emp.id === employeeId || (emp as any).Id === employeeId) || null;
  });

  // Statistics
  stats = signal({
    pending: 0,
    approved: 0,
    rejected: 0,
    cancelled: 0,
    total: 0
  });

  // Status options for filtering
  statusOptions = [
    { label: 'Tous', value: null },
    { label: 'En attente', value: LeaveRequestStatus.Submitted },
    { label: 'Approuvées', value: LeaveRequestStatus.Approved },
    { label: 'Rejetées', value: LeaveRequestStatus.Rejected },
    { label: 'Brouillons', value: LeaveRequestStatus.Draft },
    { label: 'Annulées', value: LeaveRequestStatus.Cancelled }
  ];

  // Tab management
  activeTab = signal<string>('pending');
  activeTabValue: string = 'pending';

  // Subject for subscription management
  private destroy$ = new Subject<void>();

  constructor(
    private leaveRequestService: LeaveRequestService,
    private leaveService: LeaveService,
    private employeeService: EmployeeService,
    private companyContextService: CompanyContextService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private fb: FormBuilder,
    private translate: TranslateService,
    private router: Router
  ) {
    this.initializeForms();

    // Reactive filtering - trigger when filters change
    effect(() => {

      // Only trigger filtering when relevant data changes
      if (this.allLeaveRequests().length > 0) {
        this.applyFilters();
      } else {
      }
    });
  }

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForms(): void {
    this.approvalForm = this.fb.group({
      approverNotes: ['', [Validators.maxLength(500)]]
    });

    this.grantLeaveForm = this.fb.group({
      employeeId: [null, Validators.required],
      leaveTypeId: [null, Validators.required],
      startDate: [null, Validators.required],
      endDate: [null, Validators.required],
      reason: ['', [Validators.required, Validators.maxLength(500)]]
    });
  }

  private loadData(): void {
    this.loadAllLeaveRequests();
    this.loadEmployees();
    this.loadAvailableLeaveTypes();
  }

  private loadAllLeaveRequests(): void {
    const companyId = this.companyContextService.companyId();
    if (!companyId) return;

    this.loading.set(true);
    this.leaveRequestService.getAll(parseInt(companyId))
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (requests) => {
          if (requests && requests.length > 0) {
          }

          this.allLeaveRequests.set(requests);
          this.updateStatistics(requests);
          this.applyFilters();
          this.loading.set(false);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: 'Impossible de charger les demandes de congé'
          });
          this.loading.set(false);
        }
      });
  }

  private loadEmployees(): void {
    const companyId = this.companyContextService.companyId();
    if (!companyId) return;

    this.employeeService.getEmployees({ companyId: parseInt(companyId) })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.employees.set(
            (response.employees || []).filter(
              (e: any) => e.status === 'active' || e.status === 'on_leave'
            )
          );
        },
        error: (error: any) => {
          alert('Error loading employees:');
        }
      });
  }

  private loadAvailableLeaveTypes(): void {
    const companyId = this.companyContextService.companyId();
    if (!companyId) return;

    this.leaveService.getAll(parseInt(companyId))
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (leaveTypes) => {
          this.availableLeaveTypes.set(leaveTypes);
        },
        error: (error) => {
          alert('Error loading leave types:');
        }
      });
  }

  /** Navigate to employee profile page */
  viewEmployee(employeeId: any): void {
    const id = employeeId?.id ?? employeeId;
    if (!id) return;
    const prefix = this.companyContextService.isExpertMode() ? '/expert' : '/app';
    this.router.navigate([`${prefix}/employees`, id]);
  }

  private updateStatistics(requests: LeaveRequest[]): void {

    const stats = {
      pending: requests.filter(r => r.status === LeaveRequestStatus.Submitted).length,
      approved: requests.filter(r => r.status === LeaveRequestStatus.Approved).length,
      rejected: requests.filter(r => r.status === LeaveRequestStatus.Rejected).length,
      cancelled: requests.filter(r => r.status === LeaveRequestStatus.Cancelled).length,
      total: requests.length
    };

    this.stats.set(stats);
  }

  private applyFilters(): void {
    let filtered = [...this.allLeaveRequests()];
    const activeTab = this.activeTab();

    // Filter by active tab (primary filter)
    if (activeTab === 'pending') {
      filtered = filtered.filter(r => r.status === LeaveRequestStatus.Submitted);
    } else if (activeTab === 'approved') {
      filtered = filtered.filter(r => r.status === LeaveRequestStatus.Approved);
    } else if (activeTab === 'rejected') {
      filtered = filtered.filter(r => r.status === LeaveRequestStatus.Rejected);
    } else if (activeTab === 'cancelled') {
      filtered = filtered.filter(r => r.status === LeaveRequestStatus.Cancelled);
    }
    // For 'all' tab, apply status filter if any
    else if (activeTab === 'all') {
      const selectedStatus = this.selectedStatus();
      if (selectedStatus !== null) {
        filtered = filtered.filter(r => r.status === selectedStatus);
      }
    }

    // Apply additional filters (only non-status filters when not on 'all' tab)
    const selectedEmployee = this.selectedEmployee();
    if (selectedEmployee) {
      filtered = filtered.filter(r => r.employeeId.toString() === selectedEmployee.toString());
    }

    const selectedLeaveType = this.selectedLeaveType();
    if (selectedLeaveType) {
      filtered = filtered.filter(r => r.leaveTypeId === selectedLeaveType);
    }

    const query = this.requestSearch().toLowerCase().trim();
    if (query) {
      filtered = filtered.filter(r =>
        this.getEmployeeName(r.employeeId).toLowerCase().includes(query)
      );
    }

    this.filteredRequests.set(filtered);
  }

  onTabChange(event: any): void {

    this.setActiveTab(event.value);

  }

  onActiveItemChange(event: any): void {
    this.setActiveTab(event);
  }

  onActiveTabChange(event: any): void {
    this.setActiveTab(event);
  }

  setActiveTab(tabValue: string): void {

    this.activeTab.set(tabValue);
    this.activeTabValue = tabValue;


    // Clear filters but preserve status filter for 'all' tab
    this.selectedEmployee.set(null);
    this.selectedLeaveType.set(null);

    // Only clear status filter when not switching to 'all' tab
    if (tabValue !== 'all') {
      this.selectedStatus.set(null);
    } else {
    }

    // Manually trigger filtering
    this.applyFilters();
  }

  onEmployeeFilterChange(employeeId: number | null): void {
    this.selectedEmployee.set(employeeId);
  }

  onStatusFilterChange(status: LeaveRequestStatus | null): void {
    this.selectedStatus.set(status);
  }

  onLeaveTypeFilterChange(leaveTypeId: number | null): void {
    this.selectedLeaveType.set(leaveTypeId);
  }

  clearFilters(): void {
    this.selectedEmployee.set(null);
    this.selectedStatus.set(null);
    this.selectedLeaveType.set(null);
    // No need to call applyFilters() manually as the effect will handle it
  }

  // Approval actions
  approveRequest(request: LeaveRequest): void {
    this.selectedRequest.set(request);
    this.approvalForm.reset();
    this.showApprovalDialog.set(true);
  }

  rejectRequest(request: LeaveRequest): void {
    this.selectedRequest.set(request);
    this.approvalForm.reset();
    this.showApprovalDialog.set(true);
  }

  confirmApproval(): void {
    const request = this.selectedRequest();
    if (!request) return;

    const approvalDto: ApprovalDto = {
      comment: this.approvalForm.value.approverNotes || ''
    };

    this.leaveRequestService.approve(request.id, approvalDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Succès',
            detail: 'Demande de congé approuvée avec succès'
          });
          this.loadAllLeaveRequests();
          this.hideApprovalDialog();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: 'Impossible d\'approuver la demande de congé'
          });
        }
      });
  }

  confirmRejection(): void {
    const request = this.selectedRequest();
    if (!request) return;

    const approvalDto: ApprovalDto = {
      comment: this.approvalForm.value.approverNotes || ''
    };

    this.leaveRequestService.reject(request.id, approvalDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Succès',
            detail: 'Demande de congé rejetée avec succès'
          });
          this.loadAllLeaveRequests();
          this.hideApprovalDialog();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: 'Impossible de rejeter la demande de congé'
          });
        }
      });
  }

  hideApprovalDialog(): void {
    this.showApprovalDialog.set(false);
    this.selectedRequest.set(null);
  }

  // Grant leave functionality
  showGrantLeaveForm(): void {
    this.grantLeaveForm.reset({
      employeeId: null,
      leaveTypeId: null,
      startDate: null,
      endDate: null,
      reason: ''
    });
    this.grantLeaveForm.markAsUntouched();
    this.grantLeaveForm.markAsPristine();
    this.showGrantLeaveDialog.set(true);
  }

  /** Open grant leave dialog with the given employee pre-selected (from employees table). */
  openGrantLeaveForEmployee(emp: Employee): void {
    const id = emp?.id ?? (emp as any)?.Id;
    if (id != null) {
      this.grantLeaveForm.reset({
        employeeId: id,
        leaveTypeId: null,
        startDate: null,
        endDate: null,
        reason: ''
      });
      this.grantLeaveForm.markAsUntouched();
      this.grantLeaveForm.markAsPristine();
      this.showGrantLeaveDialog.set(true);
    }
  }

  hideGrantLeaveForm(): void {
    this.showGrantLeaveDialog.set(false);
    this.grantLeaveForm.markAsUntouched();
    this.grantLeaveForm.markAsPristine();
  }

  grantLeave(): void {
    if (!this.grantLeaveForm.valid) return;

    const formValue = this.grantLeaveForm.value;
    const employeeId = parseInt(formValue.employeeId);

    const createDto: LeaveRequestCreateForEmployeeDto = {
      leaveTypeId: formValue.leaveTypeId,
      startDate: this.formatDateForAPI(formValue.startDate),
      endDate: this.formatDateForAPI(formValue.endDate),
      employeeNote: formValue.reason
    };

    this.leaveRequestService.createForEmployee(employeeId, createDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.autoApproveLastCreatedRequest(employeeId, formValue);
        },
        error: (error) => {
          const errorMessage = error.error?.Message || error.message || 'Impossible de créer la demande de congé';
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: errorMessage
          });
        }
      });
  }

  private autoApproveLastCreatedRequest(employeeId: number, formValue: any): void {
    // Load fresh data to get the newly created request
    const companyId = this.companyContextService.companyId();
    if (!companyId) return;

    this.leaveRequestService.getAll(parseInt(companyId))
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (requests) => {
          // Find the most recently created request for this employee that matches our criteria
          const targetStartDate = this.formatDateForAPI(formValue.startDate);
          const targetEndDate = this.formatDateForAPI(formValue.endDate);

          const newRequest = requests
            .filter(r =>
              r.employeeId === employeeId &&
              r.status === LeaveRequestStatus.Submitted &&
              r.leaveTypeId === formValue.leaveTypeId &&
              this.formatDateForAPI(new Date(r.startDate)) === targetStartDate &&
              this.formatDateForAPI(new Date(r.endDate)) === targetEndDate
            )
            .sort((a, b) => new Date(b.createdAt || '').getTime() - new Date(a.createdAt || '').getTime())[0];

          if (newRequest) {
            // Auto-approve the request
            const approvalDto: ApprovalDto = {
              comment: 'Approuvé automatiquement lors de la création par RH'
            };

            this.leaveRequestService.approve(newRequest.id, approvalDto)
              .pipe(takeUntil(this.destroy$))
              .subscribe({
                next: () => {
                  this.messageService.add({
                    severity: 'success',
                    summary: 'Succès',
                    detail: 'Demande de congé créée et approuvée automatiquement'
                  });
                  this.loadAllLeaveRequests();
                  this.hideGrantLeaveForm();
                },
                error: (error) => {
                  this.messageService.add({
                    severity: 'warn',
                    summary: 'Attention',
                    detail: 'Demande créée mais l\'approbation automatique a échoué'
                  });
                  this.loadAllLeaveRequests();
                  this.hideGrantLeaveForm();
                }
              });
          } else {
            // Fallback if we can't find the request
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Demande de congé créée avec succès'
            });
            this.loadAllLeaveRequests();
            this.hideGrantLeaveForm();
          }
        },
        error: (error) => {
          alert('Error loading requests for auto-approval:');
          this.messageService.add({
            severity: 'warn',
            summary: 'Attention',
            detail: 'Demande créée mais l\'approbation automatique a échoué'
          });
          this.loadAllLeaveRequests();
          this.hideGrantLeaveForm();
        }
      });
  }


  // Utility methods
  getEmployeeName(employeeIdOrEmployee: number | string | any | null): string {
    if (!employeeIdOrEmployee) return 'Employé inconnu';

    // Si on reçoit un objet employé (PrimeNG selectedItem passe l'objet entier)
    if (typeof employeeIdOrEmployee === 'object' && employeeIdOrEmployee.firstName && employeeIdOrEmployee.lastName) {
      return `${employeeIdOrEmployee.firstName} ${employeeIdOrEmployee.lastName}`;
    }

    // Si on reçoit juste un ID, chercher l'employé dans la liste
    const employee = this.employees().find(e =>
      e.id === employeeIdOrEmployee ||
      e.id === employeeIdOrEmployee.toString() ||
      e.id.toString() === employeeIdOrEmployee.toString()
    );

    return employee ? `${employee.firstName} ${employee.lastName}` : 'Employé inconnu';
  }

  getLeaveTypeName(leaveTypeId: number): string {
    const leaveType = this.availableLeaveTypes().find(lt => lt.Id === leaveTypeId);
    return leaveType?.LeaveName || 'Type inconnu';
  }

  getStatusSeverity(status: LeaveRequestStatus): "success" | "secondary" | "info" | "warn" | "danger" | "contrast" | null {
    switch (status) {
      case LeaveRequestStatus.Draft:
        return 'secondary';
      case LeaveRequestStatus.Submitted:
        return 'warn';
      case LeaveRequestStatus.Approved:
        return 'success';
      case LeaveRequestStatus.Rejected:
        return 'danger';
      case LeaveRequestStatus.Cancelled:
      case LeaveRequestStatus.Renounced:
        return 'info';
      default:
        return 'secondary';
    }
  }

  getStatusLabel(status: LeaveRequestStatus): string {
    switch (status) {
      case LeaveRequestStatus.Draft:
        return 'Brouillon';
      case LeaveRequestStatus.Submitted:
        return 'En attente';
      case LeaveRequestStatus.Approved:
        return 'Approuvée';
      case LeaveRequestStatus.Rejected:
        return 'Rejetée';
      case LeaveRequestStatus.Cancelled:
        return 'Annulée';
      case LeaveRequestStatus.Renounced:
        return 'Renoncée';
      default:
        return 'Inconnu';
    }
  }

  calculateDuration(startDate: Date, endDate: Date): number {
    if (!startDate || !endDate) {
      return 0;
    }
    const start = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate());
    const end = new Date(endDate.getFullYear(), endDate.getMonth(), endDate.getDate());
    const diffTime = end.getTime() - start.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays + 1;
  }

  formatDisplayDate(date: Date): string {
    if (!date) return '';
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  }

  private formatDateForAPI(date: Date): string {
    if (!date) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  canApprove(request: LeaveRequest): boolean {
    return request.status === LeaveRequestStatus.Submitted;
  }

  canReject(request: LeaveRequest): boolean {
    return request.status === LeaveRequestStatus.Submitted;
  }

  canSubmit(request: LeaveRequest): boolean {
    return request.status === LeaveRequestStatus.Draft;
  }

  canCancel(request: LeaveRequest): boolean {
    return request.status === LeaveRequestStatus.Submitted || request.status === LeaveRequestStatus.Approved;
  }

  cancelRequest(request: LeaveRequest): void {
    this.confirmationService.confirm({
      message: 'Êtes-vous sûr de vouloir annuler cette demande de congé ?',
      header: 'Confirmation',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Oui, annuler',
      rejectLabel: 'Non',
      accept: () => {
        const approvalDto: ApprovalDto = {
          comment: 'Demande annulée par RH'
        };

        this.leaveRequestService.cancel(request.id, approvalDto)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Succès',
                detail: 'Demande de congé annulée avec succès'
              });
              this.loadAllLeaveRequests(); // Reload data
            },
            error: (error: any) => {
              this.messageService.add({
                severity: 'error',
                summary: 'Erreur',
                detail: 'Impossible d\'annuler la demande de congé'
              });
            }
          });
      }
    });
  }

    submitRequest(request: LeaveRequest): void {
      if (!this.canSubmit(request)) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Attention',
          detail: this.translate.instant('leave.messages.onlyDraftCanSubmit')
        });
        return;
      }

      this.confirmationService.confirm({
        message: this.translate.instant('leave.confirmations.submit.message'),
        header: this.translate.instant('leave.confirmations.submit.title') || 'Confirmation',
        icon: 'pi pi-exclamation-triangle',
        acceptLabel: this.translate.instant('leave.confirmations.submit.submitButton') || 'Submit',
        rejectLabel: this.translate.instant('leave.confirmations.submit.cancelButton') || 'Cancel',
        accept: () => {
          this.leaveRequestService.submit(request.id)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: () => {
                this.messageService.add({
                  severity: 'success',
                  summary: this.translate.instant('common.success') || 'Succès',
                  detail: this.translate.instant('leave.messages.submitSuccess')
                });
                this.loadAllLeaveRequests();
              },
              error: (error: any) => {
                this.messageService.add({
                  severity: 'error',
                  summary: this.translate.instant('common.error') || 'Erreur',
                  detail: this.translate.instant('leave.errors.loadFailed') || 'Impossible de soumettre la demande'
                });
              }
            });
        }
      });
    }

  getEmptyMessage(): string {
    const activeTab = this.activeTab();
    if (!activeTab) {
      return this.translate.instant('leave.hr.emptyMessages.default');
    }
    const key = activeTab === 'all' ? 'all' : (activeTab === 'pending' ? 'pending' : (activeTab === 'approved' ? 'approved' : (activeTab === 'rejected' ? 'rejected' : (activeTab === 'cancelled' ? 'cancelled' : 'default'))));
    return this.translate.instant(`leave.hr.emptyMessages.${key}`);
  }

  getEmptySubMessage(): string {
    const activeTab = this.activeTab();
    const allRequests = this.allLeaveRequests();

    if (!activeTab || !allRequests) {
      return this.translate.instant('leave.hr.emptyMessages.default');
    }

    const totalRequests = allRequests.length;
    if (totalRequests === 0) {
      return this.translate.instant('leave.hr.emptyMessages.noRequestsYet');
    }
    if (activeTab === 'pending' || activeTab === 'approved' || activeTab === 'rejected' || activeTab === 'cancelled') {
      const statusKey = activeTab === 'pending' ? 'leave.status.pending' : (activeTab === 'approved' ? 'leave.status.approved' : (activeTab === 'rejected' ? 'leave.status.rejected' : 'leave.status.cancelled'));
      const status = this.translate.instant(statusKey);
      return this.translate.instant('leave.hr.emptyMessages.totalButNone', { count: totalRequests, status });
    }
    return this.translate.instant('leave.hr.emptyMessages.tryFilters');
  }
}
