using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqa_core.Models
{
    [Route("api/[controller]")]
    [ApiController]
    public class StateMastersController : ControllerBase

    {
        private readonly AppDbContext _context;

        public StateMastersController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("get-all-states")]
        public async Task<IActionResult> GetAllStates()
        {
            var data = await _context.StateMasters
                .Where(x => x.IsDeleted != true)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return Ok(new { Data = data, Success = true });
        }









        [HttpPost("add-state")]
        public async Task<IActionResult> Upsert([FromBody] StateMaster model)
        {
            // CORRECTED LOGIC: 
            // Check if another state already has this name (ignoring the current state being edited)
            var exists = await _context.StateMasters.AnyAsync(x =>
                x.StateName.ToLower() == model.StateName.ToLower() &&
                x.StateId != model.StateId &&
                x.IsDeleted != true);

            if (exists) return BadRequest(new { Message = "State already exists!", Success = false });

            if (model.StateId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;
                _context.StateMasters.Add(model);

                await _context.SaveChangesAsync();
                return Ok(new { Message = "State Added Successfully", Success = true });
            }
            else
            {
                var dbItem = await _context.StateMasters.FindAsync(model.StateId);
                if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

                // Update the existing item
                dbItem.StateName = model.StateName;
                dbItem.ModifiedDate = DateTime.UtcNow;
                dbItem.ModifiedBy = model.ModifiedBy;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "State Updated Successfully", Success = true });
            }
        }







        [HttpPost("toggle-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] StateMaster model)
        {
            var dbItem = await _context.StateMasters.FindAsync(model.StateId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsActive = !dbItem.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Status Changed", Success = true });
        }



        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] StateMaster model)
        {
            var dbItem = await _context.StateMasters.FindAsync(model.StateId);
            if (dbItem == null) return NotFound(new { Message = "Not Found", Success = false });

            dbItem.IsDeleted = true;
            dbItem.DeletedDate = DateTime.UtcNow;
            dbItem.DeletedBy = model.DeletedBy;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "State Deleted", Success = true });
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



    }
}
