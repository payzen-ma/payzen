# 💡 Stratégie d'Agrégation des Primes Imposables

## 🎯 Problématique

Le **DSL de paie** limite le nombre de primes imposables à **3** :
- `prime_imposable_1`
- `prime_imposable_2`  
- `prime_imposable_3`

**MAIS** dans la réalité, un employé peut avoir **beaucoup plus** de primes :
- Prime d'ancienneté
- Prime de rendement
- Prime de fonction
- Prime d'objectif
- Prime exceptionnelle
- Bonus annuel proratisé
- Commission sur ventes
- Prime d'astreinte
- Prime de zone
- ... et bien d'autres

---

## ✅ Solution Implémentée : Agrégation Intelligente

### Stratégie Adoptée

```csharp
// 1. Collecter TOUTES les primes imposables
var primesImposables = new List<(string label, decimal montant)>();

// Sources :
// - SalaryComponents (toujours imposables)
// - PackageItems avec IsTaxable = true

// 2. Trier par montant DÉCROISSANT
var primesTriees = primesImposables
    .OrderByDescending(p => p.montant)
    .ToList();

// 3. Mapper sur les 3 slots du DSL
prime_imposable_1 = primesTriees[0].montant         // La plus grande
prime_imposable_2 = primesTriees[1].montant         // La 2ème plus grande
prime_imposable_3 = primesTriees.Skip(2).Sum(...)   // TOUTES les autres agrégées
```

### Pourquoi Cette Stratégie ?

✅ **Avantages :**
1. **Aucune perte de donnée** : Toutes les primes sont incluses dans le calcul
2. **Conformité DSL** : On reste dans les 3 slots imposés
3. **Traçabilité** : Le champ `_metadata.detail_primes` conserve le détail
4. **Flexibilité** : Fonctionne avec 1, 5, 10, ou 50 primes

⚠️ **Inconvénient :**
- On perd la granularité des libellés pour les primes agrégées (mais pas les montants)

---

## 📊 Exemples Concrets

### Exemple 1 : 2 Primes

```csharp
Input:
- Prime ancienneté : 500 MAD
- Prime rendement : 800 MAD

Output DSL:
{
  "prime_imposable_1": 800,    // La plus grande
  "prime_imposable_2": 500,
  "prime_imposable_3": 0
}
```

### Exemple 2 : 5 Primes

```csharp
Input:
- Prime ancienneté : 500 MAD
- Prime rendement : 1200 MAD
- Prime fonction : 800 MAD
- Commission : 300 MAD
- Prime astreinte : 200 MAD

Output DSL:
{
  "prime_imposable_1": 1200,   // Rendement
  "prime_imposable_2": 800,    // Fonction
  "prime_imposable_3": 1000,   // = 500 + 300 + 200 (agrégé)
  
  "_metadata": {
    "total_primes_agregees": 5,
    "detail_primes": [
      { "label": "Prime rendement", "montant": 1200 },
      { "label": "Prime fonction", "montant": 800 },
      { "label": "Prime ancienneté", "montant": 500 },
      { "label": "Commission", "montant": 300 },
      { "label": "Prime astreinte", "montant": 200 }
    ]
  }
}
```

### Exemple 3 : 10 Primes

```csharp
Input:
- 10 primes de montants variés

Output DSL:
{
  "prime_imposable_1": <la plus grande>,
  "prime_imposable_2": <la 2ème>,
  "prime_imposable_3": <somme des 8 restantes>,  // Agrégation !
}
```

---

## 🔍 Impact sur le Calcul de Paie

### ✅ Impact NULS

Le calcul de paie n'est **PAS affecté** par l'agrégation car :

1. **Le MODULE[05] du DSL** calcule le brut imposable comme :
   ```
   salaire_brut_imposable = salaire_base_mensuel
                          + prime_anciennete
                          + total_hsupp
                          + prime_imposable_1
                          + prime_imposable_2
                          + prime_imposable_3   <--- SOMME
                          + total_ni_excedent_imposable
   ```

