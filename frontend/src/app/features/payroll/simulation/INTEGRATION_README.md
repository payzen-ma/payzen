# Intégration Frontend - API de Simulation de Salaire

## 📁 Fichiers Créés/Modifiés

### ✅ Service créé
- `src/app/core/services/salary-simulation.service.ts` - Service Angular pour appeler les API de simulation

### ✅ Composant mis à jour
- `src/app/features/payroll/simulation/simulation.component.ts` - Utilise maintenant le vrai service API
- `src/app/features/payroll/simulation/simulation.component.html` - Affiche la réponse brute de Claude

---

## 🔌 Endpoints Disponibles

### 1. **POST `/api/claudesimulation/simulate-quick`** ⚡ (Recommandé)
Simulation rapide avec les règles système

**Utilisation dans le code :**
```typescript
this.simulationService.simulateQuick("Je veux un net de 10 000 DH")
  .subscribe(response => {
    console.log(response.result); // Réponse markdown avec 3 scénarios
  });
```

### 2. **POST `/api/claudesimulation/simulate`**
Simulation avec règles personnalisées

**Utilisation :**
```typescript
this.simulationService.simulate({
  regleContent: "... règles DSL ...",
  instruction: "Je veux un net de 15 000 DH"
}).subscribe(response => {
  // ...
});
```

### 3. **GET `/api/claudesimulation/rules`**
Récupérer les règles DSL du système

**Utilisation :**
```typescript
this.simulationService.getRules()
  .subscribe(response => {
    console.log(response.content); // Contenu du fichier regles_paie_compact.txt
  });
```

---

## 🚀 Comment Tester

1. **Démarrer le backend** :
   ```bash
   cd payzen/payzen_backend/payzen_backend
   dotnet run
   ```

2. **Démarrer le frontend** :
   ```bash
   cd payzen/frontend
   npm start
   ```

3. **Accéder à la page de simulation** :
   - URL : `http://localhost:4200/payroll/simulation`
   - Entrer une instruction comme : "Je veux un net de 10 000 DH"
   - Cliquer sur "Générer les compositions"

---

## 📋 Réponse Attendue de l'API

```json
{
  "success": true,
  "result": "## Scénario 1 : Approche équilibrée\n\n### Composition du salaire\n...",
  "timestamp": "2026-03-06T14:30:00Z"
}
```

Le champ `result` contient du **Markdown** avec 3 scénarios structurés selon le format défini dans le prompt système.

---

## 🎯 Prochaines Étapes

### TODO : Parser Markdown → Objets TypeScript

Créer une fonction pour convertir la réponse markdown de Claude en objets `Composition[]` :

```typescript
private parseClaudeResponse(markdown: string): Composition[] {
  // Parser le markdown pour extraire les 3 scénarios
  // Extraire : titre, description, éléments, montants, etc.
  // Retourner un tableau de 3 Composition
}
```

**Structure cible** :
```typescript
interface Composition {
  titre: string;              // "Approche équilibrée"
  description: string;        // Description du scénario
  elements: PayElement[];     // Salaire base, primes, déductions
  brut_imposable: number;
  total_retenues: number;
  cout_employeur: number;
  salaire_net: number;
  calcul_steps: CalculStep[];
}
```

### Stratégie de Parsing

1. **Diviser par scénarios** : Séparer sur `## Scénario`
2. **Extraire les titres** : Regex sur `## Scénario [0-9]: (.+)`
3. **Parser les tableaux markdown** : Extraire montants et libellés
4. **Extraire les montants clés** : Brut, retenues, net, coût employeur
5. **Construire les objets** : Mapper vers `Composition[]`

### Alternative : Demander JSON à Claude

Modifier le prompt système pour demander une réponse JSON au lieu de Markdown :

```diff
- "Utilise un format markdown avec des tableaux pour une meilleure lisibilité."
+ "Réponds UNIQUEMENT avec un JSON valide contenant un tableau de 3 scénarios."
```

---

## 🔐 Configuration Requise

### Backend : `appsettings.json`
```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-...",
    "UseMock": false
  }
}
```

### Frontend : `environment.ts`
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7193/api'
};
```

---

## 🐛 Debugging

### Si l'API ne répond pas
1. Vérifier que le backend tourne
2. Vérifier l'URL de l'API dans `environment.ts`
3. Ouvrir la console navigateur (F12) pour voir les erreurs HTTP

### Si Claude ne retourne rien
1. Vérifier que `AnthropicApiKey` est configurée dans `appsettings.json`
2. Vérifier que le fichier `/rules/regles_paie_compact.txt` existe
3. Regarder les logs du backend pour voir les erreurs

### Affichage de la réponse brute
La réponse markdown brute de Claude s'affiche temporairement dans un bloc sur la page de simulation pour faciliter le debug.

---

## ✅ Statut Actuel

- ✅ Service API créé
- ✅ Composant connecté à l'API réelle  
- ✅ Affichage de la réponse brute de Claude
- ⏳ Parser Markdown → TypeScript (TODO)
- ⏳ Affichage des 3 scénarios parsés (TODO)

L'intégration est **fonctionnelle** : vous pouvez déjà recevoir les 3 formules de Claude ! Le parsing pour un affichage visuel élégant reste à implémenter.
