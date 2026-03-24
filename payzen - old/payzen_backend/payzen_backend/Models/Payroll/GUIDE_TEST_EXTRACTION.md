# 🧪 Guide de Test : Extraction Complète de la Fiche de Paie

Ce guide vous permet de tester que tous les 60+ champs sont correctement extraits et sauvegardés.

---

## 📋 Prérequis

✅ Migration appliquée : `20260225095211_AjoutChampsFichePaieComplete`  
✅ Base de données à jour avec les nouvelles colonnes  
✅ Service LLM configuré (Gemini ou Claude)  
✅ Employé de test avec package salarial

---

## 🚀 Test 1 : Calcul d'une paie unique

### 1️⃣ Appeler l'API

```http
POST https://localhost:7XXX/api/payroll/recalculate/single
Content-Type: application/json

{
  "employeeId": 3,
  "month": 3,
  "year": 2026
}
```

### 2️⃣ Vérifier la réponse

```json
{
  "message": "Recalcul terminé avec succès.",
  "employeeId": 3,
  "month": 3,
  "year": 2026,
  "status": "OK",
  "errorMessage": null,
  "resultId": 34
}
```

### 3️⃣ Vérifier dans la base de données

```sql
SELECT TOP 1 
    -- IDENTIFICATION
    [EmployeeId], [Month], [Year], [Status],
    
    -- SALAIRE DE BASE
    [SalaireBase], [PrimeAnciennete],
    [HeuresSupp25], [HeuresSupp50], [HeuresSupp100],
    [Conges], [JoursFeries],
    
    -- PRIMES IMPOSABLES
    [PrimeImposable1], [PrimeImposable2], [PrimeImposable3],
    [TotalPrimesImposables],
    
    -- SALAIRE BRUT
    [TotalBrut], [BrutImposable],
    
    -- FRAIS PROFESSIONNELS
    [FraisProfessionnels],
    
    -- INDEMNITES (quelques exemples)
    [PrimeTransport], [PrimePanier], [IndemniteRepresentation],
    [TotalIndemnites],
    
    -- COTISATIONS SALARIALES
    [CnssPartSalariale], [AmoPartSalariale], 
    [CimrPartSalariale], [MutuellePartSalariale],
    [TotalCotisationsSalariales],
    
    -- COTISATIONS PATRONALES
    [CnssPartPatronale], [AmoPartPatronale],
    [CimrPartPatronale], [MutuellePartPatronale],
    [TotalCotisationsPatronales],
    
    -- IMPOT ET ARRONDI
    [ImpotRevenu], [Arrondi],
    
    -- AVANCES
    [AvanceSurSalaire], [InteretSurLogement],
    
    -- TOTAUX FINAUX
    [NetImposable], [TotalGains], [TotalRetenues], [NetAPayer],
    
    -- COMPATIBILITE
    [TotalNet], [TotalNet2]
    
FROM [PayrollResults]
WHERE [EmployeeId] = 3 
  AND [Month] = 3 
  AND [Year] = 2026
ORDER BY [Id] DESC;
```

### 4️⃣ Vérifier les valeurs attendues

Pour l'employé de test avec :
- Salaire de base : 9000 MAD
- Ancienneté : 6 ans
- Prime 1 : 500 MAD
- Prime 2 : 1000 MAD

**Résultats attendus** :

| **Champ** | **Valeur attendue** | **✅** |
|-----------|---------------------|--------|
| `SalaireBase` | 9000.00 | |
| `PrimeAnciennete` | 900.00 | |
| `PrimeImposable1` | 500.00 | |
| `PrimeImposable2` | 1000.00 | |
| `PrimeImposable3` | 0.00 ou NULL | |
| `TotalPrimesImposables` | 1500.00 | |
| `TotalBrut` | 11400.00 | |
| `FraisProfessionnels` | 2850.00 | |
| `CnssPartSalariale` | ~268.80 | |
| `AmoPartSalariale` | ~257.64 | |
| `TotalCotisationsSalariales` | ~526.44 | |
| `ImpotRevenu` | ~907.07 | |
| `Arrondi` | > 0 et < 1 | |
| `NetAPayer` | ~9967.00 | |
| `TotalNet` | ~9966.49 (avant arrondi) | |
| `TotalNet2` | ~9967.00 (après arrondi) | |

---

## 🧪 Test 2 : Vérification des champs NULL

Certains champs peuvent être NULL si non applicables :

