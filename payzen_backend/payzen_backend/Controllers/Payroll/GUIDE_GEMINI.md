# 🌟 Guide : Utiliser Google Gemini (100% Gratuit)

## 🎯 Pourquoi Gemini ?

✅ **100% GRATUIT** pour les tests  
✅ **1,000,000 tokens/jour** (vs 5$ seulement avec Claude)  
✅ **15 requêtes/minute**  
✅ **1500 requêtes/jour**  
✅ Très bon pour les calculs structurés (JSON)

---

## 🔑 Étape 1 : Obtenir une Clé API

1. Allez sur : **https://aistudio.google.com/app/apikey**
2. Cliquez sur **"Create API Key"**
3. Copiez la clé (format : `AIza...`)

---

## ⚙️ Étape 2 : Configurer l'Application

Éditez `appsettings.json` et remplacez `YOUR_GOOGLE_API_KEY_HERE` par votre clé :

```json
{
  "Google": {
    "ApiKey": "AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXX",
    "UseGemini": true
  }
}
```

---

## 🚀 Étape 3 : Activer Gemini

### Option A : Via `appsettings.json` (Production)

```json
{
  "Anthropic": {
    "UseMock": false
  },
  "Google": {
    "UseGemini": true  // ⭐ Activer Gemini
  }
}
```

### Option B : Via `appsettings.Development.json` (Développement)

```json
{
  "Anthropic": {
    "UseMock": false
  },
  "Google": {
    "UseGemini": true  // ⭐ Activer Gemini
  }
}
```

---

## 🧪 Étape 4 : Tester

Relancez l'application et vous verrez :

```
🌟 MODE GEMINI ACTIVÉ - Google Gemini 1.5 Flash
💚 100% GRATUIT - 15 req/min, 1500 req/jour, 1M tokens/jour
```

Faites un calcul de paie et vous verrez les statistiques :

```
📊 Statistiques Gemini :
   - Modèle : gemini-1.5-flash
   - Input Tokens : 4523
   - Output Tokens : 2847
   - Total Tokens : 7370
   - 💰 Coût : GRATUIT (15 req/min, 1500 req/jour)
```

---

## 🔄 Basculer entre les Services

### 3 Modes Disponibles :

| Mode | Config | Cas d'usage |
|------|--------|-------------|
| **Mock** | `UseMock: true` | Tests locaux (pas de LLM) |
| **Gemini** | `UseGemini: true` | Tests gratuits illimités |
| **Claude** | Les deux à `false` | Production (payant) |

### Exemples :

```json
// Mode 1 : Mock (tests locaux)
{
  "Anthropic": { "UseMock": true },
  "Google": { "UseGemini": false }
}

// Mode 2 : Gemini (tests gratuits)
{
  "Anthropic": { "UseMock": false },
  "Google": { "UseGemini": true }
}

// Mode 3 : Claude (production)
{
  "Anthropic": { "UseMock": false },
  "Google": { "UseGemini": false }
}
```

---

## 📊 Comparaison : Gemini vs Claude

| Critère | Gemini 1.5 Flash | Claude Sonnet 4.5 |
|---------|------------------|-------------------|
| **Coût** | **GRATUIT** | 0.015$/calcul |
| **Limite/jour** | **1500 calculs** | ~333 avec 5$ |
| **Qualité** | Très bon | Excellent |
| **Vitesse** | Rapide | Rapide |
| **JSON** | ✅ Natif | ✅ Très bon |

---

## ⚠️ Limites de Gemini Gratuit

Si vous dépassez les limites, vous verrez :

```
Error 429: Resource has been exhausted (e.g. check quota)
```

**Solutions :**
1. Attendre 1 minute (limite : 15 req/min)
2. Attendre le lendemain (limite : 1500 req/jour)
3. Passer à Claude (payant mais illimité)

---

## 🎯 Recommandation

- **Développement & Tests** : Utilisez **Gemini** (gratuit, illimité)
- **Production** : Utilisez **Claude** (meilleure qualité, prompt caching)

---

## 🔧 Troubleshooting

### Erreur : `Google:ApiKey non configuré`

➡️ Vérifiez que vous avez bien ajouté la clé dans `appsettings.json`

### Erreur : `401 Unauthorized`

➡️ Votre clé API est invalide. Générez-en une nouvelle sur https://aistudio.google.com/app/apikey

### Erreur : `429 Resource exhausted`

➡️ Vous avez atteint la limite (15 req/min ou 1500 req/jour). Attendez ou passez à Claude.

---

## ✅ Configuration Finale Recommandée

**`appsettings.Development.json`** (Tests) :
```json
{
  "Anthropic": { "UseMock": false },
  "Google": { 
    "ApiKey": "YOUR_GOOGLE_API_KEY",
    "UseGemini": true 
  }
}
```

**`appsettings.json`** (Production) :
```json
{
  "Anthropic": { 
    "ApiKey": "YOUR_CLAUDE_API_KEY",
    "UseMock": false 
  },
  "Google": { "UseGemini": false }
}
```

**Résultat** : Tests gratuits avec Gemini en dev, Claude en production ! 🎉
