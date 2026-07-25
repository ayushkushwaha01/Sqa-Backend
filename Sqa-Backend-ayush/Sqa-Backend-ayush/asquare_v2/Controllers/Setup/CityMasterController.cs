using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;

namespace sqa_core.Models
{


    [Route("api/[controller]")]
    [ApiController]
    public class CityMastersController : ControllerBase

    {
        private readonly AppDbContext _context;

        public CityMastersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get-all-cities")]
        public async Task<IActionResult> GetAllCities()
        {
            var data = await (
                from city in _context.CityMasters
                join state in _context.StateMasters
                    on city.StateId equals state.StateId
                where city.IsDeleted != true && state.IsDeleted != true
                orderby city.CreatedDate descending
                select new
                {
                    city.CityId,
                    city.CityName,
                    city.StateId,
                    StateName = state.StateName,
                    city.IsActive,
                    city.CreatedDate
                }
            ).ToListAsync();

            return Ok(new
            {
                Data = data,
                Success = true
            });
        }


        [HttpPost("add-city")]
        public async Task<IActionResult> AddCity([FromBody] CityMaster model)
        {
            if (model == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid data provided." });
            }

            try
            {
                // Check if city already exists in that state (optional but recommended)
                var exists = await _context.CityMasters
                    .AnyAsync(x => x.CityName.ToLower() == model.CityName.ToLower()
                                && x.StateId == model.StateId
                                && x.CityId != model.CityId
                                && x.IsDeleted != true);

                if (exists)
                {
                    return Ok(new { Success = false, Message = "City already exists in this state." });
                }

                if (model.CityId == 0)
                {
                    // Add new city
                    model.CreatedDate = DateTime.Now;
                    _context.CityMasters.Add(model);
                    await _context.SaveChangesAsync();
                    return Ok(new { Success = true, Message = "City added successfully." });
                }
                else
                {
                    // Update existing city
                    var existingCity = await _context.CityMasters.FindAsync(model.CityId);
                    if (existingCity == null) return NotFound(new { Success = false, Message = "City not found." });

                    existingCity.CityName = model.CityName;
                    existingCity.StateId = model.StateId;
                    existingCity.IsActive = model.IsActive;
                    existingCity.ModifiedDate = DateTime.Now;
                    // existingCity.ModifiedBy = model.ModifiedBy; // Add if you have auth context

                    _context.CityMasters.Update(existingCity);
                    await _context.SaveChangesAsync();

                    return Ok(new { Success = true, Message = "City updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while saving the city.", Error = ex.Message });
            }
        }


        [HttpPost("toggle-city")]
        public async Task<IActionResult> ToggleStatus([FromBody] CityMaster model)
        {
            var dbItem = await _context.CityMasters.FindAsync(model.CityId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Status Changed", Success = true });
        }


        [HttpPost("delete-city")]
        public async Task<IActionResult> Delete([FromBody] CityMaster model)
        {
            var dbItem = await _context.CityMasters.FindAsync(model.CityId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = model.DeletedBy;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "City Deleted", Success = true });
        }





    }

}
