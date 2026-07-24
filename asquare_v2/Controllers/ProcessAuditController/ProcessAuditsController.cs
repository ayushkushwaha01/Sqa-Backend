using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System.Security.Claims;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessAuditsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProcessAuditsController(AppDbContext context) { _context = context; }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userIdClaim) ? 0 : long.Parse(userIdClaim);
        }



        //[HttpGet("get-all")]
        //public async Task<IActionResult> GetAll()
        //{
        //    var data = await (from a in _context.ProcessAudits
        //                      join c in _context.CommodityMasters on a.CommodityId equals c.CommodityId into ac
        //                      from c in ac.DefaultIfEmpty()
        //                      join s in _context.SupplierMasters on a.SupplierId equals s.SupplierId into asup
        //                      from s in asup.DefaultIfEmpty()
        //                          // 🔥 Added join for StateMasters
        //                      join st in _context.StateMasters on a.StateId equals st.StateId into ast
        //                      from st in ast.DefaultIfEmpty()
        //                      join ct in _context.CityMasters on a.CityId equals ct.CityId into act
        //                      from ct in act.DefaultIfEmpty()
        //                      join u in _context.Users on a.AuditorId equals u.UserId into au
        //                      from u in au.DefaultIfEmpty()
        //                      join l in _context.Lookups on a.StatusId equals l.LookupId into al
        //                      from l in al.DefaultIfEmpty()
        //                      where a.IsDeleted != true
        //                      orderby a.CreatedDate descending
        //                      select new
        //                      {
        //                          a.ProcessAuditId,
        //                          a.AuditReference,
        //                          a.CommodityId,
        //                          CommodityName = c != null ? c.Name : "",
        //                          a.SupplierId,
        //                          SupplierName = s != null ? s.SupplierName : "",
        //                          a.StateId,
        //                          StateName = st != null ? st.StateName : "", // 🔥 Now returns StateName
        //                          a.CityId,
        //                          CityName = ct != null ? ct.CityName : "",
        //                          a.AuditorId,
        //                          AuditorName = u != null ? u.UserName : "",
        //                          a.AuditDate,
        //                          a.Remarks,
        //                          a.StatusId,
        //                          a.IsDone,
        //                          a.CAPA,
        //                          a.Report
        //                      }).ToListAsync();

        //    return Ok(new { Data = data, Success = true });
        //}


        //[HttpGet("get-all")]
        //public async Task<IActionResult> GetAll()
        //{
        //    var data = await (from a in _context.ProcessAudits
        //                          // Join with CAPA table to calculate counts
        //                      join c in _context.ProcessAuditCAPAs.Where(x => x.IsDeleted != true)
        //                           on a.ProcessAuditId equals c.ProcessAuditId into acapa

        //                      let totalFailed = acapa.Count(x => x.Compliance == "Fail")
        //                      let totalResolved = acapa.Count(x => x.Compliance == "Fail" && x.IsResolved == true)

        //                      join c in _context.CommodityMasters on a.CommodityId equals c.CommodityId into ac
        //                      from c in ac.DefaultIfEmpty()
        //                      join s in _context.SupplierMasters on a.SupplierId equals s.SupplierId into asup
        //                      from s in asup.DefaultIfEmpty()
        //                      join st in _context.StateMasters on a.StateId equals st.StateId into ast
        //                      from st in ast.DefaultIfEmpty()
        //                      join ct in _context.CityMasters on a.CityId equals ct.CityId into act
        //                      from ct in act.DefaultIfEmpty()
        //                      join u in _context.Users on a.AuditorId equals u.UserId into au
        //                      from u in au.DefaultIfEmpty()
        //                      join l in _context.Lookups on a.StatusId equals l.LookupId into al
        //                      from l in al.DefaultIfEmpty()
        //                      where a.IsDeleted != true
        //                      orderby a.CreatedDate descending
        //                      select new
        //                      {
        //                          a.ProcessAuditId,
        //                          a.AuditReference,
        //                          a.CommodityId,
        //                          CommodityName = c != null ? c.Name : "",
        //                          a.SupplierId,
        //                          SupplierName = s != null ? s.SupplierName : "",
        //                          a.StateId,
        //                          StateName = st != null ? st.StateName : "",
        //                          a.CityId,
        //                          CityName = ct != null ? ct.CityName : "",
        //                          a.AuditorId,
        //                          AuditorName = u != null ? u.UserName : "",
        //                          a.AuditDate,
        //                          a.Remarks,
        //                          a.StatusId,
        //                          a.IsDone,
        //                          a.CAPA,
        //                          a.Report,
        //                          // 🔥 New Property for the Grid
        //                          CapaSummary = totalFailed > 0 ? $"{totalResolved}/{totalFailed}" : "-"
        //                      }).ToListAsync();

        //    return Ok(new { Data = data, Success = true });
        //}

        private string GetCurrentUserType()
        {
            var userTypeClaim = User.FindFirstValue("UserType");
            return string.IsNullOrEmpty(userTypeClaim) ? "Internal" : userTypeClaim;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            long currentUserId = GetCurrentUserId();
            string userType = GetCurrentUserType(); // 🔥 Get the UserType

            var query = from a in _context.ProcessAudits
                        join c in _context.ProcessAuditCAPAs.Where(x => x.IsDeleted != true)
                             on a.ProcessAuditId equals c.ProcessAuditId into acapa
                        let totalFailed = acapa.Count(x => x.Compliance == "Fail")
                        let totalResolved = acapa.Count(x => x.Compliance == "Fail" && x.IsResolved == true)
                        join c in _context.CommodityMasters on a.CommodityId equals c.CommodityId into ac
                        from c in ac.DefaultIfEmpty()
                        join s in _context.SupplierMasters on a.SupplierId equals s.SupplierId into asup
                        from s in asup.DefaultIfEmpty()
                        join st in _context.StateMasters on a.StateId equals st.StateId into ast
                        from st in ast.DefaultIfEmpty()
                        join ct in _context.CityMasters on a.CityId equals ct.CityId into act
                        from ct in act.DefaultIfEmpty()
                        join u in _context.Users on a.AuditorId equals u.UserId into au
                        from u in au.DefaultIfEmpty()
                        join l in _context.Lookups on a.StatusId equals l.LookupId into al
                        from l in al.DefaultIfEmpty()
                        where a.IsDeleted != true
                        select new { a, c, s, st, ct, u, l, totalFailed, totalResolved };

            // 🔥 If logged in as Supplier, only return audits assigned to their SupplierId
            if (userType == "Supplier")
            {
                query = query.Where(x => x.a.SupplierId == currentUserId);
            }

            var data = await query.OrderByDescending(x => x.a.CreatedDate).Select(x => new
            {
                x.a.ProcessAuditId,
                x.a.AuditReference,
                x.a.CommodityId,
                CommodityName = x.c != null ? x.c.Name : "",
                x.a.SupplierId,
                SupplierName = x.s != null ? x.s.SupplierName : "",
                x.a.StateId,
                StateName = x.st != null ? x.st.StateName : "",
                x.a.CityId,
                CityName = x.ct != null ? x.ct.CityName : "",
                x.a.AuditorId,
                AuditorName = x.u != null ? x.u.UserName : "",
                x.a.AuditDate,
                x.a.Remarks,
                x.a.StatusId,
                x.a.IsDone,
                x.a.CAPA,
                x.a.Report,
                CapaSummary = x.totalFailed > 0 ? $"{x.totalResolved}/{x.totalFailed}" : "-"
            }).ToListAsync();

            return Ok(new { Data = data, Success = true });
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] ProcessAudit model)
        {
            long currentUserId = GetCurrentUserId();

            if (model.ProcessAuditId == 0)
            {
                model.CreatedBy = currentUserId;
                model.CreatedDate = DateTime.UtcNow;

                _context.ProcessAudits.Add(model);
                await _context.SaveChangesAsync();

                // Generate Unique Audit Reference: YYYY/Process/0000ID
                model.AuditReference = $"{DateTime.UtcNow.Year}/Process/{model.ProcessAuditId.ToString("D6")}";
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Audit Created Successfully", Success = true });
            }
            else
            {
                var dbItem = await _context.ProcessAudits.FindAsync(model.ProcessAuditId);
                if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

                dbItem.CommodityId = model.CommodityId;
                dbItem.SupplierId = model.SupplierId;
                dbItem.StateId = model.StateId;
                dbItem.CityId = model.CityId;
                dbItem.AuditorId = model.AuditorId;
                dbItem.AuditDate = model.AuditDate;
                dbItem.Remarks = model.Remarks;
                dbItem.StatusId = model.StatusId;
                dbItem.CAPA = model.CAPA;
                dbItem.Report = model.Report;
                dbItem.IsDone = model.IsDone;
                dbItem.ModifiedBy = currentUserId;  
                dbItem.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Audit Updated Successfully", Success = true });
            }
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] ProcessAudit model)
        {
            var dbItem = await _context.ProcessAudits.FindAsync(model.ProcessAuditId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = GetCurrentUserId();
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Audit Deleted", Success = true });
        }




    }
}