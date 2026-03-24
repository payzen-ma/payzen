import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PariteDiversiteData } from '../../../state/dashboard-hr.models';
import { SectionHeaderComponent } from '../../shared/section-header/section-header.component';
import { KpiCardComponent } from '../../shared/kpi-card/kpi-card.component';
import { ProgressRowComponent } from '../../shared/progress-row/progress-row.component';

@Component({
  selector: 'app-parite-diversite-tab',
  standalone: true,
  imports: [CommonModule, SectionHeaderComponent, KpiCardComponent, ProgressRowComponent],
  templateUrl: './parite-diversite-tab.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PariteDiversiteTabComponent {
  readonly data = input.required<PariteDiversiteData>();
}
