namespace Zyan.Communication
{
    /// <summary>
    /// Aufzählung der verfügbaren Aktivierungsarten.
    /// </summary>
    public enum ActivationType : short
    {
        /// <summary>
        /// Komponenteninstanz lebt nur einen Aufruf lang. Für jeden Aufruf wird eine separate Instanz erzeugt.
        /// <remarks>SingleCallaktivierte Komponenten müssen nicht threadsicher sein.</remarks>
        /// </summary>
        SingleCall = 1,
        /// <summary>
        /// Komponenteninstanz wird bei erstem Aufruf erzeugt und wird für alle weiteren Aufrufe wiederverwendet.
        /// <remarks>Singltonaktivierte Komponenten müssen threadsicher sein.</remarks>
        /// </summary>
        Singleton
    }
}