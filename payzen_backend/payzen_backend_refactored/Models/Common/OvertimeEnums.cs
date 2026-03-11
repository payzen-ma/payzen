namespace payzen_backend.Models.Common.OvertimeEnums
{
    /// <summary>
    /// Type d'heures suppl�mentaires
    /// </summary>
    [Flags]
    public enum OvertimeType
    {
        /// <summary>
        /// Aucun (valeur par d�faut)
        /// </summary>
        None = 0,

        /// <summary>
        /// Heures suppl�mentaires normales (jours ouvrables standards)
        /// Exemple : prolongement de la journ�e normale
        /// </summary>
        Standard = 1 << 0,  // 1

        /// <summary>
        /// Travail pendant jour de repos hebdomadaire
        /// D�termin� par WorkingCalendar (IsWorkingDay = false)
        /// Exemple : travail le dimanche
        /// </summary>
        WeeklyRest = 1 << 1,  // 2

        /// <summary>
        /// Travail pendant jour f�ri� officiel
        /// D�termin� par Holiday table (Scope: Global ou Company)
        /// Exemple : travail le 1er Mai, A�d al-Fitr
        /// </summary>
        PublicHoliday = 1 << 2,  // 4

        /// <summary>
        /// Travail de nuit (tranche horaire sp�cifique)
        /// G�n�ralement 21h-6h selon l�gislation marocaine
        /// </summary>
        Night = 1 << 3,  // 8

        /// <summary>
        /// Combinaison : F�ri� OU Repos (pour r�gles communes)
        /// </summary>
        FerieOrRest = PublicHoliday | WeeklyRest  // 6
    }

    /// <summary>
    /// Mode de saisie des heures suppl�mentaires
    /// </summary>
    public enum OvertimeEntryMode
    {
        /// <summary>
        /// Plage horaire avec heure d�but/fin
        /// </summary>
        HoursRange = 1,

        /// <summary>
        /// Dur�e saisie manuellement (en heures d�cimales)
        /// </summary>
        DurationOnly = 2,

        /// <summary>
        /// Journ�e compl�te (utilise dur�e standard entreprise)
        /// </summary>
        FullDay = 3
    }

    /// <summary>
    /// Statut workflow des heures suppl�mentaires
    /// </summary>
    public enum OvertimeStatus
    {
        /// <summary>
        /// Brouillon (pas encore soumis)
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Soumis pour approbation
        /// </summary>
        Submitted = 1,

        /// <summary>
        /// Approuv� par le manager
        /// </summary>
        Approved = 2,

        /// <summary>
        /// Rejet� par le manager
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// Annul� par l'employ�
        /// </summary>
        Cancelled = 4
    }

    /// <summary>
    /// Type de plage horaire pour les r�gles
    /// </summary>
    public enum TimeRangeType
    {
        /// <summary>
        /// Toute la journ�e (pas de restriction horaire)
        /// </summary>
        AllDay = 0,

        /// <summary>
        /// Plage horaire simple (m�me jour)
        /// </summary>
        SameDay = 1,

        /// <summary>
        /// Plage traversant minuit (ex: 22:00-02:00)
        /// </summary>
        CrossesMidnight = 2
    }

    /// <summary>
    /// Strat�gie de cumul de r�gles multiples
    /// </summary>
    public enum MultiplierCumulationStrategy
    {
        /// <summary>
        /// Prend le multiplicateur maximum
        /// </summary>
        TakeMaximum = 1,

        /// <summary>
        /// Multiplie les taux (ex: 1.25 � 1.50 = 1.875)
        /// </summary>
        Multiply = 2,

        /// <summary>
        /// Additionne les taux - 100% (ex: 125% + 150% - 100% = 175%)
        /// </summary>
        AdditiveMinus100 = 3
    }
}