namespace OnigiriShop.Infrastructure;

/// <summary>
/// Configuration de la sauvegarde via GitHub.
/// </summary>
public class GitHubBackupConfig
{
    /// <summary>
    /// Jeton d'accès personnel pour GitHub.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Propriétaire du dépôt.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Nom du dépôt.
    /// </summary>
    public string Repo { get; set; } = string.Empty;

    /// <summary>
    /// Chemin du fichier dans le dépôt.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Branche cible (par défaut "main").
    /// </summary>
    public string Branch { get; set; } = "main";
}