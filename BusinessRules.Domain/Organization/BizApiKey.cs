using BusinessRules.Domain.Common;

namespace BusinessRules.Domain.Organization
{
    public class BizApiKey: IHaveAnId
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid TopLevelFieldId { get; set; }
        public string Description { get; set; }
        public string AllowedDomains { get; set; }
        public string FtpsServer { get; set; }
        public string FtpsPort { get; set; }
        public string FtpsUsername { get; set; }
        public string FtpsPassword { get; set; }
        public string FtpsRemoteDirectory { get; set; }
        public string LocalLoggingDirectory { get; set; }
        public bool AllowUpdateBizField { get; set; }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public BizApiKey()
        {
            
        }

        public BizApiKey(Guid companyId, Guid topLevelFieldId)
        {
            Id = Guid.NewGuid();
            CompanyId = companyId;
            TopLevelFieldId = topLevelFieldId;
            AllowedDomains = "";
            Description = "";
            FtpsServer = "";
            FtpsPort = "";
            FtpsUsername = "";
            FtpsPassword = "";
            FtpsRemoteDirectory = "";
            LocalLoggingDirectory = "";
        }
    }
}
