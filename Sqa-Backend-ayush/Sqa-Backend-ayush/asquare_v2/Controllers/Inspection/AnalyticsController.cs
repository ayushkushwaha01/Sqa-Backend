using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sqa_core.Data;
using sqa_core.Models;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace sqa_core.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
{
        private readonly AppDbContext _context;

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }


        //hhhh










        [HttpGet("monthly-error-rates/{year}")]
        public async Task<IActionResult> GetMonthlyErrorRates(int year)
        {
            try
            {
                // 1. Fetch only the necessary fields (Month and DefectRate) for the requested year.
                var rawData = await (from i in _context.Inspections
                                     join r in _context.Inspectionrefs on i.InspectionId equals r.InspectionId
                                     where i.IsDeleted != true
                                        && i.IsArchive != true
                                        && r.IsDeleted != true
                                        && r.DefectRate != null
                                        && i.InspectionDate.HasValue
                                        && i.InspectionDate.Value.Year == year
                                     select new
                                     {
                                         Month = i.InspectionDate.Value.Month,
                                         r.DefectRate
                                     }).ToListAsync();

                // 2. Parse the string defect rates safely into doubles and MULTIPLY BY 1000
                var parsedRates = rawData.Select(x =>
                {
                    string cleanString = x.DefectRate.Replace("%", "").Trim();
                    // Parse the number and multiply by 1000
                    double parsedVal = double.TryParse(cleanString, out double val) ? (val * 1000) : 0.0;
                    return new { x.Month, Rate = parsedVal };
                });

                // 3. Group by month and calculate the average rate
                var monthlyAverages = parsedRates
                    .GroupBy(x => x.Month)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Average(x => x.Rate)
                    );

                // 4. Build the final response, ensuring all 12 months are represented
                var finalData = Enumerable.Range(1, 12).Select(monthNumber =>
                {
                    // Get short month name (e.g., "Jan", "Feb")
                    string monthName = System.Globalization.DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(monthNumber);

                    // Format average rate or default to 0
                    string formattedAverage = monthlyAverages.ContainsKey(monthNumber)
                        ? $"{Math.Round(monthlyAverages[monthNumber], 1)}"
                        : "0";

                    return new
                    {
                        monthNumber = monthNumber,
                        monthName = monthName,
                        averageErrorRate = formattedAverage
                    };
                }).ToList();

                return Ok(new { Data = finalData, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }










        [HttpGet("daily-error-rates/{year}/{month}")]
        public async Task<IActionResult> GetDailyErrorRates(int year, int month)
        {
            try
            {
                // Basic validation
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                // 1. Fetch only the necessary fields (Day and DefectRate) for the requested year and month.
                var rawData = await (from i in _context.Inspections
                                     join r in _context.Inspectionrefs on i.InspectionId equals r.InspectionId
                                     where i.IsDeleted != true
                                        && i.IsArchive != true
                                        && r.IsDeleted != true
                                        && r.DefectRate != null
                                        && i.InspectionDate.HasValue
                                        && i.InspectionDate.Value.Year == year
                                        && i.InspectionDate.Value.Month == month
                                     select new
                                     {
                                         Day = i.InspectionDate.Value.Day,
                                         r.DefectRate
                                     }).ToListAsync();

                // 2. Parse the string defect rates safely into doubles and MULTIPLY BY 1000 (for PPM)
                var parsedRates = rawData.Select(x =>
                {
                    string cleanString = x.DefectRate.Replace("%", "").Trim();
                    // Parse the number and multiply by 1000
                    double parsedVal = double.TryParse(cleanString, out double val) ? (val * 1000) : 0.0;
                    return new { x.Day, Rate = parsedVal };
                });

                // 3. Group by day and calculate the average rate
                var dailyAverages = parsedRates
                    .GroupBy(x => x.Day)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Average(x => x.Rate)
                    );

                // 4. Build the final response, ensuring every day of the month is represented
                int daysInMonth = DateTime.DaysInMonth(year, month);

                var finalData = Enumerable.Range(1, daysInMonth).Select(dayNumber =>
                {
                    // Create a formatted date string for the frontend (e.g., "2026-07-01")
                    string dateString = new DateTime(year, month, dayNumber).ToString("yyyy-MM-dd");

                    // Format average rate or default to "0"
                    string formattedAverage = dailyAverages.ContainsKey(dayNumber)
                        ? $"{Math.Round(dailyAverages[dayNumber], 1)}"
                        : "0";

                    return new
                    {
                        dayNumber = dayNumber,
                        date = dateString,
                        averageErrorRate = formattedAverage
                    };
                }).ToList();

                return Ok(new { Data = finalData, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }








        [HttpGet("monthly-defect-counts/{year}/{month}")]
        public async Task<IActionResult> GetMonthlyDefectCounts(int year, int month)
        {
            try
            {
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                // 1. Fetch the JSON Status strings directly from InspectionDefects based on CreatedDate
                var defectRecords = await _context.InspectionDefects
    .Where(d => d.IsDeleted != true
             && d.CreatedDate.Year == year    // Removed .Value and .HasValue checks
             && d.CreatedDate.Month == month  // Removed .Value
             && !string.IsNullOrEmpty(d.Status))
    .Select(d => d.Status)
    .ToListAsync();

                // 2. Fetch Defect Master data to map IDs to Names
                var defectMasterDict = await _context.DefectMasters
                    .Where(dm => dm.IsDeleted != true)
                    .ToDictionaryAsync(dm => dm.DefectId.ToString(), dm => dm.DefectName);

                // 3. Parse the JSON and aggregate counts in memory
                var defectCounts = new Dictionary<string, int>();

                foreach (var statusJson in defectRecords)
                {
                    try
                    {
                        // Deserialize the Status JSON (e.g., {"2":5, "3":1})
                        var statusDict = JsonSerializer.Deserialize<Dictionary<string, int>>(statusJson);

                        if (statusDict != null)
                        {
                            foreach (var kvp in statusDict)
                            {
                                string defectId = kvp.Key;

                                if (defectCounts.ContainsKey(defectId))
                                {
                                    defectCounts[defectId]++;
                                }
                                else
                                {
                                    defectCounts[defectId] = 1;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore invalid JSON rows without breaking the entire loop
                    }
                }

                // 4. Build final response by mapping the aggregated IDs to their Names
                var finalData = defectCounts.Select(dc => new
                {
                    defectName = defectMasterDict.ContainsKey(dc.Key) ? defectMasterDict[dc.Key] : $"Unknown (ID: {dc.Key})",
                    count = dc.Value
                })
                .OrderByDescending(x => x.count)
                .ToList();

                return Ok(new { Data = finalData, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }













        [HttpGet("monthly-part-family-counts/{year}/{month}")]
        public async Task<IActionResult> GetMonthlyPartFamilyCounts(int year, int month)
        {
            try
            {
                // 1. Basic validation
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                // 2. Perform Join, Filter, Group, and Count directly in the database query
                var partFamilyCounts = await (from i in _context.Inspections
                                              join pf in _context.PartFamilies on i.PartFamilyId equals pf.PartFamilyId
                                              where i.IsDeleted != true
                                                 && i.IsArchive != true
                                                 && pf.IsDeleted != true
                                                 && i.InspectionDate.HasValue
                                                 && i.InspectionDate.Value.Year == year
                                                 && i.InspectionDate.Value.Month == month
                                              group i by pf.PartFamilyName into g
                                              select new
                                              {
                                                  partFamilyName = g.Key ?? "Unknown",
                                                  count = g.Count()
                                              })
                                              .OrderByDescending(x => x.count) // Sort by highest count first
                                              .ToListAsync();

                return Ok(new { Data = partFamilyCounts, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }





        //daily view 


        [HttpGet("hourly-error-rates/{year}/{month}/{day}")]
        public async Task<IActionResult> GetHourlyErrorRates(int year, int month, int day)
        {
            try
            {
                // 1. Validate the date
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                int daysInMonth = DateTime.DaysInMonth(year, month);
                if (day < 1 || day > daysInMonth)
                {
                    return BadRequest(new { Success = false, Message = $"Invalid day. {month}/{year} only has {daysInMonth} days." });
                }

                // 2. Fetch the data for the specific day
                // We pull the raw Time and InspectionDate to memory first to avoid EF Core translation errors
                var rawData = await (from i in _context.Inspections
                                     join r in _context.Inspectionrefs on i.InspectionId equals r.InspectionId
                                     where i.IsDeleted != true
                                        && i.IsArchive != true
                                        && r.IsDeleted != true
                                        && r.DefectRate != null
                                        && i.InspectionDate.HasValue
                                        && i.InspectionDate.Value.Year == year
                                        && i.InspectionDate.Value.Month == month
                                        && i.InspectionDate.Value.Day == day
                                     select new
                                     {
                                         i.Time,
                                         i.InspectionDate,
                                         r.DefectRate
                                     }).ToListAsync();

                // 3. Extract the hour and parse the defect rate to PPM
                var parsedRates = rawData.Select(x =>
                {
                    // Fallback: If Time is null, try to get the hour from InspectionDate
                    int hour = 0;
                    if (x.Time.HasValue)
                    {
                        hour = x.Time.Value.Hours;
                    }
                    else if (x.InspectionDate.HasValue)
                    {
                        hour = x.InspectionDate.Value.Hour;
                    }

                    // Clean the string and multiply by 1000 to get PPM
                    string cleanString = x.DefectRate.Replace("%", "").Trim();
                    double parsedVal = double.TryParse(cleanString, out double val) ? (val * 1000) : 0.0;

                    return new { Hour = hour, Rate = parsedVal };
                });

                // 4. Group by hour and calculate the average
                var hourlyAverages = parsedRates
                    .GroupBy(x => x.Hour)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Average(x => x.Rate)
                    );

                // 5. Build the final response representing all 24 hours (0 to 23)
                var finalData = Enumerable.Range(0, 24).Select(hourNumber =>
                {
                    // Format time nicely for the front end (e.g., "08:00", "14:00")
                    string timeString = $"{hourNumber:D2}:00";

                    // Get the calculated average or default to 0
                    string formattedAverage = hourlyAverages.ContainsKey(hourNumber)
                        ? $"{Math.Round(hourlyAverages[hourNumber], 1)}"
                        : "0";

                    return new
                    {
                        hourNumber = hourNumber,
                        time = timeString,
                        averageErrorRate = formattedAverage
                    };
                }).ToList();

                return Ok(new { Data = finalData, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }




        [HttpGet("shift-error-rates/{year}/{month}/{day}")]
        public async Task<IActionResult> GetShiftErrorRates(int year, int month, int day)
        {
            try
            {
                // 1. Basic Date Validation
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                int daysInMonth = DateTime.DaysInMonth(year, month);
                if (day < 1 || day > daysInMonth)
                {
                    return BadRequest(new { Success = false, Message = $"Invalid day. {month}/{year} only has {daysInMonth} days." });
                }

                // 2. Fetch joined data for the specific day
                // Joining Inspections, InspectionRefs, and SqaLookup on ShiftId
                var rawData = await (from i in _context.Inspections
                                     join r in _context.Inspectionrefs on i.InspectionId equals r.InspectionId
                                     join l in _context.Lookups on i.ShiftId equals l.LookupId
                                     where i.IsDeleted != true
                                        && i.IsArchive != true
                                        && r.IsDeleted != true
                                        && l.IsDeleted != true
                                        && r.DefectRate != null
                                        && i.InspectionDate.HasValue
                                        && i.InspectionDate.Value.Year == year
                                        && i.InspectionDate.Value.Month == month
                                        && i.InspectionDate.Value.Day == day
                                     select new
                                     {
                                         ShiftName = l.LookupName ?? "Unknown Shift",
                                         r.DefectRate
                                     }).ToListAsync();

                // 3. Parse the defect rates into PPM (multiply by 1000)
                var parsedRates = rawData.Select(x =>
                {
                    string cleanString = x.DefectRate.Replace("%", "").Trim();
                    double parsedVal = double.TryParse(cleanString, out double val) ? (val * 1000) : 0.0;

                    return new { x.ShiftName, Rate = parsedVal };
                });

                // 4. Group by ShiftName and calculate the average PPM
                var shiftAverages = parsedRates
                    .GroupBy(x => x.ShiftName)
                    .Select(g => new
                    {
                        shiftName = g.Key,
                        averageErrorRate = $"{Math.Round(g.Average(x => x.Rate), 1)}"
                    })
                    .ToList();

                return Ok(new { Data = shiftAverages, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }









        [HttpGet("daily-defect-counts/{year}/{month}/{day}")]
        public async Task<IActionResult> GetDailyDefectCounts(int year, int month, int day)
        {
            try
            {
                // 1. Basic validation
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                int daysInMonth = DateTime.DaysInMonth(year, month);
                if (day < 1 || day > daysInMonth)
                {
                    return BadRequest(new { Success = false, Message = $"Invalid day. {month}/{year} only has {daysInMonth} days." });
                }

                // 2. Fetch the JSON Status strings directly from InspectionDefects based on exact CreatedDate
                var defectRecords = await _context.InspectionDefects
                    .Where(d => d.IsDeleted != true
                             && d.CreatedDate.Year == year
                             && d.CreatedDate.Month == month
                             && d.CreatedDate.Day == day      // Added filter for the specific day
                             && !string.IsNullOrEmpty(d.Status))
                    .Select(d => d.Status)
                    .ToListAsync();

                // 3. Fetch Defect Master data to map IDs to Names
                var defectMasterDict = await _context.DefectMasters
                    .Where(dm => dm.IsDeleted != true)
                    .ToDictionaryAsync(dm => dm.DefectId.ToString(), dm => dm.DefectName);

                // 4. Parse the JSON and aggregate counts in memory
                var defectCounts = new Dictionary<string, int>();

                foreach (var statusJson in defectRecords)
                {
                    try
                    {
                        // Deserialize the Status JSON (e.g., {"2":5, "3":1})
                        var statusDict = JsonSerializer.Deserialize<Dictionary<string, int>>(statusJson);

                        if (statusDict != null)
                        {
                            foreach (var kvp in statusDict)
                            {
                                string defectId = kvp.Key;

                                if (defectCounts.ContainsKey(defectId))
                                {
                                    // Increment by 1 to count occurrences (rows), 
                                    // ignoring the quantity inside the JSON
                                    defectCounts[defectId]++;
                                }
                                else
                                {
                                    defectCounts[defectId] = 1;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore invalid JSON rows without breaking the entire loop
                    }
                }

                // 5. Build final response by mapping the aggregated IDs to their Names
                var finalData = defectCounts.Select(dc => new
                {
                    defectName = defectMasterDict.ContainsKey(dc.Key) ? defectMasterDict[dc.Key] : $"Unknown (ID: {dc.Key})",
                    count = dc.Value
                })
                .OrderByDescending(x => x.count)
                .ToList();

                return Ok(new { Data = finalData, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }





        [HttpGet("daily-part-family-counts/{year}/{month}/{day}")]
        public async Task<IActionResult> GetDailyPartFamilyCounts(int year, int month, int day)
        {
            try
            {
                // 1. Basic validation
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                int daysInMonth = DateTime.DaysInMonth(year, month);
                if (day < 1 || day > daysInMonth)
                {
                    return BadRequest(new { Success = false, Message = $"Invalid day. {month}/{year} only has {daysInMonth} days." });
                }

                // 2. Perform Join, Filter, Group, and Count directly in the database query
                var partFamilyCounts = await (from i in _context.Inspections
                                              join pf in _context.PartFamilies on i.PartFamilyId equals pf.PartFamilyId
                                              where i.IsDeleted != true
                                                 && i.IsArchive != true
                                                 && pf.IsDeleted != true
                                                 && i.InspectionDate.HasValue
                                                 && i.InspectionDate.Value.Year == year
                                                 && i.InspectionDate.Value.Month == month
                                                 && i.InspectionDate.Value.Day == day // Added day filter
                                              group i by pf.PartFamilyName into g
                                              select new
                                              {
                                                  partFamilyName = g.Key ?? "Unknown",
                                                  count = g.Count()
                                              })
                                              .OrderByDescending(x => x.count) // Sort by highest count first
                                              .ToListAsync();

                return Ok(new { Data = partFamilyCounts, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }




        // Define two routes so Swagger handles the optional path parameter correctly
        [HttpGet("top-defect-counts/{year}/{month}")]
        [HttpGet("top-defect-counts/{year}/{month}/{day}")]
        public async Task<IActionResult> GetTopDefectCounts(int year, int month, int? day)
        {
            try
            {
                // 1. Basic validation for month
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                // Validate the day only if it has been provided
                if (day.HasValue)
                {
                    int daysInMonth = DateTime.DaysInMonth(year, month);
                    if (day.Value < 1 || day.Value > daysInMonth)
                    {
                        return BadRequest(new { Success = false, Message = $"Invalid day. {month}/{year} only has {daysInMonth} days." });
                    }
                }

                // 2. Build the query dynamically based on whether 'day' was provided
                var defectRecordsQuery = _context.InspectionDefects
                    .Where(d => d.IsDeleted != true
                             && d.CreatedDate.Year == year
                             && d.CreatedDate.Month == month
                             && !string.IsNullOrEmpty(d.Status));

                if (day.HasValue)
                {
                    // Apply the day filter only if a day was passed to the API
                    defectRecordsQuery = defectRecordsQuery.Where(d => d.CreatedDate.Day == day.Value);
                }

                // Execute the query and fetch the Status JSON strings
                var defectRecords = await defectRecordsQuery.Select(d => d.Status).ToListAsync();

                // 3. Fetch Defect Master data to map IDs to Names
                var defectMasterDict = await _context.DefectMasters
                    .Where(dm => dm.IsDeleted != true)
                    .ToDictionaryAsync(dm => dm.DefectId.ToString(), dm => dm.DefectName);

                // 4. Parse the JSON and aggregate counts in memory
                var defectCounts = new Dictionary<string, int>();

                foreach (var statusJson in defectRecords)
                {
                    try
                    {
                        // Deserialize the Status JSON (e.g., {"2":5, "3":1})
                        var statusDict = JsonSerializer.Deserialize<Dictionary<string, int>>(statusJson);

                        if (statusDict != null)
                        {
                            foreach (var kvp in statusDict)
                            {
                                string defectId = kvp.Key;

                                if (defectCounts.ContainsKey(defectId))
                                {
                                    defectCounts[defectId]++; // Counting occurrences (rows)
                                }
                                else
                                {
                                    defectCounts[defectId] = 1;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore invalid JSON rows without breaking the entire loop
                    }
                }

                // 5. Build final response, map IDs to Names, sort by highest count, and take TOP 10
                var finalData = defectCounts.Select(dc => new
                {
                    defectName = defectMasterDict.ContainsKey(dc.Key) ? defectMasterDict[dc.Key] : $"Unknown (ID: {dc.Key})",
                    count = dc.Value
                })
                .OrderByDescending(x => x.count)
                .Take(10) // Filters to only return the top 10 results
                .ToList();

                return Ok(new { Data = finalData, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }












        // Define two routes so Swagger handles the optional path parameter correctly
        [HttpGet("top-inspector-counts/{year}/{month}")]
        [HttpGet("top-inspector-counts/{year}/{month}/{day}")]
        public async Task<IActionResult> GetTopInspectorCounts(int year, int month, int? day)
        {
            try
            {
                // 1. Basic validation for month
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { Success = false, Message = "Invalid month. Must be between 1 and 12." });
                }

                // 2. Validate the day only if it has been provided
                if (day.HasValue)
                {
                    int daysInMonth = DateTime.DaysInMonth(year, month);
                    if (day.Value < 1 || day.Value > daysInMonth)
                    {
                        return BadRequest(new { Success = false, Message = $"Invalid day. {month}/{year} only has {daysInMonth} days." });
                    }
                }

                // 3. Build the query dynamically based on whether 'day' was provided
                var query = from i in _context.Inspections
                            join u in _context.Users on i.InspectorId equals u.UserId
                            where i.IsDeleted != true
                               && i.IsArchive != true
                               && u.IsDeleted != true
                               && i.InspectionDate.HasValue
                               && i.InspectionDate.Value.Year == year
                               && i.InspectionDate.Value.Month == month
                            select new { i, u };

                if (day.HasValue)
                {
                    // Apply the day filter only if a day was passed to the API
                    query = query.Where(x => x.i.InspectionDate.Value.Day == day.Value);
                }

                // 4. Group by Inspector (UserName), count, sort, and take Top 10
                var topInspectors = await query
                    .GroupBy(x => x.u.UserName)
                    .Select(g => new
                    {
                        inspectorName = g.Key ?? "Unknown",
                        count = g.Count()
                    })
                    .OrderByDescending(x => x.count)
                    .Take(10) // Filter to return only the top 10 results
                    .ToListAsync();

                return Ok(new { Data = topInspectors, Success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }


    }
}
