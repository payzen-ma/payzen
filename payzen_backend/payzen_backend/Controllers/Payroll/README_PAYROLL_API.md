# API Payroll avec Claude AI

## Configuration

### 1. Clé API Anthropic

Ajouter dans `appsettings.json` ou variables d'environnement :

```json
{
  "Anthropic": {
    "ApiKey": "votre-clé-api-anthropic"
  }
}
```

### 2. Fichier de règles de paie

Créer le fichier `rules/regles_paie.txt` à la racine du projet avec les règles marocaines de paie.

Exemple de contenu :
```
RÈGLES DE PAIE MAROC 2025

1. COTISATIONS SOCIALES CNSS
   - Salariales : 4.48% du salaire brut plafonné à 6 000 MAD
   - Patronales : 16.47% du salaire brut plafonné à 6 000 MAD

2. AMO (Assurance Maladie Obligatoire)
   - Salariales : 2.26% du salaire brut
   - Patronales : 4.11% du salaire brut

3. IMPÔT SUR LE REVENU (IR)
   - Tranches progressives selon barème
   - Déductions fiscales : famille, frais professionnels

4. CIMR (optionnel)
   - Taux variable selon contrat

... etc ...
```

## Endpoints disponibles

### 1. Calculer la paie pour tous les employés

```http
POST /api/payroll/calculate?month=1&year=2025
Authorization: Bearer {token}
```

**Réponse :**
```json
{
  "message": "Calcul de paie terminé pour 1/2025.",
  "month": 1,
  "year": 2025
}
```

---

### 2. Obtenir les résultats de paie

```http
GET /api/payroll/results?month=1&year=2025&companyId=1&status=2
Authorization: Bearer {token}
```

**Paramètres optionnels :**
- `companyId` : Filtrer par entreprise
- `status` : Filtrer par statut (0=Pending, 1=Processing, 2=OK, 3=Error, 4=ManualReviewRequired)

**Réponse :**
```json
{
  "count": 25,
  "month": 1,
  "year": 2025,
  "results": [
    {
      "id": 1,
      "employeeId": 123,
      "employeeName": "Mohammed Alami",
      "companyId": 1,
      "companyName": "ACME Corp",
      "month": 1,
      "year": 2025,
      "status": 2,
      "errorMessage": null,
      "salaireBase": 8000.00,
      "totalBrut": 8500.00,
      "totalCotisationsSalariales": 850.00,
      "totalCotisationsPatronales": 1400.00,
      "impotRevenu": 450.00,
      "totalNet": 7200.00,
      "processedAt": "2025-01-15T10:30:00Z",
      "claudeModel": "claude-3-5-sonnet-20241022",
      "tokensUsed": null
    }
  ]
}
```

---

### 3. Obtenir le détail d'une fiche de paie

```http
GET /api/payroll/results/{id}
Authorization: Bearer {token}
```

**Réponse :**
```json
{
  "id": 1,
  "employeeId": 123,
  "employeeName": "Mohammed Alami",
  "companyId": 1,
  "companyName": "ACME Corp",
  "month": 1,
  "year": 2025,
  "status": 2,
  "errorMessage": null,
  "montants": {
    "salaireBase": 8000.00,
    "totalBrut": 8500.00,
    "totalCotisationsSalariales": 850.00,
    "totalCotisationsPatronales": 1400.00,
    "impotRevenu": 450.00,
    "totalNet": 7200.00,
    "totalNet2": 7200.00
  },
  "detailClaude": {
    "salaire_base": 8000,
    "primes": [
      { "type": "Prime ancienneté", "montant": 500 }
    ],
    "total_brut": 8500,
    "cotisations_salariales": 850,
    "cotisations_patronales": 1400,
    "ir": 450,
    "net_a_payer": 7200,
    "details_cnss": { ... },
    "details_amo": { ... }
  },
  "metadata": {
    "claudeModel": "claude-3-5-sonnet-20241022",
    "tokensUsed": null,
    "processedAt": "2025-01-15T10:30:00Z",
    "createdAt": "2025-01-15T10:30:00Z"
  }
}
```

