import { Component, signal, computed, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TagComponent } from '@app/shared/components/tag/tag.component';
import { TagVariant } from '@app/shared/components/tag/tag.types';
import { EmptyState } from '@app/shared/components/empty-state/empty-state';
import { LeaveService } from '@app/core/services/leave.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { LeaveType, LeaveScope, LeaveTypeCreateDto, LeaveTypePatchDto } from '@app/core/models';
import { MessageService, ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-leave-types',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    ToastModule,
    ConfirmDialogModule,
    DialogModule,
    CheckboxModule,
    TagComponent,
    EmptyState,
    IconFieldModule,
    InputIconModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './leave-types.html',
  styleUrl: './leave-types.css'
})
export class LeaveTypesPage implements OnInit {
  private leaveService = inject(LeaveService);
  private contextService = inject(CompanyContextService);
  private destroyRef = inject(DestroyRef);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private translate = inject(TranslateService);
  private fb = inject(FormBuilder);

  // State
  readonly leaveTypes = signal<LeaveType[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchQuery = signal('');
  readonly selectedScope = signal<LeaveScope | null>(null);

  // Dialog state
  readonly showDialog = signal(false);
  readonly isEditMode = signal(false);
  readonly isSaving = signal(false);
  readonly selectedLeaveType = signal<LeaveType | null>(null);

  // Form
  form!: FormGroup;

  // Computed
  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  readonly scopeOptions = signal([
    { label: 'Tous les types', value: null },
    { label: 'Légal (Global)', value: LeaveScope.Global },
    { label: 'Entreprise', value: LeaveScope.Company }
  ]);

  readonly dialogTitle = computed(() =>
    this.isEditMode() ? 'leave.types.edit' : 'leave.types.add'
  );

  readonly filteredLeaveTypes = computed(() => {
    let result = this.leaveTypes();

    // Filter by search query
    if (this.searchQuery()) {
      const query = this.searchQuery().toLowerCase();
      result = result.filter(lt =>
        lt.LeaveCode.toLowerCase().includes(query) ||
        lt.LeaveName.toLowerCase().includes(query)
      );
    }

    // Filter by scope
    if (this.selectedScope() !== null) {
      result = result.filter(lt => lt.Scope === this.selectedScope());
    }

    return result;
  });

  readonly stats = computed(() => {
    const types = this.leaveTypes();
    return {
      total: types.length,
      global: types.filter(t => t.Scope === LeaveScope.Global).length,
      company: types.filter(t => t.Scope === LeaveScope.Company).length,
      active: types.filter(t => t.IsActive).length
    };
  });

  readonly statCards = [
    {
      label: 'leave.types.stats.total',
      accessor: (stats: any) => stats.total,
      icon: 'pi pi-list',
      iconColor: 'text-blue-500'
    },
    {
      label: 'leave.types.stats.global',
      accessor: (stats: any) => stats.global,
      icon: 'pi pi-globe',
      iconColor: 'text-purple-500'
    },
    {
      label: 'leave.types.stats.company',
      accessor: (stats: any) => stats.company,
      icon: 'pi pi-building',
      iconColor: 'text-orange-500'
    },
    {
      label: 'leave.types.stats.active',
      accessor: (stats: any) => stats.active,
      icon: 'pi pi-check-circle',
      iconColor: 'text-green-500'
    }
  ];

  // Two-way binding helpers
  get searchQueryModel(): string {
    return this.searchQuery();
  }

  set searchQueryModel(value: string) {
    this.searchQuery.set(value);
  }

  get selectedScopeModel(): LeaveScope | null {
    return this.selectedScope();
  }

  set selectedScopeModel(value: LeaveScope | null) {
    this.selectedScope.set(value);
  }

  get disableClearButton(): boolean {
    return (!this.searchQuery() && this.selectedScope() === null) || this.isLoading();
  }

  ngOnInit(): void {
    this.initForm();
    this.loadLeaveTypes();

    // Reload on context change
    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadLeaveTypes();
      });
  }

  private initForm(): void {
    this.form = this.fb.group({
      leaveCode: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
      leaveName: ['', [Validators.required, Validators.maxLength(100)]],
      leaveDescription: ['', [Validators.required, Validators.maxLength(500)]],
      isActive: [true]
    });
  }

  loadLeaveTypes(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const companyId = parseInt(this.contextService.companyId() || '0', 10);
    if (!companyId) {
      this.error.set('Aucune entreprise sélectionnée');
      this.isLoading.set(false);
      return;
    }

    this.leaveService.getAll(companyId).subscribe({
      next: (types) => {
        this.leaveTypes.set(types);
        this.isLoading.set(false);
      },
      error: (err: any) => {
        this.error.set(err.error?.message || 'Échec du chargement des types de congés');
        this.isLoading.set(false);
      }
    });
  }

  getScopeLabel(scope: number): string {
    return scope === LeaveScope.Global
      ? this.translate.instant('leave.types.global')
      : this.translate.instant('leave.types.company');
  }

  getScopeVariant(scope: number): TagVariant {
    return scope === LeaveScope.Global ? 'info' : 'warning';
  }

  viewLeaveType(leaveType: LeaveType): void {
    // For viewing, open dialog in read-only mode or just show details
    this.selectedLeaveType.set(leaveType);
    this.isEditMode.set(false);
    this.patchForm(leaveType);
    this.form.disable();
    this.showDialog.set(true);
  }

  addLeaveType(): void {
    this.selectedLeaveType.set(null);
    this.isEditMode.set(false);
    this.form.reset({
      leaveCode: '',
      leaveName: '',
      leaveDescription: '',
      isActive: true
    });
    this.form.enable();
    this.showDialog.set(true);
  }

  editLeaveType(leaveType: LeaveType, event: Event): void {
    event.stopPropagation();

    if (leaveType.Scope === LeaveScope.Global) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Action non autorisée',
        detail: 'Les types de congés légaux ne peuvent pas être modifiés'
      });
      return;
    }

    this.selectedLeaveType.set(leaveType);
    this.isEditMode.set(true);
    this.patchForm(leaveType);
    this.form.enable();
    this.showDialog.set(true);
  }

  private patchForm(leaveType: LeaveType): void {
    this.form.patchValue({
      leaveCode: leaveType.LeaveCode,
      leaveName: leaveType.LeaveName,
      leaveDescription: leaveType.LeaveDescription || '',
      isActive: leaveType.IsActive
    });
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.selectedLeaveType.set(null);
    this.form.reset();
  }

  saveLeaveType(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);

    if (this.isEditMode() && this.selectedLeaveType()) {
      this.updateLeaveType();
    } else {
      this.createLeaveType();
    }
  }

  private createLeaveType(): void {
    const companyId = parseInt(this.contextService.companyId() || '0', 10);
    const request: LeaveTypeCreateDto = {
      LeaveCode: this.form.value.leaveCode,
      LeaveName: this.form.value.leaveName,
      LeaveDescription: this.form.value.leaveDescription || '',
      Scope: LeaveScope.Company,
      CompanyId: companyId || null,
      IsActive: this.form.value.isActive
    };

    this.leaveService.create(request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Type de congé créé avec succès'
        });
        this.isSaving.set(false);
        this.closeDialog();
        this.loadLeaveTypes();
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err.error?.message || 'Échec de la création'
        });
        this.isSaving.set(false);
      }
    });
  }

  private updateLeaveType(): void {
    const leaveType = this.selectedLeaveType();
    if (!leaveType) return;

    const request: LeaveTypePatchDto = {
      LeaveCode: this.form.value.leaveCode,
      LeaveName: this.form.value.leaveName,
      LeaveDescription: this.form.value.leaveDescription,
      IsActive: this.form.value.isActive
    };

    this.leaveService.update(leaveType.Id, request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Type de congé mis à jour'
        });
        this.isSaving.set(false);
        this.closeDialog();
        this.loadLeaveTypes();
      },
      error: (err: any) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail: err.error?.message || 'Échec de la mise à jour'
        });
        this.isSaving.set(false);
      }
    });
  }

  deleteLeaveType(leaveType: LeaveType, event: Event): void {
    event.stopPropagation();

    // Only company-specific types can be deleted
    if (leaveType.Scope === LeaveScope.Global) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Action non autorisée',
        detail: 'Les types de congés légaux ne peuvent pas être supprimés'
      });
      return;
    }

    this.confirmationService.confirm({
      message: `Êtes-vous sûr de vouloir supprimer le type "${leaveType.LeaveName}" ?`,
      header: 'Confirmation de suppression',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',
      acceptButtonStyleClass: 'btn btn-danger',
      rejectButtonStyleClass: 'btn btn-secondary',
      accept: () => {
        this.leaveService.delete(leaveType.Id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Succès',
              detail: 'Type de congé supprimé'
            });
            this.loadLeaveTypes();
          },
          error: (err: any) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Erreur',
              detail: err.error?.message || 'Échec de la suppression'
            });
          }
        });
      }
    });
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.selectedScope.set(null);
  }

  canEdit(leaveType: LeaveType): boolean {
    // Only company-specific types can be edited
    return leaveType.Scope === LeaveScope.Company;
  }

  canDelete(leaveType: LeaveType): boolean {
    // Only company-specific types can be deleted
    return leaveType.Scope === LeaveScope.Company;
  }
}
