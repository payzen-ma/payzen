import { Component, OnInit, OnDestroy, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';

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
import { MessageService, ConfirmationService } from 'primeng/api';

// Translation
import { TranslateModule } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';

// Services and models
import { LeaveRequestService } from '../../../core/services/leave-request.service';
import { LeaveService } from '../../../core/services/leave.service';
import { CompanyContextService } from '../../../core/services/companyContext.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaveRequest, LeaveRequestCreateDto, LeaveRequestStatus, LeaveType } from '../../../core/models/leave.model';

@Component({
  selector: 'app-leave-requests',
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
    TranslateModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './leave-requests.component.html',
  styleUrl: './leave-requests.component.css'
})
export class LeaveRequestsComponent implements OnInit, OnDestroy {
  // Signals for reactive state management
  leaveRequests = signal<LeaveRequest[]>([]);
  availableLeaveTypes = signal<LeaveType[]>([]);
  loading = signal<boolean>(false);
  
  // Dialog states
  showCreateDialog = signal<boolean>(false);
  showEditDialog = signal<boolean>(false);
  selectedRequest = signal<LeaveRequest | null>(null);
  
  // Forms
  createForm!: FormGroup;
  editForm!: FormGroup;
  
  // Current employee from auth service
  currentEmployeeId = signal<number | null>(null);
  
  // Subject for subscription management
  private destroy$ = new Subject<void>();
  
  // Status options for filtering
  statusOptions = [
    { label: 'Tous', value: null },
    { label: 'Brouillon', value: LeaveRequestStatus.Draft },
    { label: 'Soumise', value: LeaveRequestStatus.Submitted },
    { label: 'Approuvée', value: LeaveRequestStatus.Approved },
    { label: 'Rejetée', value: LeaveRequestStatus.Rejected },
    { label: 'Annulée', value: LeaveRequestStatus.Cancelled },
    { label: 'Renoncée', value: LeaveRequestStatus.Renounced }
  ];
  
  constructor(
    private leaveRequestService: LeaveRequestService,
    private leaveService: LeaveService,
    private companyContextService: CompanyContextService,
    private authService: AuthService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private fb: FormBuilder
  ) {
    this.initializeForms();
    
    // Load current user info using effect
    effect(() => {
      const user = this.authService.currentUser();
      if (user?.id) {
        this.currentEmployeeId.set(Number(user.id));
        // Reload leave requests when employee ID changes
        if (this.currentEmployeeId()) {
          this.loadLeaveRequests();
        }
      }
    });
  }
  
  ngOnInit(): void {
    this.loadAvailableLeaveTypes();
    // loadLeaveRequests will be called from effect when currentEmployeeId is set
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  private initializeForms(): void {
    this.createForm = this.fb.group({
      leaveTypeId: [null, Validators.required],
      startDate: [null, Validators.required],
      endDate: [null, Validators.required],
      reason: ['', [Validators.required, Validators.maxLength(500)]]
    });
    
    this.editForm = this.fb.group({
      leaveTypeId: [null, Validators.required],
      startDate: [null, Validators.required],
      endDate: [null, Validators.required],
      reason: ['', [Validators.required, Validators.maxLength(500)]]
    });
  }
  
  async loadLeaveRequests(): Promise<void> {
    const employeeId = this.currentEmployeeId();
    if (!employeeId) {
      return;
    }
    
    try {
      this.loading.set(true);
      this.leaveRequestService.getByEmployee(employeeId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
        next: (requests) => {
          this.leaveRequests.set(requests);
          this.loading.set(false);
        },
        error: (error) => {
          console.error('Error loading leave requests:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Erreur',
            detail: 'Impossible de charger les demandes de congé'
          });
          this.loading.set(false);
        }
      });
    } catch (error) {
      console.error('Error in loadLeaveRequests:', error);
      this.loading.set(false);
    }
  }
  
  async loadAvailableLeaveTypes(): Promise<void> {
    try {
      const companyId = this.companyContextService.companyId();
      if (companyId) {
        this.leaveService.getAll(parseInt(companyId))
          .pipe(takeUntil(this.destroy$))
          .subscribe({
          next: (leaveTypes) => {
            this.availableLeaveTypes.set(leaveTypes);
          },
          error: (error) => {
            console.error('Error loading leave types:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail: 'Impossible de charger les types de congé'
            });
          }
        });
      }
    } catch (error) {
      console.error('Error loading leave types:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'Impossible de charger les types de congé'
      });
    }
  }
  
  showCreateForm(): void {
    this.createForm.reset();
    this.showCreateDialog.set(true);
  }
  
  hideCreateForm(): void {
    this.showCreateDialog.set(false);
  }
  
  async createLeaveRequest(): Promise<void> {
    const employeeId = this.currentEmployeeId();
    if (this.createForm.valid && employeeId) {
      try {
        const formValue = this.createForm.value;
        const createDto: LeaveRequestCreateDto = {
          employeeId: employeeId,
          leaveTypeId: formValue.leaveTypeId,
          startDate: this.formatDateForAPI(formValue.startDate),
          endDate: this.formatDateForAPI(formValue.endDate),
          employeeNote: formValue.reason
        };
        
        this.leaveRequestService.create(createDto)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Demande de congé créée avec succès'
            });
            
            this.hideCreateForm();
            this.loadLeaveRequests();
          },
          error: (error) => {
            console.error('Error creating leave request:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail: 'Impossible de créer la demande de congé'
            });
          }
        });
      } catch (error) {
        console.error('Error in createLeaveRequest:', error);
      }
    } else if (!employeeId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'Impossible d\'identifier l\'employé connecté'
      });
    }
  }
  
  editRequest(request: LeaveRequest): void {
    if (request.status !== LeaveRequestStatus.Draft) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Attention',
        detail: 'Seules les demandes en brouillon peuvent être modifiées'
      });
      return;
    }
    
    this.selectedRequest.set(request);
    this.editForm.patchValue({
      leaveTypeId: request.leaveTypeId,
      startDate: request.startDate,
      endDate: request.endDate,
      reason: request.reason
    });
    this.showEditDialog.set(true);
  }
  
  hideEditForm(): void {
    this.showEditDialog.set(false);
    this.selectedRequest.set(null);
  }
  
  async updateLeaveRequest(): Promise<void> {
    const request = this.selectedRequest();
    if (this.editForm.valid && request) {
      try {
        const formValue = this.editForm.value;
        const patchDto = {
          leaveTypeId: formValue.leaveTypeId,
          startDate: this.formatDateForAPI(formValue.startDate),
          endDate: this.formatDateForAPI(formValue.endDate),
          employeeNote: formValue.reason
        };
        
        this.leaveRequestService.update(request.id, patchDto)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Demande de congé mise à jour avec succès'
            });
            
            this.hideEditForm();
            this.loadLeaveRequests();
          },
          error: (error) => {
            console.error('Error updating leave request:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail: 'Impossible de mettre à jour la demande de congé'
            });
          }
        });
        
      } catch (error) {
        console.error('Error in updateLeaveRequest:', error);
      }
    }
  }
  
  async submitRequest(request: LeaveRequest): Promise<void> {
    if (request.status !== LeaveRequestStatus.Draft) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Attention',
        detail: 'Seules les demandes en brouillon peuvent être soumises'
      });
      return;
    }
    
    this.confirmationService.confirm({
      message: 'Êtes-vous sûr de vouloir soumettre cette demande de congé ?',
      header: 'Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.leaveRequestService.submit(request.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Demande de congé soumise avec succès'
            });
            this.loadLeaveRequests();
          },
          error: (error) => {
            console.error('Error submitting leave request:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail: 'Impossible de soumettre la demande de congé'
            });
          }
        });
      }
    });
  }
  
  async deleteRequest(request: LeaveRequest): Promise<void> {
    if (request.status !== LeaveRequestStatus.Draft) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Attention',
        detail: 'Seules les demandes en brouillon peuvent être supprimées'
      });
      return;
    }
    
    this.confirmationService.confirm({
      message: 'Êtes-vous sûr de vouloir supprimer cette demande de congé ?',
      header: 'Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.leaveRequestService.delete(request.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Demande de congé supprimée avec succès'
            });
            this.loadLeaveRequests();
          },
          error: (error) => {
            console.error('Error deleting leave request:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail: 'Impossible de supprimer la demande de congé'
            });
          }
        });
      }
    });
  }

  async cancelRequest(request: LeaveRequest): Promise<void> {
    if (request.status !== LeaveRequestStatus.Submitted && request.status !== LeaveRequestStatus.Approved) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Attention',
        detail: 'Seules les demandes soumises ou approuvées peuvent être annulées'
      });
      return;
    }

    this.confirmationService.confirm({
      message: 'Êtes-vous sûr de vouloir annuler cette demande de congé ? Cette action est irréversible.',
      header: 'Confirmation d\'annulation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        const approvalDto = {
          comment: 'Demande annulée par l\'employé'
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
            this.loadLeaveRequests();
          },
          error: (error) => {
            console.error('Error canceling leave request:', error);
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
        return 'Soumise';
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
  
  canEdit(request: LeaveRequest): boolean {
    return request.status === LeaveRequestStatus.Draft;
  }
  
  canSubmit(request: LeaveRequest): boolean {
    return request.status === LeaveRequestStatus.Draft;
  }
  
  canDelete(request: LeaveRequest): boolean {
    return request.status === LeaveRequestStatus.Draft;
  }

  canCancel(request: LeaveRequest): boolean {
    // Un employé peut annuler sa demande si elle est soumise ou même approuvée (avant la prise d'effet)
    return request.status === LeaveRequestStatus.Submitted || 
           (request.status === LeaveRequestStatus.Approved && new Date(request.startDate) > new Date());
  }

  getLeaveTypeName(leaveTypeId: number): string {
    const leaveType = this.availableLeaveTypes().find(lt => lt.Id === leaveTypeId);
    return leaveType?.LeaveName || '';
  }

  private formatDateForAPI(date: Date): string {
    if (!date) return '';
    // Utiliser getFullYear, getMonth, getDate pour éviter les problèmes de fuseau horaire
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`; // YYYY-MM-DD format
  }

  formatDisplayDate(date: Date): string {
    if (!date) return '';
    // Formatage pour l'affichage en évitant les problèmes de fuseau horaire
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  }

  calculateDuration(startDate: Date, endDate: Date): number {
    if (!startDate || !endDate) {
      return 0;
    }
    // Utiliser les dates locales pour éviter les problèmes de fuseau horaire
    const start = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate());
    const end = new Date(endDate.getFullYear(), endDate.getMonth(), endDate.getDate());
    const diffTime = end.getTime() - start.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays + 1; // Include both start and end dates
  }

  // Method used in template to check if we're in edit mode
  isEditMode(): boolean {
    return this.showEditDialog() && this.selectedRequest() !== null;
  }
}