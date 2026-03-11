using System;
using System.Collections.Generic;

namespace payzen_backend.Services.Company.Defaults.Catalog
{
    /// <summary>
    /// Catalogue du calendrier de travail par défaut (semaine type Maroc : Lun–Ven 9h–18h).
    /// DayOfWeek : 0 = Dimanche, 1 = Lundi, ..., 6 = Samedi.
    /// </summary>
    public static class DefaultWorkingCalendar
    {
        public sealed class DayDefinition
        {
            public int DayOfWeek { get; init; }
            public bool IsWorkingDay { get; init; }
            public TimeSpan? StartTime { get; init; }
            public TimeSpan? EndTime { get; init; }
        }

        /// <summary>
        /// Retourne les 7 jours de la semaine. Par défaut : Lundi–Vendredi travaillés 09:00–18:00, Samedi et Dimanche chômés.
        /// </summary>
        public static IReadOnlyList<DayDefinition> GetDefaults()
        {
            var start = new TimeSpan(9, 0, 0);
            var end = new TimeSpan(18, 0, 0);
            return new List<DayDefinition>
            {
                new() { DayOfWeek = 0, IsWorkingDay = false, StartTime = null, EndTime = null },   // Dimanche
                new() { DayOfWeek = 1, IsWorkingDay = true, StartTime = start, EndTime = end },    // Lundi
                new() { DayOfWeek = 2, IsWorkingDay = true, StartTime = start, EndTime = end },     // Mardi
                new() { DayOfWeek = 3, IsWorkingDay = true, StartTime = start, EndTime = end },    // Mercredi
                new() { DayOfWeek = 4, IsWorkingDay = true, StartTime = start, EndTime = end },    // Jeudi
                new() { DayOfWeek = 5, IsWorkingDay = true, StartTime = start, EndTime = end },    // Vendredi
                new() { DayOfWeek = 6, IsWorkingDay = false, StartTime = null, EndTime = null }   // Samedi
            };
        }
    }
}
