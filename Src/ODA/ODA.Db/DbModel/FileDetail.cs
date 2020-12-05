namespace Db.OneDriveAudit.DbModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("a00.FileDetail")]
    public partial class FileDetail
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FileDetail()
        {
            FileDetail1 = new HashSet<FileDetail>();
        }

        public int ID { get; set; }

        public int? MasterID { get; set; }

        public DateTime AddedByAuditTimeID { get; set; }

        [Required]
        [StringLength(256)]
        public string FileName { get; set; }

        [Required]
        [StringLength(64)]
        public string FileExtn { get; set; }

        [Required]
        [StringLength(256)]
        public string FilePath { get; set; }

        public long FileSize { get; set; }

        public DateTime FileCreated { get; set; }

        [StringLength(128)]
        public string FileHash { get; set; }

        [Column(TypeName = "date")]
        public DateTime LastSeen { get; set; }

        public string Note { get; set; }

        public string Metadata { get; set; }

        public DbGeography Location { get; set; }

        public double? LocLat { get; set; }

        public double? LocLon { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public virtual AuditHist AuditHist { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<FileDetail> FileDetail1 { get; set; }

        public virtual FileDetail FileDetail2 { get; set; }
    }
}
