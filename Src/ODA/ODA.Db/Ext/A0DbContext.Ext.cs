namespace Db.OneDriveAudit.DbModel
{
    public partial class A0DbContext : System.Data.Entity.DbContext
    {
        public A0DbContext()
        {
            Database.Connection.ConnectionString =

#if DEBUG
                @"data source=.\sqlexpress;initial catalog=OneDriveAuditDb.Dbg;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework";
#else
                @"data source=.\sqlexpress;initial catalog=OneDriveAuditDb.V2; integrated security=True;MultipleActiveResultSets=True;App=EntityFramework";
#endif
        }
    }
}
    