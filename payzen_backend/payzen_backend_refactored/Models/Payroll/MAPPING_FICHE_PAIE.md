# 📊 Mapping Complet : Fiche de Paie Marocaine → Base de Données

Ce document détaille la correspondance entre les champs de la fiche de paie marocaine standard et les colonnes de la table `PayrollResults`.

## 🔍 Vue d'ensemble

La table `PayrollResults` stocke maintenant **TOUS** les éléments d'une fiche de paie marocaine :
- ✅ **Gains** : Salaire de base, heures supplémentaires, primes, ancienneté
- ✅ **Retenues** : CNSS, AMO, CIMR, Mutuelle, IR
- ✅ **Indemnités non imposables** : Transport, panier, déplacement, etc.
- ✅ **Charges patronales** : Contributions employeur
- ✅ **Totaux** : Brut, net imposable, net à payer

---

## 📋 Mapping détaillé

### 1️⃣ IDENTIFICATION

| **Fiche de Paie** | **Colonne DB** | **Type** | **Description** |
|-------------------|----------------|----------|-----------------|
| NOM ET PRENOM | `Employee.FullName` | Navigation | Via `EmployeeId` |
| MATRICULE | `Employee.EmployeeNumber` | Navigation | Via `EmployeeId` |
| FONCTION | `Employee.Position` | Navigation | Via `EmployeeId` |
| MOIS | `Month` + `Year` | int | Période de paie (ex: 1/2026) |
| DATE EMBAUCHE | `Employee.HireDate` | Navigation | Via `EmployeeId` |
| ANCIENNETE | Calculé | - | `Year - HireDate.Year` |

---

### 2️⃣ SALAIRE DE BASE ET HEURES

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Exemple** |
|-------------------|----------------|----------|--------------|-------------|
| Salaire de base | `SalaireBase` | decimal? | `salaire_base_mensuel` | 9000.00 |
| Heures supplémentaires 25% | `HeuresSupp25` | decimal? | `hs_25_montant` | 0.00 |
| Heures supplémentaires 50% | `HeuresSupp50` | decimal? | `hs_50_montant` | 0.00 |
| Heures supplémentaires 100% | `HeuresSupp100` | decimal? | `hs_100_montant` | 0.00 |
| Congés | `Conges` | decimal? | `conges_montant` | 0.00 |
| Jours fériés | `JoursFeries` | decimal? | `jours_feries_montant` | 0.00 |
| Ancienneté | `PrimeAnciennete` | decimal? | `prime_anciennete` | 900.00 |

---

### 3️⃣ PRIMES IMPOSABLES

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Exemple** |
|-------------------|----------------|----------|--------------|-------------|
| Prime imposable 1 | `PrimeImposable1` | decimal? | `primes_imposables[0].montant` | 500.00 |
| Prime imposable 2 | `PrimeImposable2` | decimal? | `primes_imposables[1].montant` | 1000.00 |
| Prime imposable 3 | `PrimeImposable3` | decimal? | `primes_imposables[2].montant` | 0.00 |
| **Total primes** | `TotalPrimesImposables` | decimal? | `total_primes_imposables` | 1500.00 |

💡 **Note** : Le système supporte un nombre illimité de primes via l'array `primes_imposables` du DSL v3.1, mais seules les 3 premières sont stockées dans des colonnes fixes pour compatibilité avec les anciennes fiches de paie.

---

### 4️⃣ SALAIRE BRUT

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Exemple** |
|-------------------|----------------|----------|--------------|-------------|
| Salaire brut imposable | `TotalBrut` | decimal? | `salaire_brut_imposable` | 11400.00 |
| Salaire brut imposable | `BrutImposable` | decimal? | `salaire_brut_imposable` | 11400.00 |

💡 **Note** : `TotalBrut` et `BrutImposable` contiennent la même valeur (duplication pour compatibilité).

---

### 5️⃣ FRAIS PROFESSIONNELS

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Calcul** | **Exemple** |
|-------------------|----------------|----------|--------------|------------|-------------|
| Frais professionnels | `FraisProfessionnels` | decimal? | `montant_fp` | `MIN(brut × 25%, 2500)` | 2850.00 |

---

