import { Component, OnInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  SalaryPackage,
  SalaryPackageStatus,
  SalaryPackageWriteRequest,
  PayComponent
} from '../../models/salary-package.model';
import { SalaryPackageService } from '../../services/salary-package.service';
import { DefaultCimrConfigService } from '../../services/payroll-referentiel/default-cimr-config.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { ToastService } from '../../shared/toast/toast.service';
import { ToastContainerComponent } from '../../shared/toast/toast-container.component';
import {
  TemplateListComponent,
  TemplateActionEvent
} from './components/template-list/template-list.component';
import {
  TemplateViewComponent,
  ViewAction
} from './components/template-view/template-view.component';
import {
  TemplateEditorComponent,
  EditorAction,
  DraftTemplate
} from './components/template-editor/template-editor.component';

type ViewMode = 'list' | 'view' | 'editor';

@Component({
  selector: 'app-salary-packages',
  standalone: true,
  imports: [
    CommonModule,
    TemplateListComponent,
    TemplateViewComponent,
    TemplateEditorComponent,
    ToastContainerComponent
  ],
  template: `
    <!-- Modern Full-Page Layout with Gradient Background -->
    <div class="min-h-screen bg-gradient-to-br from-gray-50 via-white to-blue-50">

      <!-- Animated Background Pattern -->
      <div class="fixed inset-0 opacity-[0.02] pointer-events-none">
        <div class="absolute inset-0" style="background-image: radial-gradient(circle at 1px 1px, rgb(59, 130, 246) 1px, transparent 0); background-size: 40px 40px;"></div>
      </div>

      <!-- Navigation Button to Payroll Referentiel -->
      <div class="relative pt-6 px-6">
        <button
          (click)="navigateToPayrollReferentiel()"
          class="inline-flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 hover:border-gray-400 transition-all shadow-sm">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
          </svg>
          Paie & Réglementations
        </button>
      </div>

      <!-- Main Content Area -->
      <div class="relative">
        <!-- List View with Modern Cards -->
        @if (viewMode === 'list') {
          
          <div class="max-w-[1600px] mx-auto px-6 py-8">
            <app-template-list
              [templates]="templates"
              [categories]="categories"
              [isLoading]="isLoading"
              (onNewTemplate)="startNewTemplate()"
              (onTemplateAction)="handleTemplateAction($event)"
              (onFiltersChange)="onFiltersChange($event)">
            </app-template-list>
          </div>
        }

        <!-- View Mode with Glass Morphism Design -->
        @if (viewMode === 'view' && selectedTemplate) {
          <div class="max-w-[1400px] mx-auto px-6 py-8">
            <app-template-view
              [template]="selectedTemplate"
              (onAction)="handleViewAction($event)">
            </app-template-view>
          </div>
        }

        <!-- Editor Mode with Split Layout -->
        @if (viewMode === 'editor') {
          <div class="max-w-[1600px] mx-auto px-6 py-8">
            <app-template-editor
              #editor
              [template]="selectedTemplate!"
              [payComponents]="payComponents"
              [categories]="categories"
              [isSaving]="isSaving"
              (onAction)="handleEditorAction($event)">
            </app-template-editor>
          </div>
        }
      </div>

      <!-- Loading Overlay -->
      @if (isSaving) {
        <div class="fixed inset-0 bg-black/20 backdrop-blur-sm z-50 flex items-center justify-center">
          <div class="bg-white rounded-2xl shadow-2xl p-8 flex flex-col items-center gap-4">
            <svg class="w-12 h-12 text-blue-600 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            <p class="text-lg font-medium text-gray-700">Enregistrement en cours...</p>
          </div>
        </div>
      }
    </div>

    <!-- Toast Container -->
    <app-toast-container></app-toast-container>
  `
})
export class SalaryPackagesComponent implements OnInit {
  @ViewChild('editor') editorComponent?: TemplateEditorComponent;

  private salaryPackageService = inject(SalaryPackageService);
  private defaultCimrService = inject(DefaultCimrConfigService);
  private confirm = inject(ConfirmService);
  private toast = inject(ToastService);
  private router = inject(Router);

  // Data
  templates: SalaryPackage[] = [];
  payComponents: PayComponent[] = [];
  selectedTemplate: SalaryPackage | null = null;

  // View state
  viewMode: ViewMode = 'list';

  // UI state
  isLoading = false;
  isSaving = false;

