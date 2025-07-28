namespace OnigiriShop.Infrastructure
{
    /// <summary>
    /// Configuration pour l'utilisation de Google Drive.
    /// </summary>
    public class DriveConfig
    {
        /// <summary>
        /// Chemin du fichier de crédentials JSON pour le compte de service.
        /// </summary>
        public string CredentialsPath { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant du dossier contenant la sauvegarde distante.
        /// </summary>
        public string FolderId { get; set; } = string.Empty;
    }
}