### 6️⃣ INDEMNITES NON IMPOSABLES

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Exemple** |
|-------------------|----------------|----------|--------------|-------------|
| Indemnité de représentation | `IndemniteRepresentation` | decimal? | `indemnite_representation` | 0.00 |
| Prime de transport | `PrimeTransport` | decimal? | `prime_transport` | 0.00 |
| Prime de panier | `PrimePanier` | decimal? | `prime_panier` | 0.00 |
| Indemnité de déplacement | `IndemniteDeplacement` | decimal? | `indemnite_deplacement` | 0.00 |
| Indemnité de caisse | `IndemniteCaisse` | decimal? | `indemnite_caisse` | 0.00 |
| Prime de salissure | `PrimeSalissure` | decimal? | `prime_salissure` | 0.00 |
| Gratifications familial | `GratificationsFamilial` | decimal? | `gratifications_familial` | 0.00 |
| Prime de voyage Mecque | `PrimeVoyageMecque` | decimal? | `prime_voyage_mecque` | 0.00 |
| Indemnité de licenciement | `IndemniteLicenciement` | decimal? | `indemnite_licenciement` | 0.00 |
| Indemnité kilométrique | `IndemniteKilometrique` | decimal? | `indemnite_kilometrique` | 0.00 |
| Prime de Tourné | `PrimeTourne` | decimal? | `prime_tourne` | 0.00 |
| Prime d'outillage | `PrimeOutillage` | decimal? | `prime_outillage` | 0.00 |
| Aide médicale | `AideMedicale` | decimal? | `aide_medicale` | 0.00 |
| Autres primes non imposable | `AutresPrimesNonImposable` | decimal? | `autres_primes_non_imposable` | 0.00 |
| **Total indemnités** | `TotalIndemnites` | decimal? | `total_ni_exonere` | 0.00 |

---

### 7️⃣ COTISATIONS SALARIALES (Retenues)

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Taux** | **Exemple** |
|-------------------|----------------|----------|--------------|----------|-------------|
| C.N.S.S P.S | `CnssPartSalariale` | decimal? | `cnss_rg_salarial` | 4.48% | 268.80 |
| A.M.O P.S | `AmoPartSalariale` | decimal? | `cnss_amo_salarial` | 2.26% | 257.64 |
| C.I.M.R P.S | `CimrPartSalariale` | decimal? | `cimr_salarial` | Variable | 0.00 |
| MUTUELLE P.S | `MutuellePartSalariale` | decimal? | `mutuelle_salariale` | Variable | 0.00 |
| **Total cotisations** | `TotalCotisationsSalariales` | decimal? | `total_cnss_salarial` | - | 526.44 |

---

### 8️⃣ COTISATIONS PATRONALES (Charges employeur)

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Taux** | **Exemple** |
|-------------------|----------------|----------|--------------|----------|-------------|
| CNSS PART PATRONALE | `CnssPartPatronale` | decimal? | `cnss_rg_patronal` | Variable | 1450.80 |
| AMO PART PATRONALE | `AmoPartPatronale` | decimal? | `cnss_amo_patronal` | Variable | 468.54 |
| CIMR PART PATRONALE | `CimrPartPatronale` | decimal? | `cimr_patronal` | Variable | 0.00 |
| MUTUELLE P PATRONALE | `MutuellePartPatronale` | decimal? | `mutuelle_patronale` | Variable | 0.00 |
| **Total charges** | `TotalCotisationsPatronales` | decimal? | `total_charges_patronales` | - | 1919.34 |

---

### 9️⃣ IMPOT SUR LE REVENU

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Exemple** |
|-------------------|----------------|----------|--------------|-------------|
| IR | `ImpotRevenu` | decimal? | `ir_final` | 907.07 |

---

### 🔟 ARRONDI

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Calcul** | **Exemple** |
|-------------------|----------------|----------|--------------|------------|-------------|
| ARRONDIES | `Arrondi` | decimal? | `arrondi_net` | `CEIL(net) - net` | 0.51 |

💡 **Arrondi comptable** : Le salaire net est toujours arrondi à l'unité supérieure (CEIL). L'écart est stocké dans `arrondi_net`.

**Exemple** :
- Salaire net avant arrondi : 9966.49 MAD
- Salaire net après arrondi : **9967.00 MAD**
- Arrondi : **0.51 MAD**

---

### 1️⃣1️⃣ AVANCES ET DIVERS

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Exemple** |
|-------------------|----------------|----------|--------------|-------------|
| AVANCE SUR SALAIRE | `AvanceSurSalaire` | decimal? | `avance_salaire` | 0.00 |
| INTERET SUR LOGEMENT | `InteretSurLogement` | decimal? | `interet_logement` | 0.00 |

---

### 1️⃣2️⃣ TOTAUX FINAUX

| **Fiche de Paie** | **Colonne DB** | **Type** | **JSON LLM** | **Exemple** |
|-------------------|----------------|----------|--------------|-------------|
| BRUT IMPOSABLE | `BrutImposable` | decimal? | `salaire_brut_imposable` | 11400.00 |
| NET IMPOSABLE | `NetImposable` | decimal? | `revenu_net_imposable` | 8023.56 |
| TOTAL GAINS | `TotalGains` | decimal? | `salaire_brut_imposable` | 11400.00 |
| TOTAL RETENUES | `TotalRetenues` | decimal? | `total_retenues_salariales` | 1433.51 |
| **NET A PAYER** | `NetAPayer` | decimal? | `salaire_net` | **9967.00** |

