using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;

namespace sqa_core.Models
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierMasterController : ControllerBase
    {

        private readonly AppDbContext _context;

        public SupplierMasterController(AppDbContext context)
        {
            _context = context;
        }






        [HttpGet("get-all-suppliers")]
        public async Task<IActionResult> GetAllSuppliers()
        {
            var data = await (
                from s in _context.SupplierMasters
                join st in _context.StateMasters on s.StateId equals st.StateId
                join c in _context.CityMasters on s.CityId equals c.CityId
                where s.IsDeleted != true
                orderby s.CreatedDate descending
                select new
                {
                    s.SupplierId,
                    s.SupplierName,
                    s.ContactPerson,
                    s.StateId,
                    StateName = st.StateName,
                    s.CityId,
                    CityName = c.CityName,
                    s.Address,
                    s.IsActive,
                    s.CreatedBy,
                    s.CreatedDate,
                    s.ModifiedBy,
                    s.ModifiedDate,
                    s.DeletedBy,
                    s.DeletedDate,
                    s.IsDeleted
                }).ToListAsync();

            return Ok(new { Data = data, Success = true });
        }



        //[HttpPost("add-supplier")]
        //public async Task<IActionResult> AddSupplier([FromBody] SupplierMaster model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(new { Success = false, Message = "Invalid Data" });

        //    // Check if it's a new record or an update
        //    if (model.SupplierId == 0)
        //    {
        //        // --- ADD LOGIC ---
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;
        //        model.IsDeleted = false;

        //        _context.SupplierMasters.Add(model);
        //    }
        //    else
        //    {
        //        // --- UPDATE LOGIC ---
        //        model.ModifiedDate = DateTime.Now;

        //        // Update() tells Entity Framework to generate an UPDATE statement 
        //        // instead of an INSERT statement for this record.
        //        _context.SupplierMasters.Update(model);
        //    }

        //    try
        //    {
        //        await _context.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            Success = true,
        //            Message = model.SupplierId == 0 ? "Supplier added successfully." : "Supplier updated successfully."
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Good practice: Catch any db save errors so your API doesn't crash completely
        //        return StatusCode(500, new { Success = false, Message = "Database error: " + ex.Message });
        //    }
        //}





        [HttpPost("add-supplier")]
        public async Task<IActionResult> AddSupplier([FromBody] SupplierMaster model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Success = false, Message = "Invalid Data" });

            try
            {
                if (model.SupplierId == 0)
                {
                    // --- ADD LOGIC ---
                    model.CreatedDate = DateTime.Now;
                    model.IsActive = true;
                    model.IsDeleted = false;

                    _context.SupplierMasters.Add(model);
                }
                else
                {
                    // --- UPDATE LOGIC (The Bulletproof Way) ---

                    // 1. Fetch the existing record from the DB
                    var existingSupplier = await _context.SupplierMasters.FindAsync(model.SupplierId);

                    if (existingSupplier == null)
                        return NotFound(new { Success = false, Message = "Supplier not found." });

                    // 2. Map ONLY the properties you want to update from the frontend
                    existingSupplier.SupplierName = model.SupplierName;
                    existingSupplier.ContactPerson = model.ContactPerson;
                    existingSupplier.StateId = model.StateId;
                    existingSupplier.CityId = model.CityId;
                    existingSupplier.Address = model.Address;
                    existingSupplier.IsActive = model.IsActive;

                    // 3. Update the tracking fields (Leave CreatedDate alone!)
                    existingSupplier.ModifiedDate = DateTime.Now;

                    // Note: We don't need to call .Update() because existingSupplier is already being tracked by EF!
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = model.SupplierId == 0 ? "Supplier added successfully." : "Supplier updated successfully."
                });
            }
            catch (Exception ex)
            {
                // This will now expose the REAL error if something still goes wrong
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { Success = false, Message = "DB Error: " + errorMessage });
            }
        }







        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] SupplierMaster model)
        {
            var dbItem = await _context.SupplierMasters.FindAsync(model.SupplierId);

            if (dbItem == null)
                return NotFound(new { Message = "Supplier Not Found", Success = false });

            dbItem.IsActive = !dbItem.IsActive;
            dbItem.ModifiedDate = DateTime.Now;
            dbItem.ModifiedBy = model.ModifiedBy;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Supplier Status Changed Successfully",
                Success = true
            });
        }











        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] SupplierMaster model)
        {
            var dbItem = await _context.SupplierMasters.FindAsync(model.SupplierId);
            if (dbItem == null)
                return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = model.DeletedBy;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Supplier Deleted",
                Success = true
            });
        }




    }
}
