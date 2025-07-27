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
    }
}
