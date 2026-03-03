# Guide des Modèles Claude et Optimisation des Coûts

## 📊 Comparaison des Modèles (Février 2026)

### Claude Sonnet 4.5 ⭐ (Utilisé actuellement)
- **ID** : `claude-sonnet-4-5-20250929`
- **Qualité** : Excellente (meilleur que 3.5)
- **Vitesse** : Rapide
- **Prix** : Modéré
- **Caching** : ✅ Supporté
- **Recommandation** : **Idéal pour le payroll** - Bon équilibre qualité/prix/vitesse

### Claude 3.5 Haiku
- **ID** : `claude-3-5-haiku-20241022`
- **Qualité** : Bonne
- **Vitesse** : Très rapide
- **Prix** : ~1/10ème de Sonnet
- **Caching** : ✅ Supporté
- **Recommandation** : Tester si Sonnet 4.5 est trop cher

### Claude Opus 4
- **ID** : `claude-opus-4-20250514`
- **Qualité** : Meilleure
- **Vitesse** : Lente
- **Prix** : 5x plus cher que Sonnet
- **Recommandation** : ❌ Overkill pour du calcul de paie

---

## 💰 Prompt Caching : Économisez jusqu'à 90%

### Qu'est-ce que le Prompt Caching ?

Le prompt caching permet de mettre en **cache les parties statiques** du prompt (comme les règles de paie) qui ne changent pas entre les requêtes.

### Comment ça fonctionne ?

1. **Première requête** : Claude traite tout le prompt (règles + données)
   - Coût normal : 100%

2. **Requêtes suivantes** : Claude réutilise les règles en cache
   - Coût : ~10-20% (seules les nouvelles données sont traitées)

### Économies réelles

Pour 100 employés :
- **Sans caching** : ~15 000 tokens × 100 = 1 500 000 tokens
- **Avec caching** : (15 000 tokens première fois) + (1 500 tokens × 99) = ~163 500 tokens
- **Économie** : ~89% 💸

---

## 🔧 Implémentation dans le Code

### Structure optimisée

```csharp
var parameters = new MessageCreateParams
{
    Model = "claude-sonnet-4-5-20250929",
    MaxTokens = 4096,
    
    // ✅ System prompt avec CACHE CONTROL
    // Les règles de paie sont mises en cache
    System = new TextBlockParam[]
    {
        new TextBlockParam
        {
            Text = systemPrompt, // Contient les règles
            CacheControl = new CacheControlEphemeral()
        }
    },
    
    // ✅ User message sans cache
    // Données spécifiques à chaque employé
    Messages = [...]
};
```

### Ce qui est caché

✅ **Mis en cache (statique)** :
- Règles de paie marocaines
- Instructions de calcul
- Format de sortie JSON
- Barème IR, taux CNSS, AMO, etc.

❌ **Non caché (dynamique)** :
- Données de l'employé
- Salaire, contrat, absences
- Période de calcul

---

## 📈 Durée du Cache

### Par défaut
- **TTL (Time To Live)** : 5 minutes
- Idéal pour traiter plusieurs employés d'affilée

### Cache étendu (optionnel)
```csharp
CacheControl = new CacheControlEphemeral 
{ 
    Ttl = Ttl.Ttl1h // Cache pendant 1 heure
}
```

---

## 💡 Conseils d'Optimisation

### 1. Traiter par lot
Au lieu de calculer employé par employé en temps réel, traiter tous les employés d'un coup :
```csharp
// ✅ Bon : 100 employés en une fois
await _paieService.TraiterTousLesSalariesAsync(month, year);

// ❌ Éviter : 100 appels API séparés avec délais
foreach (var emp in employees) 
{
    await RecalculateForEmployee(emp.Id);
    await Task.Delay(1000); // Cache expiré !
}
```

### 2. Grouper les règles communes
Mettre toutes les règles fixes dans le system prompt :
- ✅ Règles de paie
- ✅ Barèmes
- ✅ Format JSON
- ❌ Données variables

### 3. Monitorer l'utilisation
Stocker `TokensUsed` dans PayrollResult pour suivre :
- Tokens utilisés par employé
- Impact du caching
- Coût total mensuel

---

## 🔄 Tester différents modèles

Pour changer de modèle facilement, tu peux :

### Option 1 : Configuration dans appsettings.json

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-...",
    "Model": "claude-sonnet-4-5-20250929"
  }
}
```

```csharp
// Dans ClaudeService
Model = config["Anthropic:Model"] ?? "claude-sonnet-4-5-20250929"
```

### Option 2 : Paramètre dans la méthode

```csharp
public async Task<string> AnalyseSalarieAsync(
    string regleContent,
    EmployeePayrollDto payrollData,
    string instruction,
    string? model = null, // Permettre override
    CancellationToken cancellationToken = default)
{
    var selectedModel = model ?? "claude-sonnet-4-5-20250929";
    // ...
}
```

---

## 📊 Comparaison de Coûts (100 employés)

### Sonnet 4.5 SANS caching
- Input : ~1.5M tokens × $3/M = $4.50
- Output : ~100K tokens × $15/M = $1.50
- **Total : ~$6.00**

### Sonnet 4.5 AVEC caching
- Input (1er) : 15K tokens × $3/M = $0.045
- Input (99 suivants) : 148.5K tokens × $0.30/M = $0.045
- Cache hits : ~89% économie
- Output : 100K tokens × $15/M = $1.50
- **Total : ~$1.60** 💚

### Haiku 3.5 AVEC caching
- Encore 10x moins cher que Sonnet
- **Total : ~$0.16** 🎉

---

## ⚠️ Limitations du Caching

1. **Durée limitée** : 5 min ou 1h max
2. **Taille minimale** : Besoin d'au moins 1024 tokens à cacher
3. **Ordre important** : Le contenu caché doit être au début du prompt
4. **API Key** : Doit avoir accès au caching (généralement inclus)

---

## 🎯 Recommandation Finale

Pour le payroll avec 50-200 employés/mois :

1. **Rester sur Sonnet 4.5** avec caching ✅
   - Meilleur rapport qualité/précision
   - Caching réduit les coûts de 90%
   
2. **Tester Haiku 3.5** si le budget est serré
   - Qualité suffisante pour des calculs structurés
   - 10x moins cher
   
3. **Monitorer** :
   - Suivre les erreurs par modèle
   - Comparer les coûts réels
   - Ajuster selon les résultats

---

## 📝 Ressources

- [Anthropic Prompt Caching](https://docs.anthropic.com/en/docs/build-with-claude/prompt-caching)
- [Modèles disponibles](https://docs.anthropic.com/en/docs/about-claude/models)
- [Tarification](https://www.anthropic.com/pricing)
