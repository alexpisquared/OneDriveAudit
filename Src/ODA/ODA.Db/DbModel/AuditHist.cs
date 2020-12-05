namespace Db.OneDriveAudit.DbModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("a00.AuditHist")]
    public partial class AuditHist
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public AuditHist()
        {
            FileDetails = new HashSet<FileDetail>();
        }

        [Key]
        public DateTime AuditTimeID { get; set; }

        public int? TotalFound { get; set; }

        public int? TotalAdded { get; set; }

        public int? TotalHashed { get; set; }

        public int? FoundMissing { get; set; }

        public int? FoundRenamed { get; set; }

        public string Note { get; set; }

        [StringLength(256)]
        public string PathRoot { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<FileDetail> FileDetails { get; set; }
    }
}
