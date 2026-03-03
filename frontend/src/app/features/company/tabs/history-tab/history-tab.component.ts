import { Component, inject, signal, ViewChild, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { CompanyService } from '@app/core/services/company.service';
import { AuditLogComponent } from '@app/shared/components/audit-log/audit-log.component';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-history-tab',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    AuditLogComponent
  ],
  templateUrl: './history-tab.component.html'
})
export class HistoryTabComponent implements OnInit, OnDestroy {
  private readonly companyContextService = inject(CompanyContextService);
  private readonly companyService = inject(CompanyService);

  @ViewChild(AuditLogComponent) auditLogComponent?: AuditLogComponent;

  // Get current company ID from context
  readonly companyId = this.companyContextService.companyId;
  
  // Make Number available in template
  protected readonly Number = Number;

  private updateSubscription?: Subscription;

  ngOnInit() {
    // Subscribe to company updates to refresh audit log
    this.updateSubscription = this.companyService.onCompanyUpdate$.subscribe(() => {
      this.auditLogComponent?.refresh();
    });
  }

  ngOnDestroy() {
    this.updateSubscription?.unsubscribe();
  }
}
