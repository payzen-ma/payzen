/**
 * Payroll Referentiel Component
 * Main container for managing:
 * - Legal Parameters (SMIG, etc.)
 * - Referentiel Elements (Compensation elements)
 * - Element Rules (Exemption rules for CNSS/IR)
 */

import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LegalParametersListComponent } from './components/legal-parameters/legal-parameters-list.component';
import { LegalParameterModalComponent } from './components/legal-parameters/legal-parameter-modal.component';
import { LegalParameterHistoryModalComponent } from './components/legal-parameters/legal-parameter-history-modal.component';
import { ReferentielElementModalComponent } from './components/referentiel-elements/referentiel-element-modal.component';
import { ElementRulesListComponent } from './components/element-rules/element-rules-list.component';
import { ElementRuleWizardComponent } from './components/element-rules/element-rule-wizard.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { PayrollReferentielService } from '../../services/payroll-referentiel/payroll-referentiel.service';
import {
  LegalParameterDto,
  CreateLegalParameterDto,
  UpdateLegalParameterDto,
  ReferentielElementListDto,
  CreateReferentielElementDto,
  UpdateReferentielElementDto,
  ElementRuleDto,
  CreateElementRuleDto,
  UpdateElementRuleDto
} from '../../models/payroll-referentiel';

type SectionId = 'legal-parameters' | 'elements-rules';

@Component({
  selector: 'app-payroll-referentiel',
  standalone: true,
  imports: [
    CommonModule,
    LegalParametersListComponent,
    LegalParameterModalComponent,
    LegalParameterHistoryModalComponent,
    ReferentielElementModalComponent,
    ElementRulesListComponent,
    ElementRuleWizardComponent,
    ModalComponent
  ],
  templateUrl: './payroll-referentiel.component.html'
})
export class PayrollReferentielComponent {
  /** Accordion: all sections open by default so RH see everything by scrolling */
  openSections: Record<SectionId, boolean> = {
    'legal-parameters': true,
    'elements-rules': true
  };

  sectionCounts: Record<SectionId, number> = {
    'legal-parameters': 0,
    'elements-rules': 0
  };

  // Legal Parameters modal state
  showLegalParameterModal = false;
  showLegalParameterHistoryModal = false;
  showDeleteBlockedModal = false;
  deleteBlockedMessage = '';
  deleteBlockedElements: { elementId: number; elementName: string }[] = [];
  selectedLegalParameter: LegalParameterDto | null = null;
  legalParameterModalMode: 'create' | 'edit' = 'create';

  // Elements modal state
  showElementModal = false;
  selectedElement: ReferentielElementListDto | null = null;
  elementModalMode: 'create' | 'edit' = 'create';

  // Element Rules state
  @ViewChild(ElementRuleWizardComponent) ruleWizardRef?: ElementRuleWizardComponent;
  showElementRulesView = false;
  showRuleWizard = false;
  selectedElementForRules: ReferentielElementListDto | null = null;
  selectedRule: ElementRuleDto | null = null;
  ruleWizardMode: 'create' | 'edit' = 'create';

  /** Incremented after save/delete so the active list refetches without full page reload */
  listRefreshTrigger = 0;

  constructor(
    private payrollService: PayrollReferentielService,
    private router: Router
  ) {}

  /**
   * Navigate back to salary packages page
   */
  navigateToSalaryPackages(): void {
    this.router.navigate(['/package-salary']);
  }

  /**
   * Toggle accordion section open/closed
   */
  toggleSection(sectionId: SectionId): void {
    this.openSections[sectionId] = !this.openSections[sectionId];
  }

  /**
   * Update section count when a list has loaded (for badge in header)
   */
  onSectionLoaded(sectionId: SectionId, count: number): void {
    this.sectionCounts[sectionId] = count;
  }

  /**
   * Tell the active list to refetch data (no full page reload)
   */
  refreshCurrentList(): void {
    this.listRefreshTrigger++;
  }

  // ============================================================
  // Legal Parameters Event Handlers
  // ============================================================

  onAddLegalParameter(): void {
    this.legalParameterModalMode = 'create';
    this.selectedLegalParameter = null;
    this.showLegalParameterModal = true;
  }

  onEditLegalParameter(param: LegalParameterDto): void {
    this.legalParameterModalMode = 'edit';
    this.selectedLegalParameter = param;
    this.showLegalParameterModal = true;
  }

  onDeleteLegalParameter(param: LegalParameterDto): void {
    if (!confirm(`Êtes-vous sûr de vouloir supprimer le paramètre "${param.name}" ?`)) {
      return;
    }

    this.payrollService.getLegalParameterUsage(param.id).subscribe({
      next: (usage) => {
        if (usage.used) {
          this.deleteBlockedMessage = 'Ce paramètre ne peut pas être supprimé car il est utilisé dans des formules de règles actives.';
          this.deleteBlockedElements = usage.usedByElements ?? [];
          this.showDeleteBlockedModal = true;
        } else {
          this.payrollService.deleteLegalParameter(param.id).subscribe({
            next: () => this.refreshCurrentList(),
            error: (err) => {
              console.error('Failed to delete legal parameter:', err);
              const msg = err?.error?.message ?? err?.error?.Message ?? 'Erreur lors de la suppression du paramètre.';
              alert(msg);
            }
          });
        }
      },
      error: (err) => {
        console.error('Failed to check legal parameter usage:', err);
        alert('Impossible de vérifier l\'utilisation du paramètre.');
      }
    });
  }

