import { Component, signal, computed, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { EmptyState } from '@app/shared/components/empty-state/empty-state';
import { LeaveTypeLegalRuleService } from '@app/core/services/leave-type-legal-rule.service';
import { LeaveService } from '@app/core/services/leave.service';
import { LeaveTypeLegalRule, LeaveType } from '@app/core/models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-leave-legal-rules',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    ToastModule,
    EmptyState,
    IconFieldModule,
    InputIconModule
  ],
  providers: [MessageService],
  templateUrl: './leave-legal-rules.html',
  styleUrl: './leave-legal-rules.css'
})
export class LeaveLegalRulesPage implements OnInit {
  private legalRuleService = inject(LeaveTypeLegalRuleService);
  private leaveService = inject(LeaveService);
  private destroyRef = inject(DestroyRef);
  private messageService = inject(MessageService);
  private translate = inject(TranslateService);

  // State
  readonly legalRules = signal<LeaveTypeLegalRule[]>([]);
  readonly leaveTypes = signal<LeaveType[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchQuery = signal('');

  // Computed
  readonly filteredLegalRules = computed(() => {
    let result = this.legalRules();

    if (this.searchQuery()) {
      const query = this.searchQuery().toLowerCase();
      result = result.filter(rule =>
        rule.eventCaseCode.toLowerCase().includes(query) ||
        rule.description.toLowerCase().includes(query) ||
        rule.legalArticle.toLowerCase().includes(query) ||
        this.getLeaveTypeName(rule).toLowerCase().includes(query)
      );
    }

    return result;
  });

  readonly stats = computed(() => {
    const all = this.legalRules();
    const discontinuous = all.filter(r => r.canBeDiscontinuous);
    const withTimeLimit = all.filter(r => r.mustBeUsedWithinDays);

    return {
      total: all.length,
      discontinuous: discontinuous.length,
      withTimeLimit: withTimeLimit.length
    };
  });

  readonly statCards = [
    {
      label: 'leave.legalRules.stats.total',
      accessor: (stats: any) => stats.total,
      icon: 'pi pi-list',
      iconColor: 'text-blue-500'
    },
    {
      label: 'leave.legalRules.stats.discontinuous',
      accessor: (stats: any) => stats.discontinuous,
      icon: 'pi pi-calendar-times',
      iconColor: 'text-orange-500'
    },
    {
      label: 'leave.legalRules.stats.withTimeLimit',
      accessor: (stats: any) => stats.withTimeLimit,
      icon: 'pi pi-clock',
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
  }

  private loadData(): void {
    this.loadLegalRules();
    this.loadLeaveTypes();
  }

  private loadLegalRules(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.legalRuleService.getAll().subscribe({
      next: (rules: LeaveTypeLegalRule[]) => {
        this.legalRules.set(rules);
        this.isLoading.set(false);
      },
      error: (err: any) => {
        this.error.set(err.error?.message || 'Échec du chargement des règles légales');
        this.isLoading.set(false);
      }
    });
  }

  private loadLeaveTypes(): void {
    // Load all leave types to display names
    this.leaveService.getAll().subscribe({
      next: (types: LeaveType[]) => {
        this.leaveTypes.set(types);
      },
      error: (err: any) => {
        alert('Error loading leave types:');
      }
    });
  }

  getLeaveTypeName(rule: LeaveTypeLegalRule): string {
    const leaveType = this.leaveTypes().find(lt => lt.Id === rule.leaveTypeId);
    if (leaveType) {
      return `${leaveType.LeaveName} (${leaveType.LeaveCode})`;
    }
    return `Type ID: ${rule.leaveTypeId}`;
  }

  formatTimeLimit(days?: number | null): string {
    if (!days) return '-';

    if (days === 1) {
      return `${days} ${this.translate.instant('common.day')}`;
    } else if (days < 30) {
      return `${days} ${this.translate.instant('common.days')}`;
    } else if (days === 30 || days === 31) {
      return `1 ${this.translate.instant('common.month')}`;
    } else if (days < 365) {
      const months = Math.round(days / 30);
      return `${months} ${this.translate.instant('common.months')}`;
    } else {
      const years = Math.round(days / 365);
      return `${years} ${this.translate.instant('common.years')}`;
    }
  }

  clearSearch(): void {
    this.searchQuery.set('');
  }

  refresh(): void {
    this.loadData();
  }
}