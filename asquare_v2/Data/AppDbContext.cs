using Microsoft.EntityFrameworkCore;
using sqa_core.Models;
//using asquare_v2.CustomModels;
using System.Diagnostics;
using System.Linq;
//using asquare_v2.CustomModels.Masters;

namespace sqa_core.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
            
        //public DbSet<User> Users { get; set; }

        public DbSet<RoleMaster> RoleMasters { get; set; }
        public DbSet<UserMaster> Users { get; set; }
        public DbSet<ProcessCategory> ProcessCategories { get; set; }
        public DbSet<Checklist> Checklists { get; set; }
        public DbSet<CommodityMaster> CommodityMasters { get; set; }
        public DbSet<CommodityTarget> CommodityTargets { get; set; }

        public DbSet<CodeMaster> CodeMasters { get; set; }

        public DbSet<Lookup> Lookups { get; set; }

        public DbSet<StateMaster> StateMasters { get; set; }

        public DbSet<CityMaster> CityMasters { get; set; }

        public DbSet<SupplierMaster> SupplierMasters { get; set; }

        public DbSet<DepartmentMaster> DepartmentMasters { get; set; }

        public DbSet<PartsAudit> PartsAuditCategories { get; set; }

        public DbSet<PartFamilyModel> PartFamilies { get; set; }
        public DbSet<ParameterModel> Parameters { get; set; }
        public DbSet<PartMaster> PartMasters { get; set; }
        public DbSet<BatchMaster> BatchMasters { get; set; }
        public DbSet<DefectMaster> DefectMasters { get; set; }

        public DbSet<Inspection> Inspections { get; set; }




        public DbSet<ProcessAudit> ProcessAudits { get; set; }

        public DbSet<SeverityMaster> SeverityMasters { get; set; }

        public DbSet<ProcessAuditCAPA> ProcessAuditCAPAs { get; set; }

        // parts Audit

        public DbSet<PartsAudits> PartsAudits { get; set; }
        public DbSet<PartsAuditParameter> PartsAuditParameters { get; set; }
        public DbSet<UserGridSetting> UserGridSettings { get; set; }

        public DbSet<InspectionRef> Inspectionrefs { get; set; }


        public DbSet<InspectionCapa> InspectionCapas { get; set; }

 public DbSet<InspectionDefects> InspectionDefects { get; set; }

        // PARTS ADUIT INNER SCREEN

        public DbSet<PartsAuditCapa> PartsAuditCapas { get; set; }
        public DbSet<PartsAuditCapaDoc> PartsAuditCapaDocs { get; set; }

        public DbSet<PartsAuditCapaImage> PartsAuditCapaImages { get; set; }
       













    }
}