💡 **Champs de compatibilité** :
- `TotalNet` : Net avant arrondi (`salaire_net_avant_arrondi`)
- `TotalNet2` : Net après arrondi (`salaire_net`)

---

## 🔄 Extraction automatique

Le service `PaieService.cs` extrait automatiquement **TOUS** ces champs du JSON retourné par le LLM (Claude/Gemini) et les stocke dans la base de données.

### ✅ Avantages :

1. **Performance** : Requêtes SQL directes sans parser le JSON
2. **Reporting** : Agrégations faciles (SUM, AVG, GROUP BY)
3. **Compatibilité** : Le `ResultatJson` complet reste disponible
4. **Robustesse** : Fallback intelligent si un champ manque

### 📝 Exemple de code d'extraction :

```csharp
// Helper pour extraire un champ décimal
static decimal? GetDecimal(JsonElement root, string name)
{
    if (!root.TryGetProperty(name, out var prop)) return null;
    try { return prop.GetDecimal(); }
    catch { return null; }
}

// Helper pour extraire les primes depuis un array
static decimal? GetPrimeFromArray(JsonElement root, string arrayName, int index)
{
    if (!root.TryGetProperty(arrayName, out var arr)) return null;
    if (index >= arr.GetArrayLength()) return null;
    
    var item = arr[index];
    return item.TryGetProperty("montant", out var montant) 
        ? montant.GetDecimal() 
        : null;
}

// Extraction complète
SalaireBase = GetDecimal(resultatParse, "salaire_base_mensuel"),
PrimeImposable1 = GetPrimeFromArray(resultatParse, "primes_imposables", 0),
NetAPayer = GetDecimal(resultatParse, "salaire_net"),
// ... etc.
```

---

## 📊 Exemples de requêtes SQL simplifiées

Grâce à l'extraction automatique, les requêtes deviennent très simples :

### 1️⃣ Total net payé par mois
```sql
SELECT 
    [Year], 
    [Month], 
    SUM([NetAPayer]) AS TotalNetPaye
FROM [PayrollResults]
WHERE [Status] = 2 -- OK
GROUP BY [Year], [Month]
ORDER BY [Year] DESC, [Month] DESC;
```

### 2️⃣ Masse salariale + charges patronales
```sql
SELECT 
    [Year], 
    [Month], 
    SUM([TotalBrut]) AS MasseSalariale,
    SUM([TotalCotisationsPatronales]) AS ChargesPatronales,
    SUM([TotalBrut] + [TotalCotisationsPatronales]) AS CoutTotal
FROM [PayrollResults]
WHERE [Status] = 2
GROUP BY [Year], [Month];
```

### 3️⃣ Répartition des retenues
```sql
SELECT 
    [EmployeeId],
    [CnssPartSalariale],
    [AmoPartSalariale],
    [CimrPartSalariale],
    [ImpotRevenu],
    [TotalRetenues]
FROM [PayrollResults]
WHERE [Year] = 2026 AND [Month] = 2 AND [Status] = 2;
```

### 4️⃣ Employés avec primes
```sql
SELECT 
    e.[FullName],
    pr.[PrimeImposable1],
    pr.[PrimeImposable2],
    pr.[PrimeImposable3],
    pr.[TotalPrimesImposables]
FROM [PayrollResults] pr
INNER JOIN [Employees] e ON pr.[EmployeeId] = e.[Id]
WHERE pr.[TotalPrimesImposables] > 0
AND pr.[Year] = 2026 AND pr.[Month] = 2;
```

---

## 🎯 Statut de traitement

| **Status** | **Code** | **Description** |
|------------|----------|-----------------|
| `Pending` | 0 | En attente de traitement |
| `Processing` | 1 | En cours de traitement |
| `OK` | 2 | ✅ Traité avec succès |
| `Error` | 3 | ❌ Erreur (voir `ErrorMessage`) |
| `ManualReviewRequired` | 4 | ⚠️ Revue manuelle nécessaire |

---

## 🔗 Références

- **Modèle de données** : `Models/Payroll/PayrollResult.cs`
- **Service d'extraction** : `Services/Payroll/PaieService.cs`
- **DSL de paie** : `rules/regles_paie_compact.txt`
- **Format JSON LLM** : Voir DSL v3.1 section OUTPUT

---

## 📅 Historique

| **Version** | **Date** | **Description** |
|-------------|----------|-----------------|
| v1.0 | 2026-02-24 | Modèle initial avec totaux uniquement |
| v2.0 | 2026-02-25 | ⭐ Ajout de TOUS les champs de la fiche de paie |

---

**🎉 Ce mapping permet une extraction automatique de 60+ champs depuis le JSON LLM vers la base de données !**
