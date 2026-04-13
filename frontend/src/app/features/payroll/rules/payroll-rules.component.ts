import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { AccordionModule } from 'primeng/accordion';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

interface PayrollRuleModule {
  id: string;
  title: string;
  description: string;
  code: string;
}

@Component({
  selector: 'app-payroll-rules',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    AccordionModule,
    ButtonModule,
    TextareaModule,
    InputTextModule,
    CardModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './payroll-rules.component.html',
  styleUrls: ['./payroll-rules.component.css']
})
export class PayrollRulesComponent {

  // Modules extraits du PAYZEN DSL v3.1 (regles_paie.txt)
  readonly constants = signal<Array<{ name: string, value: string, details?: string }>>([
    { name: 'PLAFOND_CNSS_MENSUEL', value: '6 000.00 MAD' },
    { name: 'CNSS_RG_SALARIAL', value: '4.48 %' },
    { name: 'CNSS_RG_PATRONAL', value: '8.98 %' },
    { name: 'CNSS_AMO_SALARIAL', value: '2.26 %' },
    { name: 'CNSS_AMO_PATRONAL', value: '2.26 %' },
    { name: 'CNSS_AMO_PARTICIPATION_PAT', value: '1.85 %' },
    { name: 'CNSS_ALLOC_FAM_PAT', value: '6.40 %' },
    { name: 'CNSS_FP_PAT', value: '1.60 %' },
    { name: 'PLAFOND_NI_TRANSPORT', value: '500.00 / mois' },
    { name: 'PLAFOND_NI_TOURNEE', value: '1 500.00 / mois' },
    { name: 'PLAFOND_NI_PANIER_JOUR', value: '34.20 / jour' },
    { name: 'PLAFOND_NI_CAISSE', value: '239.00 / 190.00 (DGI)' },
    { name: 'IR_DEDUCTION_FAMILLE', value: '30.00 / personne' }
  ]);