  onViewLegalParameterHistory(param: LegalParameterDto): void {
    this.selectedLegalParameter = param;
    this.showLegalParameterHistoryModal = true;
  }

  onSaveLegalParameter(event: CreateLegalParameterDto | { id: number; dto: UpdateLegalParameterDto }): void {
    // Check if this is an update (has id property) or create
    if ('id' in event) {
      // Update mode
      this.payrollService.updateLegalParameter(event.id, event.dto).subscribe({
        next: () => {
          this.showLegalParameterModal = false;
          this.refreshCurrentList();
        },
        error: (err: any) => {
          console.error('Failed to update legal parameter:', err);
          alert('Erreur lors de la mise à jour du paramètre.');
        }
      });
    } else {
      // Create mode
      this.payrollService.createLegalParameter(event).subscribe({
        next: () => {
          this.showLegalParameterModal = false;
          this.refreshCurrentList();
        },
        error: (err: any) => {
          console.error('Failed to create legal parameter:', err);
          alert('Erreur lors de la création du paramètre.');
        }
      });
    }
  }

  // ============================================================
  // Elements Event Handlers
  // ============================================================

  onAddElement(): void {
    this.elementModalMode = 'create';
    this.selectedElement = null;
    this.showElementModal = true;
  }

  onEditElement(element: ReferentielElementListDto): void {
    this.elementModalMode = 'edit';
    this.selectedElement = element;
    this.showElementModal = true;
  }

  onDeleteElement(element: ReferentielElementListDto): void {
    if (!confirm(`Êtes-vous sûr de vouloir supprimer l'élément "${element.name}" ?`)) {
      return;
    }

    this.payrollService.deleteReferentielElement(element.id).subscribe({
      next: () => {
        this.refreshCurrentList();
      },
      error: (err: any) => {
        console.error('Failed to delete element:', err);
        alert('Erreur lors de la suppression de l\'élément.');
      }
    });
  }

  onSaveElement(event: CreateReferentielElementDto | { id: number; dto: UpdateReferentielElementDto }): void {
    if ('id' in event) {
      // Update mode
      this.payrollService.updateReferentielElement(event.id, event.dto).subscribe({
        next: () => {
          this.showElementModal = false;
          this.refreshCurrentList();
        },
        error: (err: any) => {
          console.error('Failed to update element:', err);
          alert('Erreur lors de la mise à jour de l\'élément.');
        }
      });
    } else {
      // Create mode
      this.payrollService.createReferentielElement(event).subscribe({
        next: () => {
          this.showElementModal = false;
          this.refreshCurrentList();
        },
        error: (err: any) => {
          console.error('Failed to create element:', err);
          alert('Erreur lors de la création de l\'élément.');
        }
      });
    }
  }

  // ============================================================
  // Element Rules Event Handlers
  // ============================================================

  onAddRule(element: ReferentielElementListDto): void {
    this.ruleWizardMode = 'create';
    this.selectedElementForRules = element;
    this.selectedRule = null;
    this.showRuleWizard = true;
  }

  onEditRule(rule: ElementRuleDto): void {
    this.ruleWizardMode = 'edit';
    this.selectedRule = rule;
    // Create a minimal element object with the required id for edit mode
    // The wizard only needs the element.id for updates
    this.selectedElementForRules = {
      id: rule.elementId,
      code: undefined,
      name: '',
      categoryName: '',
      defaultFrequency: 'MONTHLY' as any,
      status: 'ACTIVE' as any,
      isActive: true,
      hasConvergence: true,
      ruleCount: 0,
      hasCnssRule: false,
      hasDgiRule: false
    };
    this.showRuleWizard = true;
  }

  onDeleteRule(rule: ElementRuleDto): void {
    if (!confirm(`Êtes-vous sûr de vouloir supprimer cette règle ?`)) {
      return;
    }

    this.payrollService.deleteElementRule(rule.id).subscribe({
      next: () => {
        this.refreshCurrentList();
      },
      error: (err: any) => {
        console.error('Failed to delete rule:', err);
        alert('Erreur lors de la suppression de la règle.');
      }
    });
  }

  onSaveRule(event: CreateElementRuleDto | { id: number; dto: UpdateElementRuleDto }): void {
    if ('id' in event) {
      // Update mode
      this.payrollService.updateElementRule(event.id, event.dto).subscribe({
        next: () => {
          this.showRuleWizard = false;
          this.refreshCurrentList();
        },
        error: (err: any) => {
          console.error('Failed to update rule:', err);
          const msg = this.extractErrorMessage(err) || 'Erreur lors de la mise à jour de la règle.';
          this.ruleWizardRef?.setError(msg);
        }
      });
    } else {
      // Create mode - DTO includes elementId
      this.payrollService.createElementRule(event).subscribe({
        next: () => {
          this.showRuleWizard = false;
          this.refreshCurrentList();
        },
        error: (err: any) => {
          console.error('Failed to create rule:', err);
          const msg = this.extractErrorMessage(err) || 'Erreur lors de la création de la règle.';
          this.ruleWizardRef?.setError(msg);
        }
      });
    }
  }

  private extractErrorMessage(err: any): string {
    const body = err?.error;
    if (typeof body === 'string') return body;
    if (body?.message) return body.message;
    if (body?.Message) return body.Message;
    // ASP.NET ModelState errors
    if (body?.errors) {
      const msgs = Object.values(body.errors).flat();
      return msgs.join('; ');
    }
    if (body?.title) return body.title;
    return '';
  }
}
