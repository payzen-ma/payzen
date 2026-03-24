# 🎯 Gestion Dynamique des Primes Imposables

## 📊 Problématique

La fiche de paie marocaine standard affiche 3 lignes de "Prime imposable", mais un employé peut avoir **plus de 3 primes** selon son package salarial.

**Exemple** : Un employé peut avoir :
- Prime d'excellence : 1000 MAD
- Prime de commission : 500 MAD
- Prime de performance : 800 MAD
- Prime de transport (imposable) : 400 MAD
- Prime de fidélité : 300 MAD

---

## ✅ Solution : Table de relation `PayrollResultPrimes`

Au lieu de colonnes fixes (`PrimeImposable1`, `PrimeImposable2`, `PrimeImposable3`), nous avons créé une **table séparée** qui stocke un **nombre illimité** de primes par fiche de paie.

### 📋 Structure de la table

```sql
CREATE TABLE [PayrollResultPrimes] (
    [Id] int NOT NULL IDENTITY,
    [PayrollResultId] int NOT NULL,          -- Clé étrangère vers PayrollResults
    [Label] nvarchar(max) NOT NULL,          -- "Prime d'excellence", "Prime de commission", etc.
    [Montant] decimal(18,2) NOT NULL,        -- Montant de la prime
    [Ordre] int NOT NULL,                    -- Ordre d'affichage (1, 2, 3, ...)
    [IsTaxable] bit NOT NULL,                -- true=imposable, false=indemnité non imposable
    CONSTRAINT [PK_PayrollResultPrimes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollResultPrimes_PayrollResults] FOREIGN KEY ([PayrollResultId]) 
        REFERENCES [PayrollResults] ([Id]) ON DELETE CASCADE
);
```

### 🔗 Relation

```
PayrollResult (1) ----< (N) PayrollResultPrime
```

Un `PayrollResult` peut avoir **plusieurs** `PayrollResultPrime` (relation 1-N).

---

## 🔧 Extraction automatique depuis le JSON LLM

Le service `PaieService.cs` extrait **automatiquement toutes les primes** depuis le JSON retourné par le LLM :

### 📝 Code d'extraction

```csharp
// Helper pour extraire TOUTES les primes depuis un array JSON
static List<PayrollResultPrime> ExtractAllPrimes(JsonElement root, string arrayName, bool isTaxable)
{
    var primes = new List<PayrollResultPrime>();
    
    if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        return primes;
    
    int ordre = 1;
    foreach (var item in arr.EnumerateArray())
    {
        string? label = null;
        decimal montant = 0;
        
        if (item.TryGetProperty("label", out var labelProp))
            label = labelProp.GetString();
        
        if (item.TryGetProperty("montant", out var montantProp))
        {
            try { montant = montantProp.GetDecimal(); }
            catch { continue; }
        }
        
        if (!string.IsNullOrEmpty(label) && montant > 0)
        {
            primes.Add(new PayrollResultPrime
            {
                Label = label,
                Montant = montant,
                Ordre = ordre++,
                IsTaxable = isTaxable
            });
        }
    }
    
    return primes;
}

// Utilisation
var primesImposables = ExtractAllPrimes(resultatParse, "primes_imposables", isTaxable: true);
var indemnites = ExtractAllPrimes(resultatParse, "indemnites_non_imposables", isTaxable: false);

foreach (var prime in primesImposables.Concat(indemnites))
{
    payrollResult.Primes.Add(prime);
}
```

### 📊 JSON source (DSL v3.1)

```json
{
  "primes_imposables": [
    { "label": "Prime d'excellence", "montant": 1000.00 },
    { "label": "Prime de commission", "montant": 500.00 },
    { "label": "Prime de performance", "montant": 800.00 },
    { "label": "Prime de transport", "montant": 400.00 },
    { "label": "Prime de fidélité", "montant": 300.00 }
  ],
  "total_primes_imposables": 3000.00,
  
  "indemnites_non_imposables": [
    { "label": "Prime de panier", "montant": 200.00 },
    { "label": "Indemnité de déplacement", "montant": 150.00 }
  ]
}
```

