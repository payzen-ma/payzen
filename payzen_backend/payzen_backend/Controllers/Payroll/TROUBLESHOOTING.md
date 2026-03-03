# Troubleshooting - Calcul de Paie avec Claude

## 🐛 Problèmes Courants et Solutions

### 1. Erreur JSON : "Invalid start of value"

**Symptôme :**
```json
{
    "status": "Error",
    "errorMessage": "'`' is an invalid start of a value. LineNumber: 0 | BytePositionInLine: 0."
}
```

**Cause :**  
Claude a retourné du markdown au lieu de JSON pur :
```markdown
```json
{
  "salaire_base": 8000
}
```
```

**Solution appliquée :**
1. ✅ Prompt renforcé avec instructions plus claires
2. ✅ Méthode `CleanJsonResponse()` qui nettoie automatiquement :
   - Enlève les blocs markdown (```json ... ```)
   - Enlève les backticks
   - Extrait uniquement le JSON entre `{` et `}`

**Test :**
```bash
POST /api/payroll/recalculate/3?month=1&year=2026
```

---

### 2. Erreur : "Failed to load configuration from appsettings.json"

**Symptôme :**
```
Unable to create a 'DbContext' of type 'AppDbContext'
```

**Cause :**  
JSON invalide dans `appsettings.json` (accolade orpheline, virgule manquante, etc.)

**Solution :**
Vérifier la syntaxe JSON :
```json
{
  "ConnectionStrings": { ... },
  "JwtSettings": { ... },     // ✅ Virgule après chaque propriété (sauf la dernière)
  "Anthropic": { ... }        // ✅ Pas de virgule après la dernière
}
```

**Validation :**
```bash
dotnet ef dbcontext info
```

---

### 3. Claude retourne des calculs incorrects

**Symptôme :**  
Les montants calculés ne correspondent pas aux règles de paie.

**Causes possibles :**
1. **Règles de paie incomplètes ou ambiguës** dans `rules/regles_paie.txt`
2. **Données manquantes** dans `EmployeePayrollDto`
3. **Prompt trop vague**

**Solutions :**

#### A. Améliorer les règles de paie
```
❌ Mauvais :
"Calculer la CNSS"

✅ Bon :
"CNSS : 4.48% du salaire brut, plafonné à 6 000 MAD.
Si salaire > 6 000, calculer : 6 000 × 4.48% = 268.80 MAD
Sinon : salaire × 4.48%"
```

#### B. Ajouter des exemples dans le prompt
Ajouter des cas de calcul dans `regles_paie.txt` :
```markdown
## EXEMPLE DE CALCUL COMPLET

Salaire brut : 8 400 MAD
CNSS : min(8400, 6000) × 4.48% = 268.80 MAD
AMO : 8400 × 2.26% = 189.84 MAD
...
```

#### C. Logger la réponse de Claude
La réponse complète est stockée dans `PayrollResult.ResultatJson` :
```sql
SELECT ResultatJson FROM PayrollResults WHERE Id = 1
```

---

### 4. Cache ne fonctionne pas / Coûts trop élevés

**Symptôme :**  
Les coûts restent élevés malgré le prompt caching.

**Causes possibles :**
1. **TTL du cache expiré** (5 minutes par défaut)
2. **Appels espacés** (> 5 min entre chaque employé)
3. **Contenu caché trop petit** (< 1024 tokens)

**Solutions :**

#### A. Traiter par lot
```csharp
// ✅ Bon : tous les employés d'un coup
await _paieService.TraiterTousLesSalariesAsync(month, year);

// ❌ Mauvais : un par un avec délai
foreach (var emp in employees) {
    await Task.Delay(10000); // Cache expiré !
    await RecalculateForEmployee(emp.Id);
}
```

#### B. Augmenter le TTL (optionnel)
```csharp
CacheControl = new CacheControlEphemeral 
{ 
    Ttl = Ttl.Ttl1h // 1 heure au lieu de 5 min
}
```

#### C. Vérifier la taille du prompt
Les règles doivent faire > 1024 tokens (~4000 caractères).  
Fichier `regles_paie.txt` actuel : ~900 lignes ✅

---

### 5. Timeout / Lenteur

**Symptôme :**  
Les requêtes prennent > 30 secondes ou timeout.

**Causes possibles :**
1. **Règles de paie trop longues** (> 50 000 tokens)
2. **Modèle trop lent** (Opus)
3. **Pas de MaxTokens** limité

**Solutions :**

#### A. Optimiser le modèle
```csharp
// Si trop lent avec Sonnet 4.5
Model = "claude-3-5-haiku-20241022" // 10x plus rapide
```

#### B. Limiter MaxTokens
```csharp
MaxTokens = 4096 // Évite les réponses trop longues
```

#### C. Simplifier les règles
- Enlever les exemples répétitifs
- Utiliser des tableaux au lieu de texte
- Garder uniquement les règles essentielles

---

### 6. Erreur : "No store type specified for decimal"

**Symptôme (dans les logs EF Core) :**
```
No store type was specified for the decimal property 'StandardDayHours'
```

**Cause :**  
Propriété `decimal` sans précision dans EF Core.

**Solution :**
Ajouter dans `AppDbContext.OnModelCreating` :
```csharp
entity.Property(e => e.StandardDayHours)
    .HasColumnType("decimal(18,2)");
