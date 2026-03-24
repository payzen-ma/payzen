using System;
using System.Collections.Generic;
using System.Text;

namespace Payzen.Domain.Common
{
    /// <summary>
    /// Class de base pour toutes les entités du domaine Payzen.
    /// Fournit les champs d'audit (Création, modification, suppression douce)
    /// communs à toutes les tables de la base de données.
    /// </summary>
    public class BaseEntity
    {
        public int Id { get; set; }  // Int vs Guid ( int gagne en rapidité pour les jointure compliqué)
        public DateTimeOffset CreatedAt { get; set; } // Date de création de l'entité
        public int CreatedBy { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; } // Date de dernière modification de l'entité
        public int? UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; } // Date de suppression douce de l'entité
        public int? DeletedBy { get; set; }
    }
}
