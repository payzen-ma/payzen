# 📋 Proposition : Données à Envoyer au LLM pour le Calcul de Paie

## 🎯 Résumé Exécutif

Le fichier `regles_paie.txt` est un **DSL (Domain Specific Language)** très complet qui définit 14 modules de calcul. Voici ce qui doit être envoyé au LLM :

---

## ✅ Données ACTUELLES dans `EmployeePayrollDto`

### 1. Infos Personnelles ✅
```csharp
- FullName              // ✅ OK
- CinNumber             // ✅ OK (cin dans DSL)
- CnssNumber            // ✅ OK (cnss_numero dans DSL)
- CimrNumber            // ✅ OK
- MaritalStatus         // ⚠️  À convertir en situation_fam (nombre de personnes à charge)
- NumberOfChildren      // ✅ OK (inclus dans situation_fam)
- HasSpouse             // ✅ OK (inclus dans situation_fam)
```

### 2. Contrat ✅
```csharp
- ContractType          // ✅ OK (PP, PO, STG, ANAPEC_IDMAJ, ANAPEC_TAHFIZ)
- LegalContractType     // ℹ️  Info supplémentaire (pas dans DSL)
- StateEmploymentProgram // ✅ Utile pour ANAPEC
- JobPosition           // ✅ OK (fonction dans DSL)
- ContractStartDate     // ✅ OK (date_embauche)
- AncienneteYears       // ✅ OK (calculé, anciennete_annees)
```

### 3. Salaire ✅
```csharp
- BaseSalary            // ✅ OK (salaire_base_26j)
- SalaryComponents      // ⚠️  Mapper sur prime_imposable_1/2/3
```

### 4. Package Salarial ⚠️
```csharp
- SalaryPackageName     // ℹ️  Info
- PackageItems          // ⚠️  À mapper sur NI (indemnités non imposables)
```

### 5. CIMR ✅
```csharp
- CimrEmployeeRate      // ✅ OK (cimr_taux_salarial)
- CimrCompanyRate       // ✅ OK (cimr_taux_patronal)
- HasPrivateInsurance   // ⚠️  Mutuelle ?
- PrivateInsuranceRate  // ⚠️  mutuelle_salariale/patronale
- DisableAmo            // ℹ️  Info supplémentaire
```

### 6. Absences du Mois ⚠️
```csharp
- Absences              // ⚠️  À convertir en jours_travailles
```
**Problème** : Le DSL attend `jours_travailles`, pas une liste d'absences.

### 7. Heures Supplémentaires ⚠️
```csharp
- Overtimes             // ⚠️  À convertir en h_sup_25pct, h_sup_50pct, h_sup_100pct
```
**Problème** : Le DSL attend 3 champs séparés selon le taux de majoration.

### 8. Congés ✅
```csharp
- Leaves                // ⚠️  À convertir en jours_conge
```

### 9. Période ✅
```csharp
- PayMonth              // ✅ OK (mois_paie)
- PayYear               // ✅ OK (mois_paie)
```

---

## ❌ Données MANQUANTES dans `EmployeePayrollDto`

### 🔴 Critiques pour le Calcul

#### 1. **Jours travaillés** (MODULE[02])
```csharp
// Manquant :
public int JoursTravailles { get; set; }      // jours_travailles
public int JoursFeries { get; set; }          // jours_feries  
public int JoursConge { get; set; }           // jours_conge
```

#### 2. **Heures de référence** (MODULE[03])
```csharp
// Manquant :
public int HeuresMois { get; set; } = 191;    // heures_mois
```

#### 3. **Indemnités Non Imposables** (MODULE[04])
```csharp
// Manquant : 12 types d'indemnités NI
public decimal NiTransport { get; set; }
public decimal NiKilometrique { get; set; }
public decimal NiTournee { get; set; }
public decimal NiRepresentation { get; set; }
public decimal NiPanier { get; set; }
public decimal NiCaisse { get; set; }
public decimal NiSalissure { get; set; }
public decimal NiLait { get; set; }
public decimal NiOutillage { get; set; }
public decimal NiAideMedicale { get; set; }
public decimal NiGratifSociale { get; set; }
public decimal NiAutres { get; set; }
```

#### 4. **Mutuelle** (MODULE[06])
```csharp
// Manquant :
public decimal MutuelleSalariale { get; set; }
public decimal MutuellePatronale { get; set; }
```

#### 5. **Avances et Prêts** (MODULE[09], MODULE[11])
```csharp
// Manquant :
public decimal AvanceSalaire { get; set; }
public decimal InteretPretLogement { get; set; }
```

---

## 🎯 Recommandations

### Option 1 : **Enrichir `EmployeePayrollDto`** (Recommandé)

Ajouter tous les champs manquants pour être 100% conforme au DSL :

