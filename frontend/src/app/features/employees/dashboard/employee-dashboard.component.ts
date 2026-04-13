import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ContractInfo, EmployeeDocument, LeaveDetail, PayslipDetail } from './employee-dashboard.model';
import { EmployeeDashboardService } from './employee-dashboard.service';

@Component({
    selector: 'app-employee-dashboard',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './employee-dashboard.component.html',
    styleUrl: './employee-dashboard.component.css'
})
export class EmployeeDashboardComponent implements OnInit {
    private dashboardService = inject(EmployeeDashboardService);

    isLoading = true;
    isRecalculatingLeaves = false;
    leaveRecalcError = '';
    employeeId: number | null = null;

    employeeName = '';
    initials = '';
    role = '';
    department = '';
    contractType = '';
    matricule = '';
    manager = '';
    seniority = '';

    salaryNet = 0;
    paidDate = '';

    leavesRemaining = 0;
    leavesTotal = 0;

    presenceDays = 0;
    presenceTotal = 0;

    extraHours = 0;

    leavesDetails: LeaveDetail[] = [];
    contractInfo: ContractInfo[] = [];
    payslipDetails: PayslipDetail[] = [];
    documents: EmployeeDocument[] = [];

    ngOnInit() {
        this.loadDashboardData();
    }

    loadDashboardData() {
        this.isLoading = true;
        this.dashboardService.getDashboardData().subscribe({
            next: (data: any) => {
                this.employeeId = data.EmployeeId || data.employeeId || null;
                this.employeeName = data.EmployeeName || data.employeeName;
                this.initials = data.Initials || data.initials;
                this.role = data.Role || data.role;
                this.department = data.Department || data.department;
                this.contractType = data.ContractType || data.contractType;
                this.matricule = data.Matricule || data.matricule;
                this.manager = data.Manager || data.manager;
                this.seniority = data.Seniority || data.seniority;

                this.salaryNet = data.SalaryNet || data.salaryNet || 0;
                this.paidDate = data.PaidDate || data.paidDate;

                this.leavesRemaining = data.LeavesRemaining || data.leavesRemaining || 0;
                this.leavesTotal = data.LeavesTotal || data.leavesTotal || 0;

                this.presenceDays = data.PresenceDays || data.presenceDays || 0;
                this.presenceTotal = data.PresenceTotal || data.presenceTotal || 0;

                this.extraHours = data.ExtraHours || data.extraHours || 0;

                // Map nested arrays that also use PascalCase
                this.leavesDetails = (data.LeavesDetails || data.leavesDetails || []).map((item: any) => ({
                    label: item.Label || item.label,
                    remaining: item.Remaining || item.remaining,
                    total: item.Total || item.total,
                    colorClass: item.ColorClass || item.colorClass,
                    isText: item.IsText || item.isText,
                    text: item.Text || item.text
                }));

                this.contractInfo = (data.ContractInfo || data.contractInfo || []).map((item: any) => ({
                    label: item.Label || item.label,
                    value: item.Value || item.value,
                    isTag: item.IsTag || item.isTag,
                    tagColor: item.TagColor || item.tagColor
                }));

                this.payslipDetails = (data.PayslipDetails || data.payslipDetails || []).map((item: any) => ({
                    label: item.Label || item.label,
                    value: item.Value || item.value,
                    type: item.Type || item.type
                }));

                this.documents = (data.Documents || data.documents || []).map((item: any) => ({
                    title: item.Title || item.title,
                    subtitle: item.Subtitle || item.subtitle,
                    status: item.Status || item.status
                }));

                this.isLoading = false;
            },
            error: (err) => {
                this.isLoading = false;
            }
        });
    }

    recalculateLeaves(): void {
        if (!this.employeeId || this.isRecalculatingLeaves) return;

        const now = new Date();
        const month = now.getMonth() + 1;
        const year = now.getFullYear();

        this.isRecalculatingLeaves = true;
        this.leaveRecalcError = '';
        this.dashboardService.recalculateLeaveBalance(this.employeeId, year, month).subscribe({
            next: () => {
                this.isRecalculatingLeaves = false;
                this.loadDashboardData();
            },
            error: (err) => {
                this.leaveRecalcError = err?.error?.Message || err?.error?.message || 'Recalcul impossible pour le moment.';
                this.isRecalculatingLeaves = false;
            }
        });
    }

    getLeaveToneClass(source: string): string {
        return `employee-dashboard-progress__fill employee-dashboard-progress__fill--${this.resolveTone(source)}`;
    }

    getTagToneClass(source?: string): string {
        return `employee-dashboard-tag employee-dashboard-tag--${this.resolveTone(source)}`;
    }

    private resolveTone(source?: string): 'success' | 'warning' | 'danger' | 'info' | 'muted' {
        const value = (source ?? '').toLowerCase();

        if (value.includes('success') || value.includes('green') || value.includes('emerald')) {
            return 'success';
        }

        if (value.includes('warning') || value.includes('amber') || value.includes('yellow') || value.includes('orange')) {
            return 'warning';
        }

        if (value.includes('danger') || value.includes('error') || value.includes('red')) {
            return 'danger';
        }

        if (value.includes('info') || value.includes('blue') || value.includes('cyan')) {
            return 'info';
        }

        return 'muted';
    }
}
