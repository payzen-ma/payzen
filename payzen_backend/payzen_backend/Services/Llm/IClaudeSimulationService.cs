namespace payzen_backend.Services.Llm
{
    /// <summary>
    /// Interface pour le service de simulation de Claude, permettant le mocking pour les tests
    /// </summary>
    public interface IClaudeSimulationService
    {
        /// <summary>
        /// Simule des compositions de salaire selon les règles DSL et l'instruction fournie
        /// </summary>
        /// <param name="regleContent">Contenu des règles DSL de calcul de paie</param>
        /// <param name="instruction">Instruction de l'utilisateur (ex: "Je veux un net de 10000 DH")</param>
        /// <param name="cancellationToken">Jeton d'annulation</param>
        /// <returns>Réponse formatée avec les scénarios de paie proposés</returns>
        Task<string> SimulationSalaryAsync(
            string regleContent,
            string instruction,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Simule des compositions de salaire avec HTTP (non-streaming)
        /// </summary>
        /// <param name="regleContent">Contenu des règles DSL de calcul de paie</param>
        /// <param name="instruction">Instruction de l'utilisateur</param>
        /// <param name="cancellationToken">Jeton d'annulation</param>
        /// <returns>Réponse complète de la simulation</returns>
        Task<string> SimulationSalaryStreamAsync(
            string regleContent,
            string instruction,
            CancellationToken cancellationToken = default);
    }
}