  // Computed
  get categories(): string[] {
    const cats = new Set<string>();
    for (const t of this.templates) {
      if (t.category?.trim()) cats.add(t.category.trim());
    }
    return Array.from(cats).sort();
  }

  getPublishedCount(): number {
    return this.templates.filter(t => t.status === 'published').length;
  }

  ngOnInit(): void {
    this.loadTemplates();
    this.loadPayComponents();
  }

  // ============ Data Loading ============

  private loadTemplates(callback?: () => void): void {
    this.isLoading = true;
    // Use getAll() instead of getGlobalTemplates() because /templates endpoint
    // filters out draft templates by default, but we want to see all templates
    this.salaryPackageService.getAll().subscribe({
      next: (all) => {
        // Filter client-side for OFFICIAL templates only
        const official = all.filter(
          t => t.templateType === 'OFFICIAL' && (t.companyId == null || t.isGlobalTemplate)
        );
        this.templates = this.sortTemplates(official);
        this.isLoading = false;
        if (callback) callback();
      },
      error: (err) => {
        console.error('Failed to load templates:', err);
        this.toast.error(this.extractError(err) || 'Erreur lors du chargement');
        this.templates = [];
        this.isLoading = false;
        if (callback) callback();
      }
    });
  }

  private loadPayComponents(): void {
    this.salaryPackageService.getPayComponents(undefined, true).subscribe({
      next: (components) => {
        this.payComponents = components;
      },
      error: () => {
        // Silent fail for pay components
      }
    });
  }

  // ============ Navigation ============

  navigateToPayrollReferentiel(): void {
    this.router.navigate(['/payroll-referentiel']);
  }

  startNewTemplate(): void {
    this.selectedTemplate = this.createEmptyTemplate();
    this.viewMode = 'editor';
  }

  backToList(): void {
    this.viewMode = 'list';
    this.selectedTemplate = null;
    // Reload list so newly created templates and latest data from server are shown
    this.loadTemplates();
  }

  viewTemplate(template: SalaryPackage): void {
    this.selectedTemplate = template;
    this.viewMode = 'view';
  }

  editTemplate(template: SalaryPackage): void {
    this.selectedTemplate = template;
    this.viewMode = 'editor';
  }

  // ============ List Actions ============

  handleTemplateAction(event: TemplateActionEvent): void {
    const { action, template } = event;

    switch (action) {
      case 'view':
        this.viewTemplate(template);
        break;
      case 'edit':
        this.editTemplate(template);
        break;
      case 'publish':
        this.publishTemplate(template);
        break;
      case 'deprecate':
        this.deprecateTemplate(template);
        break;
      case 'duplicate':
        this.duplicateTemplate(template);
        break;
      case 'delete':
        this.deleteTemplate(template);
        break;
    }
  }

  onFiltersChange(filters: { search: string; status: string; category: string }): void {
    // Filters are handled client-side in the list component
    // Could implement server-side filtering here if needed
  }

  // ============ View Actions ============

  handleViewAction(action: ViewAction): void {
    if (!this.selectedTemplate) return;

    switch (action) {
      case 'back':
        this.backToList();
        break;
      case 'edit':
        this.viewMode = 'editor';
        break;
      case 'duplicate':
        this.duplicateTemplate(this.selectedTemplate);
        break;
      case 'deprecate':
        this.deprecateTemplate(this.selectedTemplate);
        break;
    }
  }

  // ============ Editor Actions ============

  async handleEditorAction(action: EditorAction): Promise<void> {
    switch (action) {
      case 'back':
        await this.handleEditorBack();
        break;
      case 'save':
        this.saveTemplate();
        break;
      case 'publish':
        this.publishFromEditor();
        break;
      case 'duplicate':
        if (this.selectedTemplate) {
          this.duplicateTemplate(this.selectedTemplate);
        }
        break;
      case 'delete':
        if (this.selectedTemplate) {
          this.deleteTemplate(this.selectedTemplate);
        }
        break;
      case 'discard':
        this.discardChanges();
        break;
    }
  }

  private async handleEditorBack(): Promise<void> {
    if (this.editorComponent?.isDirty()) {
      const confirmed = await this.confirm.confirm('Vous avez des modifications non enregistrées. Voulez-vous les ignorer ?');
      if (!confirmed) return;
    }
    this.backToList();
  }

