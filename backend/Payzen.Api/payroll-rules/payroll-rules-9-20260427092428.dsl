MODULE[CUSTOM_augmentation_salaire] {
  INPUTS  : fonction, anciennete_annees, salaire_base_26j
  OUTPUTS : salaire_base_ajuste, montant_augmentation

  RULE custom.augmentation_manager_senior {
    WHEN fonction == "Manager" AND anciennete_annees > 2 {
      montant_augmentation = ROUND(salaire_base_26j * 0.15, 2)
      salaire_base_ajuste  = salaire_base_26j + montant_augmentation
    }
    ELSE {
      montant_augmentation = 0
      salaire_base_ajuste  = salaire_base_26j
    }
  }
}