2. **Seule la SOMME compte** pour le calcul :
   - CNSS, AMO, CIMR : calculés sur le brut imposable TOTAL
   - Frais professionnels : calculés sur le brut imposable TOTAL
   - IR : calculé sur le revenu net imposable dérivé du brut TOTAL

3. **Les libellés n'influencent PAS** le calcul mathématique

### Validation

```
Somme (sans agrégation) = 500 + 1200 + 800 + 300 + 200 = 3000 MAD
Somme (avec agrégation) = 1200 + 800 + 1000 = 3000 MAD ✅

Brut imposable = IDENTIQUE dans les deux cas
IR final = IDENTIQUE dans les deux cas
Net à payer = IDENTIQUE dans les deux cas
```

---

## 📋 Alternatives Évaluées

### Option A : Prendre seulement les 3 premières ❌

```csharp
prime_1 = primes[0]
prime_2 = primes[1]
prime_3 = primes[2]
// primes[3], [4], [5]... = PERDUES !
```

**Problème :** Perte de données, calcul incorrect

### Option B : Créer des catégories fixes ❌

```csharp
prime_1 = "Primes de fonction"  (somme)
prime_2 = "Primes de performance" (somme)
prime_3 = "Autres primes" (somme)
```

**Problème :** Difficile à maintenir, catégorisation arbitraire

### Option C : Agréger par ordre décroissant ✅ (CHOISI)

```csharp
prime_1 = La plus grande
prime_2 = La 2ème
prime_3 = TOUTES les autres
```

**Avantage :** Simple, aucune perte, traçable

---

## 🎯 Recommandations pour l'Avenir

### Court terme ✅
- Utiliser l'agrégation intelligente actuelle
- Logger le détail dans `_metadata` pour audit

### Moyen terme
- Ajouter un champ `detail_primes_json` dans `PayrollResult`
- Stocker le JSON détaillé des primes pour traçabilité complète

### Long terme
- **Proposer une évolution du DSL** :
  ```
  prime_imposables : List<{label, montant}>  // Au lieu de 3 champs fixes
  ```
- Cela permettrait de passer TOUTES les primes sans agrégation

---

## 🧪 Tests Recommandés

### Test 1 : 1 Prime
```
Input: [500]
Expected: prime_1=500, prime_2=0, prime_3=0
```

### Test 2 : 3 Primes exactement
```
Input: [800, 500, 200]
Expected: prime_1=800, prime_2=500, prime_3=200
```

### Test 3 : 5 Primes
```
Input: [1000, 800, 500, 300, 200]
Expected: prime_1=1000, prime_2=800, prime_3=1000 (500+300+200)
```

### Test 4 : Vérifier le total
```
Sum(input) === prime_1 + prime_2 + prime_3  // DOIT être égal
```

---

## 📝 Code Implémenté

### Localisation

```
File: Services/Llm/ClaudeService.cs
Method: AdaptToDslFormat()
Lines: ~160-230
```

### Logique Clé

```csharp
// Agréger toutes les sources de primes imposables
var primesImposables = new List<(string label, decimal montant)>();

// 1. SalaryComponents (toujours imposables)
foreach (var comp in data.SalaryComponents)
    primesImposables.Add((comp.ComponentType, comp.Amount));

// 2. PackageItems taxables
foreach (var item in data.PackageItems.Where(p => p.IsTaxable))
    primesImposables.Add((item.Label, item.DefaultValue));

// Trier + Mapper
var primesTriees = primesImposables.OrderByDescending(p => p.montant).ToList();

prime1 = primesTriees[0].montant;
prime2 = primesTriees[1].montant;
prime3 = primesTriees.Skip(2).Sum(p => p.montant); // ⭐ Agrégation
```

---

## ✅ Conclusion

L'agrégation des primes par ordre décroissant est la **meilleure solution** actuelle :

- ✅ Aucune perte de données
- ✅ Calcul de paie correct
- ✅ Compatible avec le DSL
- ✅ Traçable via metadata
- ✅ Fonctionne avec n'importe quel nombre de primes

**Impact sur le résultat final : ZÉRO** ⭐

Le net à payer sera **identique** qu'on ait 3 ou 50 primes, car seule la **somme totale** compte pour le calcul fiscal et social.