  private discardChanges(): void {
    if (this.selectedTemplate?.id) {
      // Reload template from server
      this.salaryPackageService.getById(this.selectedTemplate.id).subscribe({
        next: (template) => {
          this.selectedTemplate = template;
          this.toast.info('Modifications annulées');
        }
      });
    } else {
      this.backToList();
    }
  }

  // ============ CRUD Operations ============

  private saveTemplate(): void {
    if (!this.editorComponent || this.isSaving) return;

    const draft = this.editorComponent.getDraft();
    if (!draft.name?.trim() || !draft.category?.trim()) {
      this.toast.warning('Veuillez remplir les champs obligatoires');
      return;
    }

    const payload = this.buildWriteRequest(draft);
    this.isSaving = true;

    const request$ = draft.id === 0
      ? this.salaryPackageService.create(payload)
      : this.salaryPackageService.update(draft.id, payload);

    request$.subscribe({
      next: (saved) => {
        this.selectedTemplate = saved;
        this.editorComponent?.markAsSaved();
        this.toast.success('Brouillon enregistré');
        this.loadTemplates(() => {
          this.isSaving = false;
        });
      },
      error: (err) => {
        this.toast.error(this.extractError(err) || 'Erreur lors de la sauvegarde');
        this.isSaving = false;
      }
    });
  }

  private publishFromEditor(): void {
    if (!this.editorComponent || this.isSaving) return;

    const draft = this.editorComponent.getDraft();
    if (!this.editorComponent.canPublishTemplate()) {
      this.toast.warning('Corrigez les erreurs avant de publier');
      return;
    }

    this.confirm.confirm('Publier ce template ? Il sera verrouillé et disponible pour les clients.').then(confirmed => {
      if (!confirmed) return;

      // First save, then publish
      const payload = this.buildWriteRequest(draft);
      payload.status = 'published';
      this.isSaving = true;

      const request$ = draft.id === 0
        ? this.salaryPackageService.create(payload)
        : this.salaryPackageService.update(draft.id, payload);

      request$.subscribe({
        next: (saved) => {
          this.selectedTemplate = saved;
          this.toast.success('Template publié avec succès');
          this.viewMode = 'view';
          this.loadTemplates(() => {
            this.isSaving = false;
          });
        },
        error: (err) => {
          this.toast.error(this.extractError(err) || 'Erreur lors de la publication');
          this.isSaving = false;
        }
      });
    });
  }

  private publishTemplate(template: SalaryPackage): void {
    this.confirm.confirm('Publier ce template ? Il sera verrouillé et disponible pour les clients.').then(confirmed => {
      if (!confirmed) return;

      this.isSaving = true;
      this.salaryPackageService.publish(template.id).subscribe({
        next: (published) => {
          if (this.selectedTemplate?.id === published.id) {
            this.selectedTemplate = published;
          }
          this.toast.success('Template publié');
          this.loadTemplates(() => {
            this.isSaving = false;
          });
        },
        error: (err) => {
          this.toast.error(this.extractError(err) || 'Erreur lors de la publication');
          this.isSaving = false;
        }
      });
    });
  }

  private deprecateTemplate(template: SalaryPackage): void {
    this.confirm.confirm('Marquer ce template comme obsolète ? Il ne sera plus proposé pour de nouvelles assignations.').then(confirmed => {
      if (!confirmed) return;

      this.isSaving = true;
      this.salaryPackageService.deprecate(template.id).subscribe({
        next: (deprecated) => {
          if (this.selectedTemplate?.id === deprecated.id) {
            this.selectedTemplate = deprecated;
          }
          this.toast.success('Template marqué obsolète');
          this.loadTemplates(() => {
            this.isSaving = false;
          });
        },
        error: (err) => {
          this.toast.error(this.extractError(err) || 'Erreur lors de la mise à jour');
          this.isSaving = false;
        }
      });
    });
  }

  private duplicateTemplate(template: SalaryPackage): void {
    // Create local copy WITHOUT saving to database
    // It will only be saved when user clicks "Save" in the editor
    const localCopy: SalaryPackage = {
      ...template,
      id: 0,  // Mark as unsaved (triggers create on save)
      name: `${template.name} (Copie)`,
      status: 'draft',
      sourceTemplateId: template.id,
      sourceTemplateName: template.name,
      sourceTemplateVersion: template.version,
      createdAt: null,
      updatedAt: null,
      version: 1,
      isLocked: false,
      // Deep copy items without IDs (they'll be generated on save)
      items: template.items.map(item => ({
        ...item,
        id: undefined
      }))
    };

    this.selectedTemplate = localCopy;
    this.viewMode = 'editor';
    // No toast yet - duplicate isn't created until they save
  }

