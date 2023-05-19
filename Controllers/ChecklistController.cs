
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
    public class ChecklistController : ControllerBase
    {
        private readonly IChecklistRepository _checklistRepository;

        public ChecklistController(IChecklistRepository checklistRepository)
        {
            _checklistRepository = checklistRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateChecklist([FromQuery]int WorkspaceId, ChecklistDto checklistDto){
            try{
                if(WorkspaceId > 0 && checklistDto.Id > 0){
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _checklistRepository.CreateChecklistAsync(WorkspaceId, userId, checklistDto); 
                    return Ok(rs);
                }
                return BadRequest();
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPut("{id}", Name = "UpdateChecklistById")]
        public async Task<IActionResult> UpdateChecklist(int id, [FromQuery]int WorkspaceId, ChecklistDto checklistDto){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _checklistRepository.UpdateChecklistAsync(id, WorkspaceId, userId, checklistDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpDelete("{id}", Name = "DeleteChecklistById")]
        public async Task<IActionResult> DeleteChecklistByUser(int id, [FromQuery]int WorkspaceId){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _checklistRepository.DeleteChecklistAsync(id, WorkspaceId, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }
    }
}