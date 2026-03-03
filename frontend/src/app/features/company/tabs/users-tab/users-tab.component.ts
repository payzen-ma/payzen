import { Component, OnInit, OnDestroy, inject, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { AvatarModule } from 'primeng/avatar';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { MenuModule } from 'primeng/menu';
import { MessageModule } from 'primeng/message';
import { MessageService, MenuItem } from 'primeng/api';
import { UserService, AvailableEmployee } from '../../../../core/services/user.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { forkJoin, of } from 'rxjs';
import { PermissionManagementService } from '../../../../core/services/permission-management.service';
import { User, UserRole } from '../../../../core/models/user.model';
import { CompanyContextService } from '../../../../core/services/companyContext.service';
import { Subscription } from 'rxjs';

interface UserDisplay {
  id: string;
  userId: string | null; // User ID for API calls
  name: string;
  email: string;
  role: string;
  status: string;
  initials: string;
  avatarColor: string;
}

interface RoleOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-users-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    // Needed for [(ngModel)] in assign role dialog
    FormsModule,
    TranslateModule,
    ButtonModule,
    TableModule,
    AvatarModule,
    TagModule,
    DialogModule,
    SelectModule,
    InputTextModule,
    ToastModule,
    MenuModule,
    MessageModule
  ],
  providers: [MessageService],
  templateUrl: './users-tab.component.html'
})
export class UsersTabComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly translate = inject(TranslateService);
  private readonly userService = inject(UserService);
  private readonly employeeService = inject(EmployeeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly contextService = inject(CompanyContextService);
  private readonly permissionService = inject(PermissionManagementService);
  private contextSub?: Subscription;
  private langChangeSub?: Subscription;

  // State
  users = signal<UserDisplay[]>([]);
  availableEmployees = signal<AvailableEmployee[]>([]);
  loading = signal(false);
  inviteDialogVisible = signal(false);
  inviteLoading = signal(false);
  loadingEmployees = signal(false);
  // Roles
  roles = signal<RoleOption[]>([]);
  roleLoadError = signal<string | null>(null);

  // Assign role dialog
  assignRoleDialogVisible = signal(false);
  assigningRoleLoading = signal(false);
  selectedUserForAssign: UserDisplay | null = null;
  // Support multiple role selection
  selectedRoleIdsForAssign: string[] = [];
  // Track initial assigned roles to compute adds/removes
  initialAssignedRoleIds: string[] = [];
  assignRoleForm!: FormGroup;
  formSubmitted = false;

  // Forms
  inviteForm!: FormGroup;

  // Role options for select
  readonly roleOptions: RoleOption[] = [
    // initial placeholder; will be replaced by loaded roles
  ];

  // Localized labels for Assign Role modal (populated on init)
  assignRoleTitle = '';
  assignRoleSubtitle = '';
  assignRoleUserLabel = '';
  assignRoleRoleLabel = '';
  assignRoleAssignLabel = '';
  

  // Avatar color palette
  private readonly avatarColors = [
    { bg: 'bg-blue-100', text: 'text-blue-700' },
    { bg: 'bg-green-100', text: 'text-green-700' },
    { bg: 'bg-purple-100', text: 'text-purple-700' },
    { bg: 'bg-amber-100', text: 'text-amber-700' },
    { bg: 'bg-rose-100', text: 'text-rose-700' },
    { bg: 'bg-cyan-100', text: 'text-cyan-700' }
  ];

  ngOnInit() {
    this.initForm();
    this.loadUsers();
    this.loadRoles();
    
    // Subscribe to company context changes to refresh users
    this.contextSub = this.contextService.contextChanged$.subscribe(() => {
      this.loadUsers();
    });

    this.assignRoleForm = this.fb.group({
      role: [null]
    });

    // populate localized labels for the Assign Role modal and refresh on language change
    this.populateAssignRoleLabels();
    this.langChangeSub = this.translate.onLangChange.subscribe(() => this.populateAssignRoleLabels());
  }

  private populateAssignRoleLabels() {
    this.translate.get([
      'company.users.assignRole.title',
      'company.users.assignRole.subtitle',
      'company.users.assignRole.userLabel',
      'company.users.assignRole.roleLabel',
      'company.users.assignRole.assign'
    ]).subscribe(res => {
      this.assignRoleTitle = res['company.users.assignRole.title'] || this.assignRoleTitle;
      this.assignRoleSubtitle = res['company.users.assignRole.subtitle'] || this.assignRoleSubtitle;
      this.assignRoleUserLabel = res['company.users.assignRole.userLabel'] || this.assignRoleUserLabel;
      this.assignRoleRoleLabel = res['company.users.assignRole.roleLabel'] || this.assignRoleRoleLabel;
      this.assignRoleAssignLabel = res['company.users.assignRole.assign'] || this.assignRoleAssignLabel;
    });
  }

  private loadRoles() {
    this.userService.getRoles().subscribe({
      next: (items) => {
        const options = items
          .filter(r => {
            const name = (r.name || '').toLowerCase();
            const code = (r.code || '').toLowerCase();
            return !name.includes('admin payzen') && !code.includes('adminpayzen');
          })
          .map(r => ({ 
            label: r.name, 
            value: String(r.id)
          }));
        this.roles.set(options);
        console.log('[UsersTab] loadRoles -> roles set', options);
      },
      error: (err) => {
        console.error('Error loading roles', err);
        this.roleLoadError.set(this.translate.instant('company.users.messages.loadRolesError'));
      }
    });
  }

  ngOnDestroy() {
    if (this.contextSub) {
      this.contextSub.unsubscribe();
    }
    if (this.langChangeSub) {
      this.langChangeSub.unsubscribe();
    }
  }

  /** Check if a form field is invalid and should show error */
  isFieldInvalid(fieldName: string): boolean {
    const control = this.inviteForm.get(fieldName);
    return !!(control?.invalid && (control.touched || this.formSubmitted));
  }

  /** Get avatar background color based on user name hash */
  getAvatarBgColor(user: UserDisplay): string {
    const index = this.getColorIndex(user.name);
    return this.avatarColors[index].bg;
  }

  /** Get avatar text color based on user name hash */
  getAvatarTextColor(user: UserDisplay): string {
    const index = this.getColorIndex(user.name);
    return this.avatarColors[index].text;
  }

  /** Get status badge classes */
  getStatusClasses(status: string): string {
    const statusMap: Record<string, string> = {
      active: 'bg-green-100 text-green-700',
      pending: 'bg-amber-100 text-amber-700',
      inactive: 'bg-gray-100 text-gray-600'
    };
    return statusMap[status] ?? 'bg-gray-100 text-gray-600';
  }

  /** Get status dot color class */
  getStatusDotClass(status: string): string {
    const dotMap: Record<string, string> = {
      active: 'bg-green-500',
      pending: 'bg-amber-500',
      inactive: 'bg-gray-400'
    };
    return dotMap[status] ?? 'bg-gray-400';
  }

  /** Get role badge classes */
  getRoleClasses(role: string): string {
    const roleMap: Record<string, string> = {
      'user.role.hr': 'bg-purple-100 text-purple-700',
      'user.role.manager': 'bg-blue-100 text-blue-700',
      'user.role.viewer': 'bg-gray-100 text-gray-600',
      'viewer': 'bg-gray-100 text-gray-600'
    };
    return roleMap[role] || 'bg-gray-100 text-gray-600';
  }

  /** Get menu items for a specific user */
  getUserMenuItems(user: UserDisplay): MenuItem[] {
    return [
      {
        label: this.translate.instant('company.users.manageRole'),
        icon: 'pi pi-user-edit',
        command: () => this.openAssignRoleDialog(user)
      },
      { separator: true },
      {
        label: this.translate.instant('company.users.actions.edit'),
        icon: 'pi pi-pencil',
        command: () => this.editUser(user)
      },
      {
        label: user.status === 'active'
          ? this.translate.instant('company.users.actions.deactivate')
          : this.translate.instant('company.users.actions.activate'),
        icon: user.status === 'active' ? 'pi pi-ban' : 'pi pi-check',
        command: () => this.toggleUserStatus(user)
      },
      { separator: true },
      {
        label: this.translate.instant('company.users.actions.remove'),
        icon: 'pi pi-trash',
        styleClass: 'text-red-600',
        command: () => this.removeUser(user)
      }
    ];
  }

  openAssignRoleDialog(user: UserDisplay) {
    console.log('[UsersTab] openAssignRoleDialog called for user', user);
    this.selectedUserForAssign = user;

    // Try to preload assigned roles from server and select accordingly.
    const employeeIdNum = Number(user.id);
    if (!Number.isNaN(employeeIdNum)) {
      this.userService.getEmployeeRoles(employeeIdNum).subscribe({
        next: (assignments) => {
          console.log('[UsersTab] getUserRoles -> assignments', assignments);
          const mappedIds: string[] = (Array.isArray(assignments) ? assignments : [])
            .map(a => String(a?.RoleId ?? a?.roleId ?? a?.Role?.id ?? a?.role?.id ?? a?.id ?? ''))
            .filter(Boolean);

          this.initialAssignedRoleIds = mappedIds;

          const openWithRoles = () => {
            // select those role ids that exist in loaded roles
            this.selectedRoleIdsForAssign = mappedIds
              .map(id => this.roles().find(o => String(o.value) === String(id)))
              .filter(Boolean)
              .map((m: any) => String(m.value));
            console.log('[UsersTab] preselected roles', this.selectedRoleIdsForAssign);
            this.blurActiveElement();
            this.assignRoleDialogVisible.set(true);
          };

          if (!this.roles() || this.roles().length === 0) {
            this.userService.getRoles().subscribe({
              next: (items) => {
                const options = items
                  .filter(r => {
                    const name = (r.name || '').toLowerCase();
                    const code = (r.code || '').toLowerCase();
                    return !name.includes('admin payzen') && !code.includes('adminpayzen');
                  })
                  .map(r => ({ label: r.name, value: String(r.id) }));
                this.roles.set(options);
                openWithRoles();
              },
              error: (e) => {
                console.error('Error loading roles while matching user role', e);
                this.selectInitialRoleFromDisplay(user);
                this.blurActiveElement();
                this.assignRoleDialogVisible.set(true);
              }
            });
            return;
          }

          if (mappedIds.length > 0) {
            openWithRoles();
            return;
          }

          // Fallback to local name-based matching
          this.selectInitialRoleFromDisplay(user);
          this.blurActiveElement();
          this.assignRoleDialogVisible.set(true);
        },
        error: (err) => {
          console.error('Error fetching user roles', err);
          this.selectInitialRoleFromDisplay(user);
          this.blurActiveElement();
          this.assignRoleDialogVisible.set(true);
        }
      });
    } else {
      this.selectInitialRoleFromDisplay(user);
      this.blurActiveElement();
      this.assignRoleDialogVisible.set(true);
    }
  }

  private selectInitialRoleFromDisplay(user: UserDisplay) {
    const currentRoleName = user.role?.replace('user.role.', '').toLowerCase() || null;
    const options = this.roles();
    const match = options.find(o => 
      String(o.label).toLowerCase().includes(currentRoleName || '') ||
      currentRoleName?.includes(String(o.label).toLowerCase())
    );
    const single = match ? match.value : null;
    this.selectedRoleIdsForAssign = single ? [String(single)] : [];
    this.initialAssignedRoleIds = this.selectedRoleIdsForAssign.slice();
    this.assignRoleForm.patchValue({ role: single });
  }

  onAssignRoleDialogVisibleChange(visible: boolean) {
    this.assignRoleDialogVisible.set(visible);
    if (!visible) {
      // reset temporary state when dialog is closed
      this.selectedUserForAssign = null;
      this.selectedRoleIdsForAssign = [];
      this.initialAssignedRoleIds = [];
      this.assignRoleForm.reset();
      this.assigningRoleLoading.set(false);
    }
  }

  assignRole() {
    if (!this.selectedUserForAssign) return;
    const employeeId = Number(this.selectedUserForAssign.id);
    if (isNaN(employeeId)) {
      this.showToast(
        'error',
        this.translate.instant('common.error'),
        this.translate.instant('company.users.messages.employeeIdMissing')
      );
      return;
    }
    const selectedIds = (this.selectedRoleIdsForAssign || []).map(String).filter(Boolean);
    if (!selectedIds || selectedIds.length === 0) {
      this.showToast(
        'error',
        this.translate.instant('common.error'),
        this.translate.instant('company.users.messages.noRoleSelected')
      );
      return;
    }

    this.assigningRoleLoading.set(true);

    const roleIdsToAssign = selectedIds.map(Number);

    // Use the new employee-based endpoint
    const assign$ = this.permissionService.assignRolesToEmployee(employeeId, roleIdsToAssign);

    assign$.subscribe({
      next: () => {
        this.assigningRoleLoading.set(false);
        this.assignRoleDialogVisible.set(false);
        this.showToast(
          'success',
          this.translate.instant('common.success'),
          this.translate.instant('company.users.messages.rolesUpdated')
        );
        this.loadUsers();
      },
      error: (err) => {
        console.error('Error assigning roles', err);
        this.assigningRoleLoading.set(false);
        const apiMsg = this.formatApiError(err);
        this.showToast('error', this.translate.instant('common.error'), apiMsg);
      }
    });
  }

  selectRole(roleValue: string) {
    // Toggle selection for multi-role: add/remove from selectedRoleIdsForAssign
    const idx = this.selectedRoleIdsForAssign.indexOf(String(roleValue));
    if (idx >= 0) {
      this.selectedRoleIdsForAssign.splice(idx, 1);
    } else {
      this.selectedRoleIdsForAssign.push(String(roleValue));
    }
    // keep the form's single role value for compatibility (first selected or null)
    if (this.assignRoleForm) this.assignRoleForm.patchValue({ role: this.selectedRoleIdsForAssign[0] ?? null });
  }

  isRoleSelected(roleValue: string): boolean {
    return (this.selectedRoleIdsForAssign || []).some(id => String(id) === String(roleValue));
  }

  openInviteDialog() {
    this.inviteForm.reset();
    this.formSubmitted = false;
    this.inviteDialogVisible.set(true);
    this.loadAvailableEmployees();
  }

  closeInviteDialog() {
    this.inviteDialogVisible.set(false);
  }

  /** Load employees without user accounts */
  private loadAvailableEmployees() {
    const companyId = this.contextService.companyId();
    if (!companyId) return;

    this.loadingEmployees.set(true);
    this.userService.getAvailableEmployees(Number(companyId)).subscribe({
      next: (employees) => {
        this.availableEmployees.set(employees);
        this.loadingEmployees.set(false);
      },
      error: (err) => {
        console.error('Error loading available employees:', err);
        this.loadingEmployees.set(false);
      }
    });
  }

  sendInvite() {
    this.formSubmitted = true;
    if (this.inviteForm.invalid) {
      this.inviteForm.markAllAsTouched();
      return;
    }

    this.inviteLoading.set(true);
    const { employee, role } = this.inviteForm.value;
    const companyIdStr = this.contextService.companyId();
    const companyId = companyIdStr ? Number(companyIdStr) : 0;

    // Use the selected employee's email
    const email = employee?.email;
    if (!email) {
      this.showToast(
        'error',
        this.translate.instant('common.error'),
        this.translate.instant('company.users.messages.selectEmployee')
      );
      this.inviteLoading.set(false);
      return;
    }

    this.userService.inviteUser(email, role, companyId).subscribe({
      next: () => {
        this.inviteLoading.set(false);
        this.inviteDialogVisible.set(false);
        this.showToast('success', this.translate.instant('company.users.inviteSuccess'), 
          this.translate.instant('company.users.inviteSentTo', { name: employee.fullName }));
        this.loadUsers();
      },
      error: (err) => {
        console.error('Error sending invite:', err);
        this.inviteLoading.set(false);
        this.showToast(
          'error',
          this.translate.instant('common.error'),
          this.translate.instant('company.users.messages.inviteError')
        );
      }
    });
  }

  private initForm() {
    this.inviteForm = this.fb.group({
      employee: [null, Validators.required],
      role: ['', Validators.required]
    });

    // When employee is selected, auto-fill the email (for display purposes)
    this.inviteForm.get('employee')?.valueChanges.subscribe(employee => {
      // Employee object is selected directly
    });
  }

  private loadUsers() {
    const companyId = this.contextService.companyId();
    if (!companyId) {
      this.showToast(
        'error',
        this.translate.instant('common.error'),
        this.translate.instant('company.users.messages.companyNotSelected')
      );
      return;
    }

    this.loading.set(true);
    // Prefer fetching company employees (includes linked users and role info)
    this.employeeService.getEmployees({ companyId: Number(companyId) }).subscribe({
      next: (resp) => {
        const beforeCount = (resp?.employees || []).length;
        const employees = resp.employees || [];
        console.log('[UsersTab] employees fetched:', beforeCount);

        const displayUsers: UserDisplay[] = employees.map(e => {
          const email = (e as any).email ?? (e as any).Email ?? '';
          const roleRaw = (e as any).roleName ?? (e as any).RoleName ?? (e as any).role ?? null;
          const role = roleRaw ? `user.role.${roleRaw}` : 'user.role.viewer';
          const name = `${e.firstName ?? ''} ${e.lastName ?? ''}`.trim() || email || `#${e.id}`;
          const userId = (e as any).userId ?? (e as any).UserId ?? null;
          return {
            id: String((e as any).id ?? (e as any).Id ?? ''),
            userId: userId ? String(userId) : null,
            name,
            email,
            role,
            status: (e as any).statusRaw ?? (e as any).status ?? 'inactive',
            initials: this.getInitials(name || email || ''),
            avatarColor: 'blue'
          };
        });

        this.users.set(displayUsers);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading employees for users tab:', err);
        this.loading.set(false);
        this.showToast(
          'error',
          this.translate.instant('common.error'),
          this.translate.instant('company.users.messages.loadEmployeesError')
        );
      }
    });
  }

  private getColorIndex(name: string): number {
    const hash = name.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
    return hash % this.avatarColors.length;
  }

  private getInitials(name: string): string {
    return name
      .split(' ')
      .map(part => part[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  /** Format a name into Proper Case (Title Case) for display */
  formatProperName(name?: string | null): string {
    if (!name) return '';
    return String(name)
      .split(' ')
      .filter(Boolean)
      .map(part => part.charAt(0).toUpperCase() + part.slice(1).toLowerCase())
      .join(' ');
  }

  private editUser(user: UserDisplay) {
    // TODO: Implement edit functionality
    console.log('Edit user:', user);
  }

  private toggleUserStatus(user: UserDisplay) {
    // TODO: Implement real API call
    this.showToast(
      'info',
      this.translate.instant('company.users.messages.notImplementedTitle'),
      this.translate.instant('company.users.messages.notImplemented')
    );
  }

  private removeUser(user: UserDisplay) {
    this.userService.deleteUser(user.id).subscribe({
      next: () => {
        this.users.update(users => users.filter(u => u.id !== user.id));
        this.showToast(
          'info',
          this.translate.instant('common.success'),
          this.translate.instant('company.users.messages.userRemoved', { name: user.name })
        );
      },
      error: (err) => {
        console.error('Error removing user:', err);
        this.showToast(
          'error',
          this.translate.instant('common.error'),
          this.translate.instant('company.users.messages.removeUserError')
        );
      }
    });
  }

  private showToast(severity: 'success' | 'error' | 'info', summary: string, detail: string) {
    this.messageService.add({ severity, summary, detail, life: 4000 });
  }

  private blurActiveElement(): void {
    try {
      if (typeof document !== 'undefined' && document.activeElement instanceof HTMLElement) {
        const el = document.activeElement as HTMLElement;
        el.blur();
        console.log('[UsersTab] blurActiveElement: blurred active element');
      }
    } catch (e) {
      console.log('[UsersTab] blurActiveElement error', e);
    }
  }

  private formatApiError(err: any): string {
    try {
      if (!err) return this.translate.instant('common.error');
      const errorObj = err?.error ?? err;

      // Prefer common server message fields (capitalized and lowercase)
      if (errorObj?.Message) return String(errorObj.Message);
      if (errorObj?.message) return String(errorObj.message);
      if (errorObj?.detail) return String(errorObj.detail);

      // If error is an array, join stringified elements
      if (Array.isArray(errorObj)) return errorObj.map(e => typeof e === 'string' ? e : JSON.stringify(e)).join('; ');

      // If it's a plain string (possibly JSON), try parsing
      if (typeof errorObj === 'string') {
        try {
          const parsed = JSON.parse(errorObj);
          if (parsed?.Message) return String(parsed.Message);
          if (parsed?.message) return String(parsed.message);
          if (parsed?.detail) return String(parsed.detail);
        } catch (_) {
          // not JSON, return as-is
          const s = errorObj as string;
          return s.length > 300 ? s.slice(0, 297) + '...' : s;
        }
      }

      // Fall back to HttpErrorResponse metadata
      if (err?.status && err?.statusText) return `${err.status} ${err.statusText}`;
      if (err?.message) return String(err.message);

      // As a last resort, stringify the error object
      const out = JSON.stringify(errorObj);
      if (!out) return this.translate.instant('common.error');
      return out.length > 300 ? out.slice(0, 297) + '...' : out;
    } catch (e) {
      return this.translate.instant('common.error');
    }
  }
}
