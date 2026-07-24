namespace SalesmessageExporter.Models
{
    public class ExportMember
    {
        public long Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string RoleLabel { get; set; }
        public string Timezone { get; set; }
        public string UserNumber { get; set; }
        public bool? Disabled { get; set; }
        public bool TwoFaEnabled { get; set; }
        public string CreatedAt { get; set; }
        public ExportMemberNumber Number { get; set; }
    }

    public class ExportMemberNumber
    {
        public string FormattedNumber { get; set; }
        public bool IsLandline { get; set; }
        public bool IsTollFree { get; set; }
        public bool Voice { get; set; }
    }
}
