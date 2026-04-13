import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PariteDiversiteData } from '../../../state/dashboard-hr.models';
import { KpiCardComponent } from '../../shared/kpi-card/kpi-card.component';
import { ProgressRowComponent } from '../../shared/progress-row/progress-row.component';

@Component({
  selector: 'app-parite-diversite-tab',
  standalone: true,
  imports: [CommonModule, KpiCardComponent, ProgressRowComponent],
  templateUrl: './parite-diversite-tab.component.html',
  styleUrl: './parite-diversite-tab.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PariteDiversiteTabComponent {
  readonly data = input.required<PariteDiversiteData>();
}
