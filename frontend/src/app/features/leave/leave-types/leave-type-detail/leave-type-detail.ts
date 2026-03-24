import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { TableModule } from 'primeng/table';
import { TabsModule } from 'primeng/tabs';
import { TagComponent } from '@app/shared/components/tag/tag.component';
import { TagVariant } from '@app/shared/components/tag/tag.types';
import { EmptyState } from '@app/shared/components/empty-state/empty-state';
import { LeaveService } from '@app/core/services/leave.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { LeaveType, LeaveTypeLegalRule, LeaveTypePolicy, LeaveScope, LeaveAccrualMethod } from '@app/core/models/leave.model';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-leave-type-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TranslateModule,
    ButtonModule,
    ToastModule,
    TableModule,
    TabsModule,
    TagComponent,
    EmptyState
  ],
  providers: [MessageService],
  templateUrl: './leave-type-detail.html',
  styleUrl: './leave-type-detail.css'
})
export class LeaveTypeDetailPage implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private leaveService = inject(LeaveService);
  private contextService = inject(CompanyContextService);
  private messageService = inject(MessageService);
  private translate = inject(TranslateService);

  // State
  readonly isLoading = signal(true);
  readonly leaveType = signal<LeaveType | null>(null);
  readonly legalRules = signal<LeaveTypeLegalRule[]>([]);
  readonly policy = signal<LeaveTypePolicy | null>(null);
  readonly activeTab = signal('0');

  // Computed
  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadLeaveType(parseInt(id, 10));
    } else {
      this.goBack();
    }
  }

  private loadLeaveType(id: number): void {
    this.isLoading.set(true);

    this.leaveService.getById(id).subscribe({
      next: (leaveType: LeaveType) => {
        this.leaveType.set(leaveType);
        // Load related data
        this.loadLegalRules(id);
        this.loadPolicy(id);
        this.isLoading.set(false);
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

  private loadLegalRules(leaveTypeId: number): void {
    // For now, set empty array as the service method doesn't exist yet
    // In real implementation, this would call the backend
    this.legalRules.set([]);
    
    // TODO: Implement when backend endpoint is available
  }

  private loadPolicy(leaveTypeId: number): void {
    // For now, set null as the service method doesn't exist yet
    // In real implementation, this would call the backend
    this.policy.set(null);
    
    // TODO: Implement when backend endpoint is available
  }

  getLocalizedName(leaveType: LeaveType): string {
    // Return the leave name - in a real implementation this could be localized
    return leaveType.LeaveName || leaveType.LeaveCode;
  }

  getScopeLabel(scope: number): string {
    return scope === LeaveScope.Global 
      ? this.translate.instant('leave.types.global')
      : this.translate.instant('leave.types.company');
  }

  getScopeVariant(scope: number): TagVariant {
    return scope === LeaveScope.Global ? 'info' : 'warning';
  }

  getPaidVariant(isPaid: boolean): TagVariant {
    return isPaid ? 'success' : 'danger';
  }

  getAccrualMethodLabel(method: LeaveAccrualMethod): string {
    switch (method) {
      case LeaveAccrualMethod.Annual:
        return this.translate.instant('leave.accrual.annual');
      case LeaveAccrualMethod.Monthly:
        return this.translate.instant('leave.accrual.monthly');
      case LeaveAccrualMethod.PerPayPeriod:
        return this.translate.instant('leave.accrual.perPayPeriod');
      case LeaveAccrualMethod.HoursWorked:
        return this.translate.instant('leave.accrual.hoursWorked');
      default:
        return method.toString();
    }
  }

  editLeaveType(): void {
    const leaveType = this.leaveType();
    if (leaveType && leaveType.Scope === LeaveScope.Company) {
      this.router.navigate([`${this.routePrefix()}/leave/types`, leaveType.Id, 'edit']);
    }
  }

  configurePolicy(): void {
    const leaveType = this.leaveType();
    if (leaveType) {
      this.router.navigate([`${this.routePrefix()}/leave/policies/configure`, leaveType.Id]);
    }
  }

  goBack(): void {
    this.router.navigate([`${this.routePrefix()}/leave/types`]);
  }

  canEdit(): boolean {
    const leaveType = this.leaveType();
    return leaveType?.Scope === LeaveScope.Company;
  }

  isGlobalType(): boolean {
    return this.leaveType()?.Scope === LeaveScope.Global;
  }
}
