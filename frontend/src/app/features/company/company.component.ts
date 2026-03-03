import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { TabsModule } from 'primeng/tabs';
import { CompanyInfoTabComponent } from './tabs/company-info-tab/company-info-tab.component';
import { HrSettingsTabComponent } from './tabs/hr-settings-tab/hr-settings-tab.component';
import { DocumentsTabComponent } from './tabs/documents-tab/documents-tab.component';
import { UsersTabComponent } from './tabs/users-tab/users-tab.component';
import { HistoryTabComponent } from './tabs/history-tab/history-tab.component';
import { DepartmentTabComponent } from './tabs/department-tab/department-tab.component';
import { JobPositionTabComponent } from './tabs/job-position-tab/job-position-tab.component';
import { ContractTypeTabComponent } from './tabs/contract-type-tab/contract-type-tab.component';
import { EmployeeCategoriesTabComponent } from './tabs/employee-categories-tab/employee-categories-tab.component';
import { HolidaysComponent } from '../holidays/holidays';

@Component({
  selector: 'app-company',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    TabsModule,
    CompanyInfoTabComponent,
    HrSettingsTabComponent,
    DocumentsTabComponent,
    UsersTabComponent,
    HistoryTabComponent,
    DepartmentTabComponent,
    JobPositionTabComponent,
    ContractTypeTabComponent,
    EmployeeCategoriesTabComponent,
    HolidaysComponent
  ],
  templateUrl: './company.html',
})
export class CompanyComponent {
  // activeIndex is not strictly needed for p-tabs if we use value="0" but let's keep it simple
}
