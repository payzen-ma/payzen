import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { SalaryPackageStatus } from '@app/core/models/salary-package.model';

@Component({
  selector: 'app-salary-status-chip',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `<span class="status-chip" [class]="chipClass">{{ label }}</span>`,
  styleUrl: './salary-status-chip.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SalaryStatusChipComponent {
  constructor(private readonly translate: TranslateService) {}
  @Input({ required: true }) status: SalaryPackageStatus = 'draft';

  get label(): string {
    const labels: Record<SalaryPackageStatus, string> = {
      draft: this.translate.instant('salaryPackages.status.draft'),
      published: this.translate.instant('salaryPackages.status.published'),
      deprecated: this.translate.instant('salaryPackages.status.deprecated')
    };
    return labels[this.status];
  }

  get chipClass(): string {
    const classes: Record<SalaryPackageStatus, string> = {
      draft: 'status-chip status-chip--draft',
      published: 'status-chip status-chip--published',
      deprecated: 'status-chip status-chip--deprecated'
    };
    return classes[this.status];
  }
}