```

---

### 7. Données manquantes pour un employé

**Symptôme :**
```json
{
    "status": "Error",
    "errorMessage": "Employé 123 introuvable."
}
```

**Causes possibles :**
1. Employé supprimé (soft delete)
2. Pas de contrat actif
3. Pas de salaire configuré

**Debug :**
```csharp
// Dans EmployeePayrollDataService.BuildPayrollDataAsync
var employee = await _db.Employees
    .Include(e => e.Status)
    .FirstOrDefaultAsync(e => e.Id == employeeId);

if (employee == null)
    throw new Exception($"Employé {employeeId} introuvable.");

if (contract == null)
    _logger.LogWarning("Pas de contrat actif pour employé {EmployeeId}", employeeId);
```

---

### 8. JSON trop volumineux retourné par Claude

**Symptôme :**  
Le champ `ResultatJson` dépasse la limite SQL (nvarchar(max) mais > 2 GB en pratique).

**Solution :**
Limiter la réponse de Claude dans le prompt :
```
IMPORTANT : Retourne UNIQUEMENT les montants clés sans détails excessifs.
Ne pas inclure : historique, logs, explications détaillées.
```

Ou compresser le JSON avant stockage :
```csharp
using System.IO.Compression;

var compressedJson = Compress(jsonBrut);
result.ResultatJson = compressedJson;
```

---

### 9. Erreur de parsing des montants

**Symptôme :**
```
TotalBrut = null, TotalNet = null (alors que le JSON contient les valeurs)
```

**Cause :**  
Claude a retourné des nombres en tant que strings :
```json
{
  "total_brut": "8500.00"  // ❌ String au lieu de number
}
```

**Solution :**
Améliorer `GetDecimal()` dans `PaieService` :
```csharp
static decimal? GetDecimal(JsonElement root, string name)
{
    if (!root.TryGetProperty(name, out var prop)) return null;
    
    // Essayer en tant que nombre
    if (prop.ValueKind == JsonValueKind.Number)
        return prop.GetDecimal();
    
    // Essayer en tant que string
    if (prop.ValueKind == JsonValueKind.String)
    {
        if (decimal.TryParse(prop.GetString(), out var value))
            return value;
    }
    
    return null;
}
```

---

### 10. Performance : traitement de 500+ employés

**Symptôme :**  
Le traitement prend > 10 minutes pour 500 employés.

**Solutions :**

#### A. Traitement en parallèle (avec limite)
```csharp
var semaphore = new SemaphoreSlim(10); // Max 10 en parallèle
await Parallel.ForEachAsync(employeeIds, async (empId, ct) =>
{
    await semaphore.WaitAsync(ct);
    try
    {
        await TraiterUnSeulEmployeAsync(empId, month, year);
    }
    finally
    {
        semaphore.Release();
    }
});
```

#### B. Background job (Hangfire)
```csharp
[Queue("payroll")]
public async Task ProcessPayrollJob(int month, int year)
{
    await _paieService.TraiterTousLesSalariesAsync(month, year);
}
```

#### C. Utiliser Haiku au lieu de Sonnet
```csharp
Model = "claude-3-5-haiku-20241022" // 10x plus rapide, 10x moins cher
```

---

## 📊 Monitoring et Logs

### Logs utiles

```csharp
// Dans PaieService
_logger.LogInformation("Début traitement employé {EmployeeId}", employeeId);
_logger.LogError(ex, "Erreur pour employé {EmployeeId}: {Error}", employeeId, ex.Message);
```

### Métriques à suivre

1. **Temps de traitement moyen par employé**
2. **Taux d'erreur** (Status = Error)
3. **Tokens utilisés** (via `TokensUsed`)
4. **Coût total mensuel**

### Requête SQL utile

```sql
-- Statistiques du dernier traitement
SELECT 
    Status,
    COUNT(*) as Count,
    AVG(DATEDIFF(second, CreatedAt, ProcessedAt)) as AvgDurationSec,
    SUM(TokensUsed) as TotalTokens
FROM PayrollResults
WHERE Month = 1 AND Year = 2026
GROUP BY Status;
```

---

## 🆘 Support

Si le problème persiste :

1. **Vérifier les logs** : `dotnet run` en mode Development
2. **Tester avec un seul employé** : `POST /api/payroll/recalculate/{id}`
3. **Consulter ResultatJson** : Voir exactement ce que Claude retourne
4. **Simplifier les règles** : Tester avec des règles minimales
5. **Essayer Haiku** : Plus rapide et plus prévisible pour débugger

---

## ✅ Checklist de déploiement

Avant de passer en production :

- [ ] ✅ `appsettings.json` valide (syntaxe JSON)
- [ ] ✅ Clé API Anthropic configurée
- [ ] ✅ Fichier `rules/regles_paie.txt` présent et complet
- [ ] ✅ Migration DB appliquée (table `PayrollResults`)
- [ ] ✅ Tests sur 5-10 employés
- [ ] ✅ Vérification des montants calculés
- [ ] ✅ Logs activés en production
- [ ] ✅ Monitoring des coûts API