  readonly globalRules = signal<PayrollRuleModule[]>([
    {
      id: '01',
      title: 'MODULE[01] Ancienneté',
      description: 'Calcul des années d\'ancienneté et du taux applicable pour la prime.',
      code: `RULE anciennete.2 {
  WHEN anciennete_annees < 2    THEN taux_anciennete = 0.00
  WHEN anciennete_annees < 5    THEN taux_anciennete = 0.05
  WHEN anciennete_annees < 12   THEN taux_anciennete = 0.10
  WHEN anciennete_annees < 20   THEN taux_anciennete = 0.15
  WHEN anciennete_annees >= 20  THEN taux_anciennete = 0.20
}`
    },
    {
      id: '02',
      title: 'MODULE[02] Présence',
      description: 'Calcul du nombre de jours payés.',
      code: `RULE presence.2 {
  WHEN jours_payes_total >= 26
    THEN salaire_base_mensuel = salaire_base_26j
  WHEN jours_payes_total < 26
    THEN salaire_base_mensuel = ROUND(salaire_base_26j * jours_payes_total / 26, 2)
}`
    },
    {
      id: '03',
      title: 'MODULE[03] Heures Supplémentaires',
      description: 'Calcul des montants d\'heures supplémentaires à 25%, 50% et 100%.',
      code: `RULE hsupp.1 {
  base_hsupp   = salaire_base_mensuel + prime_anciennete
  taux_horaire = ROUND(base_hsupp / heures_mois, 4)
}
RULE hsupp.2 {
  mont_hsupp_25  = ROUND(h_sup_25pct  * taux_horaire * 1.25, 2)
  mont_hsupp_50  = ROUND(h_sup_50pct  * taux_horaire * 1.50, 2)
  mont_hsupp_100 = ROUND(h_sup_100pct * taux_horaire * 2.00, 2)
  total_hsupp    = mont_hsupp_25 + mont_hsupp_50 + mont_hsupp_100
}`
    },
    {
      id: '04',
      title: 'MODULE[04] Indemnités Non Imposables',
      description: 'Gestion des plafonds des indemnités exonérées et report des excédents dans le brut imposable.',
      code: `RULE ni.1 — Transport {
  ni_transport_exo       = MIN(ni_transport, PLAFOND_NI_TRANSPORT)
  ni_transport_imposable = MAX(0, ni_transport - PLAFOND_NI_TRANSPORT)
}
RULE ni.13 — Agrégation {
  total_ni_exonere = SUM(ni_*_exo)
  total_ni_excedent_imposable = SUM(ni_*_imposable)
}`
    },
    {
      id: '05',
      title: 'MODULE[05] Salaire Brut Imposable (SBI)',
      description: 'Détermination du salaire brut soumis à l\'impôt et cotisations.',
      code: `RULE sbi.1 {
  salaire_brut_imposable = salaire_base_mensuel
                         + prime_anciennete
                         + total_hsupp
                         + total_primes_imposables
                         + total_ni_excedent_imposable
}`
    },
    {
      id: '06',
      title: 'MODULE[06] Cotisations CNSS & AMO',
      description: 'Taux salariaux et patronaux pour CNSS et AMO.',
      code: `base_cnss_rg = MIN(salaire_brut_imposable, PLAFOND_CNSS_MENSUEL)
cnss_rg_salarial    = ROUND(base_cnss_rg * CNSS_RG_SALARIAL, 2)
cnss_amo_salarial   = ROUND(salaire_brut_imposable * CNSS_AMO_SALARIAL, 2)
total_cnss_salarial = cnss_rg_salarial + cnss_amo_salarial`
    },
    {
      id: '07',
      title: 'MODULE[07] CIMR',
      description: 'Deductions salariales et charges patronales pour la CIMR selon la base de calcul définie.',
      code: `WHEN regime_cimr = AL_KAMIL {
  cimr_salarial = ROUND(salaire_brut_imposable * cimr_taux_salarial, 2)
  cimr_patronal = ROUND(salaire_brut_imposable * cimr_taux_patronal, 2)
}
WHEN regime_cimr = AL_MOUNASSIB {
  base_cimr = MAX(0, salaire_brut_imposable - PLAFOND_CNSS_MENSUEL)
  cimr_salarial = ROUND(base_cimr * cimr_taux_salarial, 2)
  cimr_patronal = ROUND(base_cimr * cimr_taux_patronal, 2)
}`
    },
    {
      id: '08',
      title: 'MODULE[08] Frais Professionnels',
      description: 'Abattement forfaitaire pour FP (25% ou 35% plafonné).',
      code: `base_fp = salaire_brut_imposable
WHEN base_fp <= 6500.00 { taux_fp = 0.35; plafond_fp = 2916.67 }
WHEN base_fp > 6500.00  { taux_fp = 0.25; plafond_fp = 2916.67 }
montant_fp = MIN(ROUND(base_fp * taux_fp, 2), plafond_fp)`
    },
    {
      id: '09',
      title: 'MODULE[09] Base IR (Revenu Net Imposable)',
      description: 'Base de calcul de l\'Impôt sur le Revenu.',
      code: `revenu_net_imposable = salaire_brut_imposable
                       - total_cnss_salarial
                       - cimr_salarial
                       - mutuelle_salariale
                       - montant_fp
                       - interet_pret_logement`
    },
    {
      id: '10',
      title: 'MODULE[10] Impôt sur le Revenu (IR)',
      description: 'Barème progressif de l\'IR mensuel.',
      code: `WHEN RNI <= 3333.33  { taux = 0.00; deduction = 0.00 }
WHEN RNI <= 5000.00  { taux = 0.10; deduction = 333.33 }
WHEN RNI <= 6666.67  { taux = 0.20; deduction = 833.33 }
WHEN RNI <= 8333.33  { taux = 0.30; deduction = 1500.00 }
WHEN RNI <= 15000.00 { taux = 0.34; deduction = 1833.33 }
WHEN RNI >  15000.00 { taux = 0.37; deduction = 2283.33 }

ir_brut = ROUND(RNI * taux_ir, 2)
ir_final = MAX(0, ir_brut - deduction_bareme - (situation_fam * 30))`
    }
  ]);

  // État de la vue
  isCreatingNewRule = signal(false);

  // Formulaire pour la nouvelle règle
  newRuleName = signal('');
  newRuleDescription = signal('');

  constructor(private messageService: MessageService) {}

  startNewRule() {
    this.isCreatingNewRule.set(true);
    // Scroll automatically to the editor section
    setTimeout(() => {
      const el = document.getElementById('new-rule-section');
      if (el) el.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  }

  cancelNewRule() {
    this.isCreatingNewRule.set(false);
    this.resetForm();
  }

  saveNewRule() {
    if (!this.newRuleName() || !this.newRuleDescription()) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'Le titre et la description détaillée de la règle sont requis.'
      });
      return;
    }

    // Simulation d'une sauvegarde
    this.messageService.add({
      severity: 'success',
      summary: 'Succès',
      detail: 'Règle sauvegardée avec succès (Simulation).'
    });

    this.isCreatingNewRule.set(false);
    this.resetForm();
  }

  private resetForm() {
    this.newRuleName.set('');
    this.newRuleDescription.set('');
  }
}