```sql
SELECT 
    [HeuresSupp25], [HeuresSupp50], [HeuresSupp100],
    [Conges], [JoursFeries],
    [PrimeTransport], [PrimePanier],
    [CimrPartSalariale], [MutuellePartSalariale],
    [AvanceSurSalaire], [InteretSurLogement]
FROM [PayrollResults]
WHERE [EmployeeId] = 3 
  AND [Month] = 3 
  AND [Year] = 2026;
```

**Comportement attendu** :
- ✅ Les champs non présents dans le JSON LLM doivent être NULL
- ✅ Les champs avec valeur 0.00 dans le JSON doivent être 0.00
- ✅ Aucun champ ne doit contenir de valeur aberrante (négatif, > 1M)

---

## 🧪 Test 3 : Vérification des totaux

Vérifier la cohérence des totaux :

```sql
SELECT 
    [TotalBrut],
    [TotalCotisationsSalariales],
    [ImpotRevenu],
    [TotalRetenues],
    [NetAPayer],
    
    -- Vérification manuelle
    ([TotalBrut] - [TotalRetenues]) AS NetCalcule,
    ([NetAPayer] - ([TotalBrut] - [TotalRetenues])) AS Ecart
    
FROM [PayrollResults]
WHERE [EmployeeId] = 3 
  AND [Month] = 3 
  AND [Year] = 2026;
```

**Cohérence attendue** :
- ✅ `NetAPayer` ≈ `TotalBrut - TotalRetenues`
- ✅ Écart minimal dû à l'arrondi comptable (< 1 MAD)

---

## 🧪 Test 4 : Extraction des primes dynamiques

Vérifier que les primes sont correctement extraites depuis l'array JSON :

```sql
SELECT 
    [PrimeImposable1], 
    [PrimeImposable2], 
    [PrimeImposable3],
    [TotalPrimesImposables],
    
    -- Vérification
    ([PrimeImposable1] + [PrimeImposable2] + [PrimeImposable3]) AS SommePrimes,
    ([TotalPrimesImposables] - ([PrimeImposable1] + [PrimeImposable2] + [PrimeImposable3])) AS EcartPrimes
    
FROM [PayrollResults]
WHERE [EmployeeId] = 3 
  AND [Month] = 3 
  AND [Year] = 2026;
```

**Comportement attendu** :
- ✅ Si 3 primes ou moins : `SommePrimes` = `TotalPrimesImposables`
- ✅ Si > 3 primes : `SommePrimes` < `TotalPrimesImposables` (les primes 4, 5, ... ne sont pas dans les colonnes fixes)

---

## 🧪 Test 5 : Vérification de l'arrondi comptable

```sql
SELECT 
    [TotalNet] AS NetAvantArrondi,
    [NetAPayer] AS NetApresArrondi,
    [Arrondi],
    
    -- Vérification
    CEILING([TotalNet]) AS ArrondiCalcule,
    (CEILING([TotalNet]) - [TotalNet]) AS ArrondiAttendu,
    ([Arrondi] - (CEILING([TotalNet]) - [TotalNet])) AS EcartArrondi
    
FROM [PayrollResults]
WHERE [EmployeeId] = 3 
  AND [Month] = 3 
  AND [Year] = 2026;
```

**Formule attendue** :
- ✅ `NetAPayer` = `CEIL(TotalNet)`
- ✅ `Arrondi` = `NetAPayer - TotalNet`
- ✅ `0 <= Arrondi < 1`

**Exemple** :
- `TotalNet` = 9966.49
- `NetAPayer` = 9967.00
- `Arrondi` = 0.51 ✅

---

## 🧪 Test 6 : Vérification des logs d'extraction

Vérifier les logs du service pour voir le détail de l'extraction :

```bash
# Dans la console du serveur, chercher :
"📊 Analyse des primes :"
"📋 SalaryComponents (salaire de base)"
"✅ Primes imposables (PackageItems avec IsTaxable=true)"
"💰 Total primes imposables envoyées au LLM"
```

**Logs attendus** :
```
📊 Analyse des primes :
   📋 SalaryComponents (salaire de base) : 2 - NON envoyés comme primes
   ✅ Primes imposables (PackageItems avec IsTaxable=true) : 2
      - Prime d'excellence : 1000,00 MAD
      - Prime de commission : 500,00 MAD
💰 Total primes imposables envoyées au LLM : 2
```

---

## 🧪 Test 7 : Rapport SQL complet

Générer un rapport complet pour vérifier tous les champs :

