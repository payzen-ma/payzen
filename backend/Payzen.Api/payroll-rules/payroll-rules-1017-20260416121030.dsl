MODULE[CUSTOM_PRIME_FIXE] {
  INPUTS  : 
  OUTPUTS : montant_prime_fixe

  RULE custom.prime_fixe_tous {
    montant_prime_fixe = 1000.00
  }
}

MODULE[CUSTOM_PRIME_EXCELLENCE] {
  INPUTS  : anciennete_annees
  OUTPUTS : montant_prime_excellence

  RULE custom.prime_excellence {
    WHEN anciennete_annees > 5  THEN montant_prime_excellence = 500.00
    WHEN anciennete_annees <= 5 THEN montant_prime_excellence = 0.00
  }
}