---

### 4. Obtenir les statistiques de paie

```http
GET /api/payroll/stats?month=1&year=2025&companyId=1
Authorization: Bearer {token}
```

**Réponse :**
```json
{
  "month": 1,
  "year": 2025,
  "companyId": 1,
  "total": 25,
  "totalMontantBrut": 212500.00,
  "totalMontantNet": 180000.00,
  "parStatut": [
    {
      "status": 2,
      "count": 23,
      "totalBrut": 195500.00,
      "totalNet": 165600.00
    },
    {
      "status": 3,
      "count": 2,
      "totalBrut": 17000.00,
      "totalNet": 14400.00
    }
  ]
}
```

---

### 5. Recalculer la paie pour un employé

```http
POST /api/payroll/recalculate/123?month=1&year=2025
Authorization: Bearer {token}
```

**Réponse :**
```json
{
  "message": "Recalcul terminé avec succès.",
  "employeeId": 123,
  "month": 1,
  "year": 2025,
  "status": "OK",
  "errorMessage": null,
  "resultId": 45
}
```

---

### 6. Supprimer un résultat de paie

```http
DELETE /api/payroll/results/{id}
Authorization: Bearer {token}
```

**Réponse :**
```json
{
  "message": "Résultat de paie supprimé avec succès."
}
```

---

## Statuts des résultats

| Valeur | Nom | Description |
|--------|-----|-------------|
| 0 | Pending | En attente de traitement |
| 1 | Processing | En cours de traitement |
| 2 | OK | Traité avec succès |
| 3 | Error | Erreur lors du traitement |
| 4 | ManualReviewRequired | Nécessite une révision manuelle |

---

## Workflow recommandé

1. **Lancer le calcul** : `POST /api/payroll/calculate?month=1&year=2025`
2. **Vérifier les résultats** : `GET /api/payroll/results?month=1&year=2025`
3. **Consulter les détails** : `GET /api/payroll/results/{id}`
4. **Recalculer si nécessaire** : `POST /api/payroll/recalculate/{employeeId}?month=1&year=2025`
5. **Obtenir les statistiques** : `GET /api/payroll/stats?month=1&year=2025`

---

## Gestion des erreurs

En cas d'erreur lors du calcul pour un employé spécifique :
- Le statut sera `Error` (3)
- Le champ `errorMessage` contiendra le détail de l'erreur
- Les autres employés continueront à être traités

Pour corriger :
1. Identifier la cause (données manquantes, règles incorrectes, etc.)
2. Corriger les données ou les règles
3. Relancer le calcul avec `POST /api/payroll/recalculate/{employeeId}`

---

## Structure du JSON retourné par Claude

Claude doit retourner un JSON avec au minimum ces champs :

```json
{
  "salaire_base": 8000,
  "total_brut": 8500,
  "cotisations_salariales": 850,
  "cotisations_patronales": 1400,
  "ir": 450,
  "net_a_payer": 7200
}
```

Ces valeurs sont extraites et stockées dans les champs dédiés de `PayrollResult` pour faciliter les requêtes et les statistiques sans avoir à parser le JSON à chaque fois.

---

## Notes importantes

1. **Performance** : Le calcul pour tous les employés peut prendre du temps. Pour une entreprise avec 100+ employés, considérer :
   - Un traitement en arrière-plan (background job)
   - Une pagination des résultats
   - Un système de queue (RabbitMQ, Azure Service Bus, etc.)

2. **Coûts API** : Chaque calcul consomme des tokens Claude. Monitorer l'utilisation via le champ `tokensUsed`.

3. **Règles de paie** : S'assurer que le fichier `rules/regles_paie.txt` est à jour et complet.

4. **Soft delete** : Les résultats supprimés sont marqués comme supprimés (`DeletedAt`) mais restent en base.
