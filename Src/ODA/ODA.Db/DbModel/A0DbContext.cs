namespace Db.OneDriveAudit.DbModel
{
	using System;
	using System.Data.Entity;
	using System.ComponentModel.DataAnnotations.Schema;
	using System.Linq;

	public partial class A0DbContext : DbContext
	{
		//public A0DbContext()
		//		: base("name=A0DbContext")
		//{
		//}

		public virtual DbSet<AuditHist> AuditHists { get; set; }
		public virtual DbSet<FileDetail> FileDetails { get; set; }
		public virtual DbSet<SysSetting> SysSettings { get; set; }
		public virtual DbSet<FileExtn> FileExtns { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<AuditHist>()
					.Property(e => e.Note)
					.IsUnicode(false);

			modelBuilder.Entity<AuditHist>()
					.Property(e => e.PathRoot)
					.IsUnicode(false);

			modelBuilder.Entity<AuditHist>()
					.HasMany(e => e.FileDetails)
					.WithRequired(e => e.AuditHist)
					.HasForeignKey(e => e.AddedByAuditTimeID)
					.WillCascadeOnDelete(false);

			modelBuilder.Entity<FileDetail>()
					.Property(e => e.FileName)
					.IsUnicode(false);

			modelBuilder.Entity<FileDetail>()
					.Property(e => e.FileExtn)
					.IsUnicode(false);

			modelBuilder.Entity<FileDetail>()
					.Property(e => e.FilePath)
					.IsUnicode(false);

			modelBuilder.Entity<FileDetail>()
					.Property(e => e.FileHash)
					.IsUnicode(false);

			modelBuilder.Entity<FileDetail>()
					.Property(e => e.Note)
					.IsUnicode(false);

			modelBuilder.Entity<FileDetail>()
					.Property(e => e.Metadata)
					.IsUnicode(false);

			modelBuilder.Entity<FileDetail>()
					.HasMany(e => e.FileDetail1)
					.WithOptional(e => e.FileDetail2)
					.HasForeignKey(e => e.MasterID);

			modelBuilder.Entity<SysSetting>()
					.Property(e => e.SSName)
					.IsUnicode(false);

			modelBuilder.Entity<SysSetting>()
					.Property(e => e.SSDatatype)
					.IsUnicode(false);

			modelBuilder.Entity<SysSetting>()
					.Property(e => e.SSValue)
					.IsUnicode(false);

			modelBuilder.Entity<SysSetting>()
					.Property(e => e.Descrn)
					.IsUnicode(false);

			modelBuilder.Entity<FileExtn>()
					.Property(e => e.ExtnID)
					.IsUnicode(false);

			modelBuilder.Entity<FileExtn>()
					.Property(e => e.Descrn)
					.IsUnicode(false);
		}
	}
}