```sql
SELECT 
    -- IDENTIFICATION
    e.[FullName] AS Employe,
    pr.[Month], pr.[Year], pr.[Status],
    
    -- GAINS
    pr.[SalaireBase],
    pr.[PrimeAnciennete],
    pr.[PrimeImposable1],
    pr.[PrimeImposable2],
    pr.[TotalPrimesImposables],
    pr.[TotalBrut],
    
    -- RETENUES
    pr.[CnssPartSalariale],
    pr.[AmoPartSalariale],
    pr.[TotalCotisationsSalariales],
    pr.[ImpotRevenu],
    pr.[TotalRetenues],
    
    -- NET
    pr.[Arrondi],
    pr.[NetAPayer],
    
    -- CHARGES EMPLOYEUR
    pr.[CnssPartPatronale],
    pr.[AmoPartPatronale],
    pr.[TotalCotisationsPatronales],
    
    -- COUT TOTAL
    (pr.[TotalBrut] + pr.[TotalCotisationsPatronales]) AS CoutTotalEmployeur
    
FROM [PayrollResults] pr
INNER JOIN [Employees] e ON pr.[EmployeeId] = e.[Id]
WHERE pr.[Year] = 2026 
  AND pr.[Month] = 3
  AND pr.[Status] = 2
ORDER BY e.[FullName];
```

---

## ✅ Checklist de validation

Après avoir exécuté tous les tests :

- [ ] Tous les champs de salaire de base sont remplis (SalaireBase, PrimeAnciennete)
- [ ] Les 3 premières primes sont extraites correctement
- [ ] Le total des primes correspond à la somme
- [ ] Le salaire brut est correct (base + primes + ancienneté)
- [ ] Les frais professionnels sont calculés (25% du brut, max 2500)
- [ ] Les cotisations salariales sont présentes (CNSS, AMO)
- [ ] Les cotisations patronales sont présentes
- [ ] L'impôt sur le revenu est calculé
- [ ] L'arrondi comptable est correct (0 <= arrondi < 1)
- [ ] Le net à payer est cohérent (brut - retenues + arrondi)
- [ ] Les indemnités non imposables sont NULL si non applicables
- [ ] Le champ `ResultatJson` contient le JSON complet
- [ ] Aucune valeur aberrante (négatif, > 1M)

---

## 🐛 En cas de problème

### Problème : Certains champs sont NULL alors qu'ils devraient avoir une valeur

**Causes possibles** :
1. Le LLM n'a pas retourné ce champ dans le JSON
2. Le nom du champ JSON ne correspond pas au mapping

**Solution** :
1. Vérifier le `ResultatJson` dans la base de données
2. Comparer avec le mapping dans `MAPPING_FICHE_PAIE.md`
3. Ajuster le mapping dans `PaieService.cs` si nécessaire

### Problème : Les primes ne sont pas extraites

**Causes possibles** :
1. L'array `primes_imposables` n'existe pas dans le JSON
2. La structure du JSON a changé

**Solution** :
1. Vérifier que le DSL v3.1 est utilisé (`regles_paie_compact.txt`)
2. Vérifier les logs d'extraction des primes
3. Vérifier le helper `GetPrimeFromArray` dans `PaieService.cs`

### Problème : L'arrondi est incorrect

**Causes possibles** :
1. Le LLM n'a pas appliqué l'arrondi comptable
2. Le champ `arrondi_net` n'est pas dans le JSON

**Solution** :
1. Vérifier que le prompt du LLM inclut les instructions d'arrondi
2. Vérifier les logs de Gemini/Claude pour les erreurs
3. Comparer avec le prompt dans `ClaudeService.cs` ou `GeminiService.cs`

---

## 📊 Exemple de résultat attendu (SQL)

```
EmployeeId: 3
Month: 3
Year: 2026
Status: OK

SalaireBase: 9000.00
PrimeAnciennete: 900.00
PrimeImposable1: 500.00
PrimeImposable2: 1000.00
PrimeImposable3: NULL
TotalPrimesImposables: 1500.00
TotalBrut: 11400.00
FraisProfessionnels: 2850.00

CnssPartSalariale: 268.80
AmoPartSalariale: 257.64
TotalCotisationsSalariales: 526.44
ImpotRevenu: 907.07
TotalRetenues: 1433.51

Arrondi: 0.51
NetAPayer: 9967.00
TotalNet: 9966.49
TotalNet2: 9967.00

CnssPartPatronale: 1450.80
AmoPartPatronale: 468.54
TotalCotisationsPatronales: 1919.34
```

---

## 🎉 Test réussi !

Si tous les champs sont correctement remplis, félicitations ! 🎊

Vous pouvez maintenant :
- Générer des rapports de masse salariale
- Exporter les fiches de paie vers Excel
- Créer des tableaux de bord en temps réel
- Analyser les données de paie sans parser le JSON

**La sauvegarde complète de la fiche de paie est opérationnelle ! 🚀**
