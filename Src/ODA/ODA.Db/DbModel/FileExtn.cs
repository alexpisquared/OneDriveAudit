namespace Db.OneDriveAudit.DbModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("lku.FileExtn")]
    public partial class FileExtn
    {
        [Key]
        [StringLength(64)]
        public string ExtnID { get; set; }

        [StringLength(1000)]
        public string Descrn { get; set; }
    }
}
