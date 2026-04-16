MODULE[CUSTOM_PRIME_ANCIENNETE] {
  INPUTS  : salaire_base_26j, anciennete_annees
  OUTPUTS : taux_anciennete, prime_anciennete

  RULE custom.calcul_taux_anciennete {
    WHEN anciennete_annees < 2   THEN taux_anciennete = 0.00
    WHEN anciennete_annees < 5   THEN taux_anciennete = 0.10
    WHEN anciennete_annees >= 5  THEN taux_anciennete = 0.50
  }

  RULE custom.calcul_montant_anciennete {
    prime_anciennete = ROUND(salaire_base_26j * taux_anciennete, 2)
  }
}

MODULE[CUSTOM_PRIME_COMPLEXE_1] {
  INPUTS  : enfant_moins_3_ans, absence_2_ans, salaire_base_26j, categorie_professionnelle
  OUTPUTS : montant_prime_complexe_1

  RULE custom.prime_complexe_1 {
    WHEN enfant_moins_3_ans == true AND absence_2_ans == false AND salaire_base_26j >= 5000 AND salaire_base_26j <= 7000 {
      WHEN categorie_professionnelle == "cadre" 
        THEN montant_prime_complexe_1 = ROUND(salaire_base_26j * 0.20, 2)
      ELSE 
        montant_prime_complexe_1 = ROUND(salaire_base_26j * 0.10, 2)
    }
    ELSE {
      montant_prime_complexe_1 = 0.00
    }
  }
}