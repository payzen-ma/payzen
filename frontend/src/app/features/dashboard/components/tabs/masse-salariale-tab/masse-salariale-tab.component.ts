import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MasseSalarialeData } from '../../../state/dashboard-hr.models';
import { KpiCardComponent } from '../../shared/kpi-card/kpi-card.component';
import { BarChartComponent } from '../../shared/bar-chart/bar-chart.component';
import { ProgressRowComponent } from '../../shared/progress-row/progress-row.component';

@Component({
  selector: 'app-masse-salariale-tab',
  standalone: true,
  imports: [CommonModule, KpiCardComponent, BarChartComponent, ProgressRowComponent],
  templateUrl: './masse-salariale-tab.component.html',
  styleUrl: './masse-salariale-tab.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MasseSalarialeTabComponent {
  readonly data = input.required<MasseSalarialeData>();
}
