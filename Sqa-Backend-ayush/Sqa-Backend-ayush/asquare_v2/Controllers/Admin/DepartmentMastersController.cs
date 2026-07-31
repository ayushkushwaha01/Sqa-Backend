using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentMastersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartmentMastersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.DepartmentMasters
                .Where(x => x.IsDeleted != true)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return Ok(new { Data = data, Success = true });
        }


        [HttpPost("add-department")]
        public async Task<IActionResult> AddDepartment([FromBody] DepartmentMaster model)
        {
            if (model == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid data provided." });
            }

            try
            {
                // Check if department already exists
                var exists = await _context.DepartmentMasters
                    .AnyAsync(x => x.DepartmentName.ToLower() == model.DepartmentName.ToLower()
                                && x.DepartmentId != model.DepartmentId
                                && x.IsDeleted != true);

                if (exists)
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "Department already exists."
                    });
                }

                if (model.DepartmentId == 0)
                {
                    // Add new department
                    model.CreatedDate = DateTime.Now;
                    model.IsActive = true;
                    model.IsDeleted = false;

                    // FIX: Added a temporary ID to satisfy the 'CreatedBy NOT NULL' database constraint
                    model.CreatedBy = 1;

                    _context.DepartmentMasters.Add(model);  
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        Success = true,
                        Message = "Department added successfully."
                    });
                }
                else
                {
                    // Update existing department
                    var existingDepartment = await _context.DepartmentMasters.FindAsync(model.DepartmentId);

                    if (existingDepartment == null)
                    {
                        return NotFound(new
                        {
                            Success = false,
                            Message = "Department not found."
                        });
                    }

                    existingDepartment.DepartmentName = model.DepartmentName;
                    existingDepartment.DepartmentCode = model.DepartmentCode;
                    existingDepartment.DepartmentHead = model.DepartmentHead;
                    existingDepartment.IsActive = model.IsActive;
                    existingDepartment.ModifiedDate = DateTime.Now;

                    // Optional: You can set a temporary ModifiedBy ID here just like we did for CreatedBy
                    existingDepartment.ModifiedBy = model.ModifiedBy ?? 1;

                    _context.DepartmentMasters.Update(existingDepartment);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        Success = true,
                        Message = "Department updated successfully."
                    });
                }
            }
            catch (Exception ex)
            {
                // FIX: Extract the inner exception so you can actually read the SQL error in Angular
                var exactError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred while saving the department.",
                    Error = exactError
                });
            }
        }


        [HttpPost("toggle-department")]
        public async Task<IActionResult> ToggleStatus([FromBody] DepartmentMaster model)
        {
            var dbItem = await _context.DepartmentMasters.FindAsync(model.DepartmentId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Status Changed", Success = true });
        }




        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DepartmentMaster model)
        {
            var dbItem = await _context.DepartmentMasters.FindAsync(model.DepartmentId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            // Soft Delete
            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = model.DeletedBy;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "User Deleted", Success = true });
        }







    }
}
