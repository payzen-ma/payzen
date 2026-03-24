namespace Payzen.Domain.Enums;

/// <summary>
/// Type d'heures supplémentaires.
/// [Flags] permet de combiner plusieurs types sur une même heure sup
/// ex: Night | PublicHoliday = travail de nuit un jour férié.
/// Le taux applicable est déterminé par OvertimeRateRule selon la stratégie
/// de cumul MultiplierCumulationStrategy.
/// </summary>
[Flags]
public enum OvertimeType
{
    /// <summary>Aucun (valeur par défaut, ne doit pas être stocké)</summary>
    None = 0,

    /// <summary>
    /// Heures sup normales (prolongement journée standard).
    /// Taux marocain habituel : +25% en journée, +50% de nuit.
    /// </summary>
    Standard = 1 << 0,  // 1

    /// <summary>
    /// Travail pendant le jour de repos hebdomadaire.
    /// Déterminé par WorkingCalendar (IsWorkingDay = false).
    /// Exemple : travail le dimanche.
    /// </summary>
    WeeklyRest = 1 << 1,  // 2

    /// <summary>
    /// Travail pendant un jour férié officiel.
    /// Déterminé par la table Holiday.
    /// Exemple : travail le 1er Mai, Aïd al-Fitr.
    /// </summary>
    PublicHoliday = 1 << 2,  // 4

    /// <summary>
    /// Travail de nuit (tranche 21h–6h selon législation marocaine).
    /// Peut se combiner avec WeeklyRest ou PublicHoliday.
    /// </summary>
    Night = 1 << 3,  // 8

    /// <summary>
    /// Combinaison Férié OU Repos hebdomadaire.
    /// Utilisé pour définir des règles communes aux deux cas
    /// sans dupliquer les OvertimeRateRule.
    /// </summary>
    FerieOrRest = PublicHoliday | WeeklyRest  // 6
}

/// <summary>
/// Mode de saisie des heures supplémentaires.
/// Détermine comment la durée est capturée dans EmployeeOvertime.
/// </summary>
public enum OvertimeEntryMode
{
    /// <summary>Saisie avec heure début et heure fin (plus précis)</summary>
    HoursRange = 1,

    /// <summary>Durée saisie directement en heures décimales (ex: 2.5h)</summary>
    DurationOnly = 2,

    /// <summary>Journée complète → utilise la durée standard du WorkingCalendar</summary>
    FullDay = 3
}

/// <summary>
/// Statut workflow des heures supplémentaires.
/// Draft → Submitted → Approved/Rejected → Cancelled
/// </summary>
public enum OvertimeStatus
{
    /// <summary>Brouillon — saisi mais pas encore soumis à validation</summary>
    Draft = 0,

    /// <summary>Soumis par l'employé, en attente de validation manager</summary>
    Submitted = 1,

    /// <summary>Approuvé par le manager, sera inclus dans la paie</summary>
    Approved = 2,

    /// <summary>Rejeté par le manager avec motif</summary>
    Rejected = 3,

    /// <summary>Annulé par l'employé avant ou après approbation</summary>
    Cancelled = 4
}

/// <summary>
/// Type de plage horaire pour les règles OvertimeRateRule.
/// Permet de gérer les cas où le travail traverse minuit.
/// </summary>
public enum TimeRangeType
{
    /// <summary>Règle applicable toute la journée (pas de restriction horaire)</summary>
    AllDay = 0,

    /// <summary>Plage horaire sur le même jour (ex: 20:00–23:00)</summary>
    SameDay = 1,

    /// <summary>
    /// Plage qui traverse minuit (ex: 22:00–02:00).
    /// Nécessite une logique de split dans OvertimeRateRule.
    /// </summary>
    CrossesMidnight = 2
}

/// <summary>
/// Stratégie de cumul quand plusieurs OvertimeRateRule s'appliquent
/// simultanément (ex: nuit + férié).
/// </summary>
public enum MultiplierCumulationStrategy
{
    /// <summary>
    /// Prend uniquement le multiplicateur le plus élevé.
    /// Exemple : nuit (×1.50) + férié (×1.25) → applique ×1.50
    /// </summary>
    TakeMaximum = 1,

    /// <summary>
    /// Multiplie les taux entre eux.
    /// Exemple : ×1.25 × ×1.50 = ×1.875
    /// </summary>
    Multiply = 2,

    /// <summary>
    /// Addition des taux moins 100%.
    /// Exemple : 125% + 150% − 100% = 175% → ×1.75
    /// Formule : (r1 - 1) + (r2 - 1) + 1 = r1 + r2 - 1
    /// </summary>
    AdditiveMinus100 = 3
}
