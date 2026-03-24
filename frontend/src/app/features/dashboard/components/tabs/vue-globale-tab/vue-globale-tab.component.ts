import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VueGlobaleData } from '../../../state/dashboard-hr.models';
import { SectionHeaderComponent } from '../../shared/section-header/section-header.component';
import { KpiCardComponent } from '../../shared/kpi-card/kpi-card.component';
import { BarChartComponent } from '../../shared/bar-chart/bar-chart.component';
import { DonutChartComponent } from '../../shared/donut-chart/donut-chart.component';

@Component({
  selector: 'app-vue-globale-tab',
  standalone: true,
  imports: [CommonModule, SectionHeaderComponent, KpiCardComponent, BarChartComponent, DonutChartComponent],
  templateUrl: './vue-globale-tab.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VueGlobaleTabComponent {
  readonly data = input.required<VueGlobaleData>();
}