### 📦 Résultat dans la base de données

| Id | PayrollResultId | Label | Montant | Ordre | IsTaxable |
|----|-----------------|-------|---------|-------|-----------|
| 1  | 34              | Prime d'excellence | 1000.00 | 1 | true |
| 2  | 34              | Prime de commission | 500.00 | 2 | true |
| 3  | 34              | Prime de performance | 800.00 | 3 | true |
| 4  | 34              | Prime de transport | 400.00 | 4 | true |
| 5  | 34              | Prime de fidélité | 300.00 | 5 | true |
| 6  | 34              | Prime de panier | 200.00 | 1 | false |
| 7  | 34              | Indemnité de déplacement | 150.00 | 2 | false |

---

## 📊 Requêtes SQL

### 1️⃣ Récupérer toutes les primes d'une fiche de paie

```sql
SELECT 
    prp.[Label],
    prp.[Montant],
    prp.[IsTaxable],
    prp.[Ordre]
FROM [PayrollResultPrimes] prp
WHERE prp.[PayrollResultId] = 34
  AND prp.[IsTaxable] = 1  -- Seulement les primes imposables
ORDER BY prp.[Ordre];
```

### 2️⃣ Fiche de paie complète avec ses primes

```sql
SELECT 
    pr.[Id],
    pr.[EmployeeId],
    pr.[Month],
    pr.[Year],
    pr.[SalaireBase],
    pr.[TotalPrimesImposables],
    pr.[NetAPayer],
    
    -- Primes détaillées (sous-requête)
    (
        SELECT 
            prp.[Label] AS [label],
            prp.[Montant] AS [montant],
            prp.[Ordre] AS [ordre]
        FROM [PayrollResultPrimes] prp
        WHERE prp.[PayrollResultId] = pr.[Id]
          AND prp.[IsTaxable] = 1
        ORDER BY prp.[Ordre]
        FOR JSON PATH
    ) AS [PrimesImposables]
    
FROM [PayrollResults] pr
WHERE pr.[Id] = 34;
```

### 3️⃣ Top 10 des primes les plus fréquentes

```sql
SELECT TOP 10
    prp.[Label],
    COUNT(*) AS [Frequence],
    AVG(prp.[Montant]) AS [MontantMoyen],
    SUM(prp.[Montant]) AS [MontantTotal]
FROM [PayrollResultPrimes] prp
WHERE prp.[IsTaxable] = 1
GROUP BY prp.[Label]
ORDER BY [Frequence] DESC;
```

### 4️⃣ Employés ayant plus de 3 primes

```sql
SELECT 
    e.[FullName],
    pr.[Month],
    pr.[Year],
    COUNT(prp.[Id]) AS [NombrePrimes],
    SUM(prp.[Montant]) AS [TotalPrimes]
FROM [PayrollResults] pr
INNER JOIN [Employees] e ON pr.[EmployeeId] = e.[Id]
LEFT JOIN [PayrollResultPrimes] prp ON pr.[Id] = prp.[PayrollResultId] AND prp.[IsTaxable] = 1
WHERE pr.[Year] = 2026 AND pr.[Month] = 3
GROUP BY e.[FullName], pr.[Month], pr.[Year]
HAVING COUNT(prp.[Id]) > 3
ORDER BY [NombrePrimes] DESC;
```

---

## 🎯 Compatibilité avec les colonnes fixes

Pour assurer la **rétrocompatibilité**, nous conservons les 3 colonnes fixes (`PrimeImposable1/2/3`) dans `PayrollResults` :

### 📋 Mapping

| **Colonne fixe** | **Source** | **Utilité** |
|------------------|------------|-------------|
| `PrimeImposable1` | Première prime de l'array | Export rapide, reporting legacy |
| `PrimeImposable2` | Deuxième prime de l'array | Export rapide, reporting legacy |
| `PrimeImposable3` | Troisième prime de l'array | Export rapide, reporting legacy |
| `TotalPrimesImposables` | Somme de TOUTES les primes | Reporting, totaux |

