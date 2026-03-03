import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { RadioButtonModule } from 'primeng/radiobutton';
import { FormsModule } from '@angular/forms';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CompanyService } from '@app/core/services/company.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { Company } from '@app/core/models/company.model';

interface CabinetUser {
  id: string;
  name: string;
  email: string;
  role: 'Owner' | 'Relay' | 'Viewer';
  accessScope: 'All' | 'Limited';
  accessibleCompanies: string[]; // IDs
  permissions: string[];
  isOwner: boolean;
}

interface PermissionModule {
  id: string;
  name: string;
  actions: {
    read: boolean;
    write: boolean;
    delete: boolean;
  };
}

@Component({
  selector: 'app-cabinet-permissions',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    TagModule,
    TooltipModule,
    DialogModule,
    CheckboxModule,
    RadioButtonModule,
    FormsModule,
    MessageModule,
    SkeletonModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './cabinet-permissions.html',
  styleUrl: './cabinet-permissions.css'
})
export class CabinetPermissionsComponent implements OnInit {
  private companyService = inject(CompanyService);
  private contextService = inject(CompanyContextService);
  private messageService = inject(MessageService);

  // Signals
  readonly users = signal<CabinetUser[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly managedCompanies = signal<Company[]>([]);
  
  // Dialog State
  readonly showEditor = signal<boolean>(false);
  readonly selectedUser = signal<CabinetUser | null>(null);
  readonly isSaving = signal<boolean>(false);
  
  // Current User Role
  readonly currentUserRole = this.contextService.role;
  readonly canManagePermissions = computed(() => this.currentUserRole() === 'Owner');

  // Editor Form State
  readonly selectedCompanies = signal<string[]>([]);
  readonly permissionLevel = signal<'full' | 'readonly'>('readonly');
  
  // Permission Matrix State (Mock for now)
  readonly modules = signal<PermissionModule[]>([
    { id: 'invoices', name: 'Invoices', actions: { read: true, write: false, delete: false } },
    { id: 'payroll', name: 'Payroll', actions: { read: true, write: false, delete: false } },
    { id: 'tax', name: 'Tax Declarations', actions: { read: true, write: false, delete: false } },
    { id: 'employees', name: 'Employees', actions: { read: true, write: false, delete: false } }
  ]);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    
    // Mock loading users for now as backend endpoint might not exist
    setTimeout(() => {
      this.users.set([
        {
          id: '1',
          name: 'Ayoub Owner',
          email: 'ayoub@payzen.ma',
          role: 'Owner',
          accessScope: 'All',
          accessibleCompanies: [],
          permissions: ['*'],
          isOwner: true
        },
        {
          id: '2',
          name: 'Mohammed Relay',
          email: 'mohammed@payzen.ma',
          role: 'Relay',
          accessScope: 'Limited',
          accessibleCompanies: ['101', '102'],
          permissions: ['read'],
          isOwner: false
        }
      ]);
      this.isLoading.set(false);
    }, 1000);

    // Load managed companies for the selector
    this.companyService.getManagedCompanies().subscribe(companies => {
      this.managedCompanies.set(companies);
    });
  }

  openEditor(user: CabinetUser): void {
    this.selectedUser.set(user);
    this.selectedCompanies.set([...user.accessibleCompanies]);
    this.permissionLevel.set(user.role === 'Relay' ? 'full' : 'readonly'); // Simplified logic
    
    // Reset matrix based on user permissions (mock logic)
    const currentModules = this.modules();
    if (user.role === 'Owner') {
       this.modules.set(currentModules.map(m => ({ ...m, actions: { read: true, write: true, delete: true } })));
    } else {
       // Reset to default or load actual
       this.modules.set(currentModules.map(m => ({ ...m, actions: { read: true, write: false, delete: false } })));
    }

    this.showEditor.set(true);
  }

  closeEditor(): void {
    this.showEditor.set(false);
    this.selectedUser.set(null);
  }

  savePermissions(): void {
    if (this.selectedCompanies().length === 0) {
      this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please select at least one company.' });
      return;
    }

    this.isSaving.set(true);
    
    // Mock save
    setTimeout(() => {
      this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Permissions updated successfully.' });
      this.isSaving.set(false);
      this.closeEditor();
      this.loadData(); // Reload
    }, 1000);
  }

  // Matrix Helpers
  toggleRow(moduleIndex: number, checked: boolean): void {
    const newModules = [...this.modules()];
    const module = newModules[moduleIndex];
    module.actions.read = checked;
    module.actions.write = checked;
    module.actions.delete = checked;
    this.modules.set(newModules);
  }

  toggleColumn(action: 'read' | 'write' | 'delete', checked: boolean): void {
    const newModules = this.modules().map(m => ({
      ...m,
      actions: {
        ...m.actions,
        [action]: checked
      }
    }));
    this.modules.set(newModules);
  }
  
  getSeverity(role: string): "success" | "info" | "warn" | "danger" | "secondary" | "contrast" | undefined {
    switch (role) {
      case 'Owner': return 'success';
      case 'Relay': return 'info';
      case 'Viewer': return 'secondary';
      default: return 'info';
    }
  }
}