```csharp
public class EmployeePayrollDto
{
    // ========== Infos existantes ==========
    // ... (conserver tout ce qui existe)
    
    // ========== AJOUTS NÉCESSAIRES ==========
    
    // Présence (MODULE[02])
    public int JoursTravailles { get; set; } = 26;
    public int JoursFeries { get; set; } = 0;
    public int JoursConge { get; set; } = 0;
    
    // Heures (MODULE[03])
    public int HeuresMois { get; set; } = 191;
    
    // Heures Sup détaillées (MODULE[03])
    public decimal HeureSup25Pct { get; set; } = 0;
    public decimal HeureSup50Pct { get; set; } = 0;
    public decimal HeureSup100Pct { get; set; } = 0;
    
    // Indemnités Non Imposables (MODULE[04])
    public decimal NiTransport { get; set; } = 0;
    public decimal NiKilometrique { get; set; } = 0;
    public decimal NiTournee { get; set; } = 0;
    public decimal NiRepresentation { get; set; } = 0;
    public decimal NiPanier { get; set; } = 0;
    public decimal NiCaisse { get; set; } = 0;
    public decimal NiSalissure { get; set; } = 0;
    public decimal NiLait { get; set; } = 0;
    public decimal NiOutillage { get; set; } = 0;
    public decimal NiAideMedicale { get; set; } = 0;
    public decimal NiGratifSociale { get; set; } = 0;
    public decimal NiAutres { get; set; } = 0;
    
    // Mutuelle (MODULE[06])
    public decimal MutuelleSalariale { get; set; } = 0;
    public decimal MutuellePatronale { get; set; } = 0;
    
    // Avances et prêts (MODULE[09], MODULE[11])
    public decimal AvanceSalaire { get; set; } = 0;
    public decimal InteretPretLogement { get; set; } = 0;
    
    // Régime CIMR (MODULE[07])
    public string RegimeCimr { get; set; } = "AUCUN"; // AL_KAMIL, AL_MOUNASSIB, AUCUN
}
```

### Option 2 : **Adaptateur dans le Service** (Plus simple à court terme)

Créer une méthode qui transforme les données actuelles en format DSL :

```csharp
private object BuildDslInput(EmployeePayrollDto data)
{
    // Calculer jours travaillés à partir des absences
    var joursAbsents = data.Absences?.Count(a => a.DurationType == "FullDay") ?? 0;
    var joursTravailles = 26 - joursAbsents;
    
    // Calculer heures sup par catégorie
    var hSup25 = data.Overtimes?.Where(o => o.RateMultiplier == 1.25m).Sum(o => o.DurationInHours) ?? 0;
    var hSup50 = data.Overtimes?.Where(o => o.RateMultiplier == 1.50m).Sum(o => o.DurationInHours) ?? 0;
    var hSup100 = data.Overtimes?.Where(o => o.RateMultiplier == 2.00m).Sum(o => o.DurationInHours) ?? 0;
    
    // Mapper PackageItems sur NI
    var niTransport = data.PackageItems?.FirstOrDefault(p => p.Label == "Transport")?.DefaultValue ?? 0;
    var niPanier = data.PackageItems?.FirstOrDefault(p => p.Label == "Panier")?.DefaultValue ?? 0;
    // ... etc
    
    return new
    {
        // Identité
        nom = data.FullName?.Split(' ').LastOrDefault(),
        prenom = data.FullName?.Split(' ').FirstOrDefault(),
        cin = data.CinNumber,
        cnss_numero = data.CnssNumber,
        fonction = data.JobPosition,
        contrat = data.ContractType,
        
        // Dates
        date_embauche = data.ContractStartDate,
        mois_paie = $"{data.PayYear}-{data.PayMonth:D2}",
        
        // Famille
        situation_fam = data.NumberOfChildren + (data.HasSpouse ? 1 : 0),
        
        // Salaire et présence
        salaire_base_26j = data.BaseSalary,
        jours_travailles = joursTravailles,
        jours_feries = 0,
        jours_conge = data.Leaves?.Sum(l => l.DaysCount) ?? 0,
        heures_mois = 191,
        
        // Heures sup
        h_sup_25pct = hSup25,
        h_sup_50pct = hSup50,
        h_sup_100pct = hSup100,
        
        // ========== PRIMES IMPOSABLES : AGRÉGATION INTELLIGENTE ==========
        // Problème : DSL limite à 3 primes, mais on peut en avoir beaucoup plus
        // Solution : Agréger toutes les primes (SalaryComponents + PackageItems taxables)
        //            Trier par montant décroissant, prendre top 2, agréger le reste dans slot 3
        // Documentation complète : Voir STRATEGIE_AGREGATION_PRIMES.md
        
        var primesImposables = new List<(string label, decimal montant)>();
        
        // Collecter SalaryComponents
        foreach (var comp in data.SalaryComponents ?? new())
            primesImposables.Add((comp.ComponentType, comp.Amount));
        
        // Collecter PackageItems taxables
        foreach (var item in (data.PackageItems ?? new()).Where(p => p.IsTaxable))
            primesImposables.Add((item.Label, item.DefaultValue));
        
        var primesTriees = primesImposables.OrderByDescending(p => p.montant).ToList();
        
        prime_imposable_1 = primesTriees.ElementAtOrDefault(0).montant,
        prime_imposable_2 = primesTriees.ElementAtOrDefault(1).montant,
        prime_imposable_3 = primesTriees.Skip(2).Sum(p => p.montant),  // Agrégation !
        
        // CIMR
        regime_cimr = DetermineRegimeCimr(data),
        cimr_taux_salarial = (data.CimrEmployeeRate ?? 0) / 100,
        cimr_taux_patronal = (data.CimrCompanyRate ?? 0) / 100,
        
        // Mutuelle
        mutuelle_salariale = data.HasPrivateInsurance ? (data.PrivateInsuranceRate ?? 0) * data.BaseSalary / 100 : 0,
        mutuelle_patronale = 0, // À déterminer
        
        // Indemnités NI (extraites des PackageItems)
        ni_transport = niTransport,
        ni_panier = niPanier,
        // ... autres NI
        
        // Avances
        avance_salaire = 0, // Non géré actuellement
        interet_pret_logement = 0 // Non géré actuellement
    };
}
```

