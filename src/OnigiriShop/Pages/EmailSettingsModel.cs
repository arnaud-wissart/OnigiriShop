namespace OnigiriShop.Pages
{
    public class EmailSettingsModel
    {
        public string ExpeditorEmail { get; set; } = string.Empty;
        public string ExpeditorName { get; set; } = string.Empty;
        public string InvitationSubject { get; set; } = string.Empty;
        public string InvitationIntro { get; set; } = string.Empty;
        public string PasswordResetSubject { get; set; } = string.Empty;
        public string PasswordResetIntro { get; set; } = string.Empty;
        public string OrderSubject { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
    }
}
