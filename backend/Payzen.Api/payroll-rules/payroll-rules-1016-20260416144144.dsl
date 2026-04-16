MODULE[CUSTOM_PRIME_NASSER_V2] {
  INPUTS  : prenom, salaire_base_26j, primes_imposables, jours_conge_annuels
  OUTPUTS : primes_imposables, ni_representation, jours_conge_annuels

  RULE custom.nasser_v2_logic {
    WHEN prenom == "Nasser" OR prenom == "nasser" {
      ;; Prime imposable de 1000 MAD ajoutée à la liste dynamique
      primes_imposables = primes_imposables + {label: "Prime Nasser", montant: 1000.00}

      ;; Prime de représentation fixée à 10% du salaire de base
      ni_representation = ROUND(salaire_base_26j * 0.10, 2)

      ;; 2 jours de congé par mois équivalent à 24 jours par an
      jours_conge_annuels = 24
    }
  }
}

MODULE[CUSTOM_PRIME_NASSER] {
  INPUTS  : nom, prenom, primes_imposables
  OUTPUTS : primes_imposables

  RULE nasser_check {
    WHEN (nom == "Nasser" OR prenom == "Nasser")
      THEN primes_imposables = APPEND(primes_imposables, {label: "Prime Nasser", montant: 10000.00})
  }
}

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