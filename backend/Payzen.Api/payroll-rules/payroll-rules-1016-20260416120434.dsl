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