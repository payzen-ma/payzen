import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VueGlobaleData } from '../../../state/dashboard-hr.models';
import { KpiCardComponent } from '../../shared/kpi-card/kpi-card.component';
import { BarChartComponent } from '../../shared/bar-chart/bar-chart.component';
import { DonutChartComponent } from '../../shared/donut-chart/donut-chart.component';

interface FilterOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-vue-globale-tab',
  standalone: true,
  imports: [CommonModule, KpiCardComponent, BarChartComponent, DonutChartComponent],
  templateUrl: './vue-globale-tab.component.html',
  styleUrl: './vue-globale-tab.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VueGlobaleTabComponent {
  readonly data = input.required<VueGlobaleData>();

  // Filter inputs - sync with dashboard parent
  readonly departmentOptions = input<FilterOption[]>([]);
  readonly selectedDepartments = input<string[]>([]);
  readonly parityOptions = input<FilterOption[]>([]);
  readonly selectedParityMode = input<string>('all');
  readonly monthOptions = input<FilterOption[]>([]);
  readonly selectedMonth = input<string>('');

  // Filter outputs - emit to dashboard parent
  readonly departmentsChange = output<string[]>();
  readonly parityChange = output<string>();
  readonly monthChange = output<string>();

  onDepartmentsChange(values: string[] | null): void {
    this.departmentsChange.emit(values ?? []);
  }

  onParityChange(value: string | null): void {
    if (value) {
      this.parityChange.emit(value);
    }
  }

  onMonthChange(value: string | null): void {
    if (value) {
      this.monthChange.emit(value);
    }
  }
}