### 🔍 Comportement

- ✅ **Si ≤ 3 primes** : Toutes sont dans les colonnes fixes + table `PayrollResultPrimes`
- ✅ **Si > 3 primes** : Les 3 premières dans les colonnes fixes, **TOUTES** dans `PayrollResultPrimes`

### 💡 Recommandation

**Utiliser la table `PayrollResultPrimes` pour :**
- Export détaillé des fiches de paie
- Génération PDF avec toutes les primes
- Analyse fine par type de prime

**Utiliser les colonnes fixes pour :**
- Reporting legacy qui attend 3 colonnes
- Agrégations rapides (déjà indexées)

---

## 🔄 Navigation EF Core

Dans votre code C#, vous pouvez facilement accéder aux primes :

```csharp
// Charger une fiche de paie avec ses primes
var payrollResult = await _db.PayrollResults
    .Include(pr => pr.Primes)  // ⭐ Inclure les primes
    .FirstOrDefaultAsync(pr => pr.Id == 34);

// Afficher toutes les primes imposables
var primesImposables = payrollResult.Primes
    .Where(p => p.IsTaxable)
    .OrderBy(p => p.Ordre)
    .ToList();

foreach (var prime in primesImposables)
{
    Console.WriteLine($"{prime.Ordre}. {prime.Label}: {prime.Montant:N2} MAD");
}

// Résultat :
// 1. Prime d'excellence: 1,000.00 MAD
// 2. Prime de commission: 500.00 MAD
// 3. Prime de performance: 800.00 MAD
// 4. Prime de transport: 400.00 MAD
// 5. Prime de fidélité: 300.00 MAD
```

---

## 📈 Avantages de cette approche

✅ **Évolutivité** : Support d'un nombre illimité de primes  
✅ **Flexibilité** : Chaque prime conserve son label original  
✅ **Performance** : Index sur `PayrollResultId` pour requêtes rapides  
✅ **Compatibilité** : Les colonnes fixes restent disponibles  
✅ **Traçabilité** : Ordre d'affichage préservé (`Ordre`)  
✅ **Distinction** : Flag `IsTaxable` pour différencier primes imposables/non imposables  

---

## 🧪 Test de l'extraction dynamique

### 1️⃣ Calcul d'une paie avec 5 primes

```http
POST /api/payroll/recalculate/single
{
  "employeeId": 3,
  "month": 3,
  "year": 2026
}
```

### 2️⃣ Vérification dans la console

Cherchez le log :
```
💰 Extraction dynamique : 5 primes imposables + 2 indemnités = 7 total
```

### 3️⃣ Vérification en base de données

```sql
-- Compter les primes extraites
SELECT 
    pr.[Id],
    pr.[EmployeeId],
    pr.[Month],
    pr.[Year],
    COUNT(prp.[Id]) AS [NombrePrimes],
    pr.[TotalPrimesImposables]
FROM [PayrollResults] pr
LEFT JOIN [PayrollResultPrimes] prp ON pr.[Id] = prp.[PayrollResultId] AND prp.[IsTaxable] = 1
WHERE pr.[Id] = 34
GROUP BY pr.[Id], pr.[EmployeeId], pr.[Month], pr.[Year], pr.[TotalPrimesImposables];
```

**Résultat attendu** :
```
Id: 34
EmployeeId: 3
Month: 3
Year: 2026
NombrePrimes: 5
TotalPrimesImposables: 3000.00
```

---

## 🎉 Résultat final

Vous pouvez maintenant :

✅ Stocker **un nombre illimité** de primes par fiche de paie  
✅ Conserver le **label exact** de chaque prime  
✅ **Filtrer** facilement par type (imposable/non imposable)  
✅ **Exporter** toutes les primes vers PDF/Excel  
✅ **Analyser** la distribution des primes par type  
✅ **Rester compatible** avec les systèmes qui attendent 3 colonnes fixes  

**La gestion dynamique des primes est maintenant opérationnelle ! 🚀**
