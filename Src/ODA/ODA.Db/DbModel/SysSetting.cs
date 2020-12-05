namespace Db.OneDriveAudit.DbModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("cfg.SysSetting")]
    public partial class SysSetting
    {
        [Key]
        [StringLength(500)]
        public string SSName { get; set; }

        [StringLength(50)]
        public string SSDatatype { get; set; }

        [Required]
        public string SSValue { get; set; }

        [StringLength(2000)]
        public string Descrn { get; set; }

        public byte[] BValue { get; set; }
    }
}