  private deleteTemplate(template: SalaryPackage): void {
    this.confirm.confirm(`Supprimer "${template.name}" ? Cette action est irréversible.`).then(confirmed => {
      if (!confirmed) return;

      this.isSaving = true;
      this.salaryPackageService.delete(template.id).subscribe({
        next: () => {
          this.toast.success('Template supprimé');
          if (this.selectedTemplate?.id === template.id) {
            this.viewMode = 'list';
            this.selectedTemplate = null;
          }
          this.loadTemplates(() => {
            this.isSaving = false;
          });
        },
        error: (err) => {
          this.toast.error(this.extractError(err) || 'Erreur lors de la suppression');
          this.isSaving = false;
        }
      });
    });
  }

  // ============ Helpers ============

  private createEmptyTemplate(): SalaryPackage {
    const defaultCimr = this.defaultCimrService.getDefaultCimrConfig();
    return {
      id: 0,
      name: '',
      category: '',
      description: '',
      baseSalary: 0,
      status: 'draft',
      companyId: null,
      companyName: null,
      templateType: 'OFFICIAL',
      regulationVersion: 'MA_2025',
      autoRules: { seniorityBonusEnabled: true, ruleVersion: 'MA_2025' },
      cimrConfig: { ...defaultCimr },
      cimrRate: defaultCimr.regime !== 'NONE' ? defaultCimr.employeeRate : null,
      hasPrivateInsurance: false,
      version: 1,
      sourceTemplateId: null,
      sourceTemplateName: null,
      sourceTemplateVersion: null,
      validFrom: null,
      validTo: null,
      isLocked: false,
      isGlobalTemplate: true,
      items: [],
      updatedAt: new Date().toISOString(),
      createdAt: new Date().toISOString()
    };
  }

  private buildWriteRequest(draft: DraftTemplate): SalaryPackageWriteRequest {
    return {
      name: draft.name.trim(),
      category: draft.category.trim(),
      description: draft.description?.trim() || null,
      baseSalary: Number(draft.baseSalary) || 0,
      status: draft.status,
      companyId: null, // Official templates have no company
      templateType: 'OFFICIAL',
      regulationVersion: 'MA_2025',
      autoRules: draft.autoRules,
      cimrConfig: draft.cimrConfig ?? null,
      cimrRate: draft.cimrRate ?? 0, // Legacy field; DB does not allow NULL, use 0 when no CIMR
      hasPrivateInsurance: draft.hasPrivateInsurance ?? false,
      validFrom: draft.validFrom ?? null,
      validTo: draft.validTo ?? null,
      items: draft.items
        .filter(item => item.label?.trim())
        .map((item, index) => ({
          id: item.id,
          payComponentId: item.payComponentId ?? null,
          referentielElementId: item.referentielElementId ?? null,
          label: item.label.trim(),
          defaultValue: Number(item.defaultValue) || 0,
          sortOrder: item.sortOrder ?? index + 1,
          type: item.type,
          isTaxable: item.isTaxable,
          isSocial: item.isSocial,
          isCIMR: item.isCIMR,
          isVariable: item.isVariable,
          exemptionLimit: item.exemptionLimit ?? null
        }))
    };
  }

  private sortTemplates(list: SalaryPackage[]): SalaryPackage[] {
    const statusOrder: Record<string, number> = { published: 0, draft: 1, deprecated: 2 };
    return [...list].sort((a, b) => {
      const statusDiff = (statusOrder[a.status] ?? 3) - (statusOrder[b.status] ?? 3);
      if (statusDiff !== 0) return statusDiff;
      return a.name.localeCompare(b.name);
    });
  }

  private updateTemplateInList(updated: SalaryPackage): void {
    const index = this.templates.findIndex(t => t.id === updated.id);
    if (index >= 0) {
      this.templates = this.sortTemplates([
        ...this.templates.slice(0, index),
        updated,
        ...this.templates.slice(index + 1)
      ]);
    } else {
      this.templates = this.sortTemplates([...this.templates, updated]);
    }
  }

  private extractError(err: any): string | null {
    if (!err) return null;
    const data = err.error ?? err;
    if (typeof data === 'string') return data;
    if (data?.Message) return data.Message;
    if (data?.message) return data.message;
    return err.message || null;
  }
}
