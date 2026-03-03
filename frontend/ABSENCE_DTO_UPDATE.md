# Mise à jour du modèle d'absence

## Changements effectués

### 1. Modèle (`absence.model.ts`)
- ✅ `absenceDate` (DateOnly) au lieu de `startDate` et `endDate`
- ✅ `durationType` ajouté : 'FullDay' | 'HalfDay' | 'Hourly'
- ✅ `isMorning` pour les demi-journées (true = matin, false = après-midi)
- ✅ `startTime` et `endTime` pour les tranches horaires
- ✅ `absenceType` mis à jour : 'JUSTIFIED' | 'UNJUSTIFIED' | 'SICK' | 'MISSION'
- ✅ Supprimé `status` (plus de workflow d'approbation)
- ✅ `employeeId` en nombre au lieu de string
- ✅ `createdAt` et `createdBy` ajoutés

### 2. Composant (`employee-absences.ts`)
- ✅ Formulaire mis à jour avec `absenceDate`, `durationType`, `absenceType`
- ✅ Ajout de `durationTypes` et `halfDayOptions`
- ✅ Validation selon le type de durée (HalfDay/Hourly)
- ✅ Méthodes `getAbsenceTypeLabel()`, `getDurationLabel()`, `getAbsenceTypeSeverity()`
- ✅ Supprimé `cancelAbsence()` et `getStatusSeverity()`
- ✅ Stats simplifiées (totalAbsences, totalDays seulement)
- ✅ Ajout de `updateField()` pour gérer les changements de formulaire

### 3. Template HTML (`employee-absences.html`)
- ✅ Colonnes du tableau : Date, Type, Durée, Motif
- ✅ Formulaire avec champs conditionnels :
  - Date (obligatoire)
  - Type d'absence (obligatoire)
  - Durée (obligatoire)
  - Période si demi-journée (Matin/Après-midi)
  - Heures de début/fin si tranche horaire
  - Motif (optionnel)

### 4. Traductions
- ✅ Français : types, durations, form fields ajoutés
- ✅ Anglais : types, durations, form fields ajoutés  
- ✅ Arabe : types, durations, form fields ajoutés
- ⚠️ À vérifier manuellement : Les anciennes clés (paid_leave, etc.) doivent être supprimées des fichiers JSON

## Actions restantes

### Fichiers de traduction
Les fichiers `fr.json`, `en.json`, `ar.json` contiennent encore les anciennes clés. Vérifier et nettoyer :
- Supprimer les anciennes clés sous `types` : `paid_leave`, `unpaid_leave`, `sick_leave`, `maternity_leave`, `paternity_leave`, `other`
- Ajouter manuellement dans `form` les clés manquantes : `date`, `durationType`, `period`, `startTime`, `endTime`
- Ajouter dans `table` : `date`

### Autres composants à mettre à jour
1. **`hr-absences.ts/html`** - Vue RH overview
2. **`employee-absence-detail.ts/html`** - Vue RH détail employé  
3. **`absence.service.ts`** - Appels API à vérifier

### Backend
Assurer que les endpoints correspondent au nouveau DTO :
- `POST /api/absences` accepte le nouveau format
- `GET /api/absences` retourne le nouveau format
- Les statistiques calculent correctement les jours (FullDay = 1, HalfDay = 0.5, Hourly = calcul horaire)
