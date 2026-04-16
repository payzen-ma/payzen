import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { TabsModule } from 'primeng/tabs';
import { LeaveTypesPage } from './leave-types/leave-types';
import { LeavePoliciesPage } from './leave-policies/leave-policies';
import { LeaveLegalRulesPage } from './leave-legal-rules/leave-legal-rules';

@Component({
  selector: 'app-leave',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    TabsModule,
    LeaveTypesPage,
    LeavePoliciesPage,
    LeaveLegalRulesPage
  ],
  template: `
    <div class="p-6 leave-page">
      <div class="max-w-7xl mx-auto">
        <header class="mb-6">
          <h1 class="text-2xl font-semibold text-gray-900">{{ 'leave.title' | translate }}</h1>
          <p class="text-gray-600 mt-1">{{ 'leave.description' | translate }}</p>
        </header>
        
        <div class="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <p-tabs value="types" class="leave-tabs">
            <p-tablist class="border-b border-gray-200 p-1">
              <p-tab value="types" class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <i class="pi pi-sitemap"></i>
                  <span>{{ 'leave.tabs.types' | translate }}</span>
                </div>
              </p-tab>
              <p-tab value="policies" class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <i class="pi pi-book"></i>
                  <span>{{ 'leave.tabs.policies' | translate }}</span>
                </div>
              </p-tab>
              <p-tab value="legal-rules" class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <i class="pi pi-file-edit"></i>
                  <span>{{ 'leave.legalRules.title' | translate }}</span>
                </div>
              </p-tab>
            </p-tablist>
            
            <p-tabpanels class="p-0">
              <p-tabpanel value="types">
                <app-leave-types></app-leave-types>
              </p-tabpanel>
              <p-tabpanel value="policies">
                <app-leave-policies></app-leave-policies>
              </p-tabpanel>
              <p-tabpanel value="legal-rules">
                <app-leave-legal-rules></app-leave-legal-rules>
              </p-tabpanel>
            </p-tabpanels>
          </p-tabs>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .leave-page {
      background: var(--bg-page, #f8fafc);
      min-height: 100%;
    }

    .leave-tabs {
      --p-tabs-tablist-border-width: 0;
    }
    
    .leave-tabs p-tab {
      border-bottom: 0;
      border-radius: 8px;
      transition: all 0.2s ease;
      color: #64748b;
      font-weight: 600;
    }
    
    .leave-tabs p-tab:hover {
      background-color: #f8fafc;
    }
    
    .leave-tabs p-tab[aria-selected="true"] {
      background-color: #ebf5ff;
      color: #1557b0;
    }
  `]
})
export class LeaveComponent implements OnInit {
  
  constructor() { }

  ngOnInit(): void {
    // Component initialization
  }
}