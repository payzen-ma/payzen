import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ConformiteSocialeData } from '../../../state/dashboard-hr.models';
import { KpiCardComponent } from '../../shared/kpi-card/kpi-card.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';

@Component({
  selector: 'app-conformite-sociale-tab',
  standalone: true,
  imports: [CommonModule, TableModule, KpiCardComponent, StatusBadgeComponent],
  templateUrl: './conformite-sociale-tab.component.html',
  styleUrl: './conformite-sociale-tab.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConformiteSocialeTabComponent {
  readonly data = input.required<ConformiteSocialeData>();
}