---

## 📝 Format JSON à Envoyer au LLM

### Structure Finale

```json
{
  "employe": {
    "nom": "Alami",
    "prenom": "Mohammed",
    "matricule": "EMP-001",
    "cin": "AB123456",
    "cnss_numero": "123456789",
    "fonction": "Développeur Senior"
  },
  "contrat": {
    "type": "PP",
    "date_embauche": "2020-01-15",
    "anciennete_annees": 6
  },
  "periode": {
    "mois_paie": "2026-01",
    "jours_travailles": 24,
    "jours_feries": 1,
    "jours_conge": 1
  },
  "salaire": {
    "salaire_base_26j": 12000.00,
    "heures_mois": 191,
    "h_sup_25pct": 8,
    "h_sup_50pct": 4,
    "h_sup_100pct": 0,
    "prime_imposable_1": 500,
    "prime_imposable_2": 0,
    "prime_imposable_3": 0
  },
  "cimr": {
    "regime": "AL_KAMIL",
    "taux_salarial": 0.04,
    "taux_patronal": 0.06
  },
  "mutuelle": {
    "salariale": 150,
    "patronale": 200
  },
  "indemnites_ni": {
    "transport": 400,
    "kilometrique": 0,
    "tournee": 0,
    "representation": 1000,
    "panier": 684,
    "caisse": 0,
    "salissure": 0,
    "lait": 0,
    "outillage": 0,
    "aide_medicale": 0,
    "gratif_sociale": 0,
    "autres": 0
  },
  "deductions": {
    "avance_salaire": 0,
    "interet_pret_logement": 0
  },
  "famille": {
    "situation_fam": 3,
    "detail": {
      "conjoint": 1,
      "enfants": 2
    }
  }
}
```

---

## 🚀 Plan d'Action Recommandé

### Court Terme (Tests)
1. ✅ **Utiliser l'Option 2** (Adaptateur)
2. ✅ Mapper les données actuelles vers le format DSL
3. ✅ Tester avec MockClaude (gratuit)
4. ✅ Valider les résultats avec les CHECKPOINTS du DSL

### Moyen Terme (Production)
1. 📋 **Enrichir `EmployeePayrollDto`** avec tous les champs manquants
2. 📋 Créer des tables pour les Indemnités NI dans la DB
3. 📋 Interface admin pour saisir les NI par employé
4. 📋 Gérer les avances de salaire et prêts logement

### Long Terme (Optimisation)
1. 🎯 Parser la réponse Claude pour validation
2. 🎯 Auto-vérification avec les CHECKPOINTS
3. 🎯 Audit trail pour traçabilité
4. 🎯 Alertes si divergence > 0.05 MAD

---

## ✅ Conclusion

Le fichier DSL est **excellent** et très détaillé. Pour le faire fonctionner :

1. **Données minimales** : Nom, salaire de base, ancienneté, jours travaillés, famille
2. **Données importantes** : Heures sup, CIMR, mutuelle, indemnités NI
3. **Données optionnelles** : Avances, prêts, contrats ANAPEC

**Prochaine étape** : Implémenter l'adaptateur (Option 2) pour mapper `EmployeePayrollDto` vers le format DSL attendu par Claude.
