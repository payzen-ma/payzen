import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
import { CardModule } from 'primeng/card';
import { ConfirmationService, MessageService } from 'primeng/api';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { OvertimeService } from '@app/core/services/overtime.service';
import { 
  Overtime, 
  OvertimeStatus,
  OvertimeType,
  OvertimeFilters 
} from '@app/core/models/overtime.model';

@Component({
  selector: 'app-overtime-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
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
    CardModule,
    TranslateModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './overtime-management.html',
  styleUrls: ['./overtime-management.css']
})
export class OvertimeManagementComponent implements OnInit {
  private readonly overtimeService = inject(OvertimeService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  // Signals
  readonly overtimes = signal<Overtime[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly showApprovalDialog = signal<boolean>(false);
  readonly selectedOvertime = signal<Overtime | null>(null);
  
  // Filters
  selectedStatus = signal<OvertimeStatus | null>(null);
  selectedStartDate = signal<Date | null>(null);
  selectedEndDate = signal<Date | null>(null);

  // Approval
  approvalComment = '';
  
  // Status enum for template
  readonly OvertimeStatus = OvertimeStatus;
  readonly OvertimeType = OvertimeType;

  // Computed
  readonly statusOptions = computed(() => [
    { label: this.translate.instant('overtime.status.all'), value: null },
    { label: this.translate.instant('overtime.status.submitted'), value: OvertimeStatus.Submitted },
    { label: this.translate.instant('overtime.status.approved'), value: OvertimeStatus.Approved },
    { label: this.translate.instant('overtime.status.rejected'), value: OvertimeStatus.Rejected },
    { label: this.translate.instant('overtime.status.cancelled'), value: OvertimeStatus.Cancelled }
  ]);

  readonly pendingCount = computed(() => 
    this.overtimes().filter(o => o.status === OvertimeStatus.Submitted).length
  );

  ngOnInit(): void {
    this.loadOvertimes();
  }

  /**
   * Load all overtimes for the company
   */
  loadOvertimes(): void {
    this.isLoading.set(true);

    const filters: OvertimeFilters = {
      pageSize: 100
    };

    if (this.selectedStatus() !== null) {
      filters.status = this.selectedStatus()!;
    }
    if (this.selectedStartDate()) {
      filters.startDate = this.formatDate(this.selectedStartDate()!);
    }
    if (this.selectedEndDate()) {
      filters.endDate = this.formatDate(this.selectedEndDate()!);
    }

    this.overtimeService.getOvertimes(filters).subscribe({
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
   * Filter overtimes
   */
  applyFilters(): void {
    this.loadOvertimes();
  }

  /**
   * Reset filters
   */
  resetFilters(): void {
    this.selectedStatus.set(null);
    this.selectedStartDate.set(null);
    this.selectedEndDate.set(null);
    this.loadOvertimes();
  }

  /**
   * Open approval dialog
   */
  openApprovalDialog(overtime: Overtime): void {
    this.selectedOvertime.set(overtime);
    this.approvalComment = '';
    this.showApprovalDialog.set(true);
  }

  /**
   * Close approval dialog
   */
  closeApprovalDialog(): void {
    this.showApprovalDialog.set(false);
    this.selectedOvertime.set(null);
    this.approvalComment = '';
  }

  /**
   * Approve overtime
   */
  approveOvertime(): void {
    const overtime = this.selectedOvertime();
    if (!overtime?.id) return;

    this.overtimeService.approveOvertime(overtime.id, this.approvalComment).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('overtime.messages.approved')
        });
        this.closeApprovalDialog();
        this.loadOvertimes();
      },
      error: (error) => {
        console.error('Error approving overtime:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('overtime.errors.approveFailed')
        });
      }
    });
  }

  /**
   * Reject overtime
   */
  rejectOvertime(): void {
    const overtime = this.selectedOvertime();
    if (!overtime?.id) return;

    if (!this.approvalComment.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: this.translate.instant('common.warning'),
        detail: this.translate.instant('overtime.validation.rejectCommentRequired')
      });
      return;
    }

    this.overtimeService.rejectOvertime(overtime.id, this.approvalComment).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('overtime.messages.rejected')
        });
        this.closeApprovalDialog();
        this.loadOvertimes();
      },
      error: (error) => {
        console.error('Error rejecting overtime:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('overtime.errors.rejectFailed')
        });
      }
    });
  }

  /**
   * Get status severity for PrimeNG Tag
   */
  getStatusSeverity(status: OvertimeStatus): 'success' | 'warn' | 'danger' | 'info' | 'secondary' | 'contrast' | undefined {
    switch (status) {
      case OvertimeStatus.Submitted:
        return 'info';
      case OvertimeStatus.Approved:
        return 'success';
      case OvertimeStatus.Rejected:
        return 'danger';
      case OvertimeStatus.Cancelled:
        return 'secondary';
      default:
        return undefined;
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
   * Check if can approve/reject
   */
  canApprove(overtime: Overtime): boolean {
    return overtime.status === OvertimeStatus.Submitted;
  }

  /**
   * Get employee initials for avatar
   */
  getEmployeeInitials(name: string): string {
    if (!name || name === 'N/A') return 'NA';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  /**
   * Format date to YYYY-MM-DD
   */
  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
