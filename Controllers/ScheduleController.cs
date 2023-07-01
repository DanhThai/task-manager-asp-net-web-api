
using System.Security.Claims;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleRepository _scheduleRepository;

        public ScheduleController(IScheduleRepository scheduleRepository)
        {
            _scheduleRepository = scheduleRepository;
        }

        [HttpPost]
        public async Task<IActionResult> Createschedule(ScheduleDto scheduleDto){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _scheduleRepository.CreateScheduleAsync(userId, scheduleDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSchedule(int workspaceId){
            try{  
                var rs = await _scheduleRepository.GetSchedulesByWorkspaceAsync(workspaceId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPatch("{id}", Name = "PatchScheduleById")]
        public async Task<IActionResult> PatchSchedule(int id, int workspaceId, JsonPatchDocument<Schedule> patchSchedule){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _scheduleRepository.PatchScheduleAsync(id, workspaceId, userId, patchSchedule); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPut("{id}", Name = "UpdateScheduleById")]
        public async Task<IActionResult> Updateschedule(int id, ScheduleDto scheduleDto){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _scheduleRepository.UpdateScheduleAsync(id, userId, scheduleDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpDelete("{id}", Name = "DeleteScheduleById")]
        public async Task<IActionResult> DeletescheduleByUser(int id, int workspaceId){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _scheduleRepository.DeleteScheduleAsync(id, workspaceId, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }
    }
}