using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubtaskController : ControllerBase
    {
        private readonly ISubtaskRepository _subtaskRepository;

        public SubtaskController(ISubtaskRepository subtaskRepository)
        {
            _subtaskRepository = subtaskRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubtask([FromQuery]int WorkspaceId, SubtaskDto subtaskDto){
            try{
                if(WorkspaceId > 0 && subtaskDto.ChecklistId > 0){
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _subtaskRepository.CreateSubtaskAsync(WorkspaceId, userId, subtaskDto); 
                    return Ok(rs);
                }
                return BadRequest();
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPut("{id}", Name = "UpdateSubtaskById")]
        public async Task<IActionResult> UpdateSubtask(int id, [FromQuery]int WorkspaceId, SubtaskDto subtaskDto){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _subtaskRepository.UpdateSubtaskAsync(id, WorkspaceId, userId, subtaskDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPatch("{id}", Name = "PatchSubtaskById")]
        public async Task<IActionResult> PatchSubtaskByUser(int id, [FromQuery]int workspaceId, [FromBody] JsonPatchDocument<Subtask> patchSubtask){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _subtaskRepository.PatchSubtaskAsync(id, workspaceId, userId, patchSubtask); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpDelete("{id}", Name = "DeleteSubtaskById")]
        public async Task<IActionResult> DeleteSubtaskByUser(int id, [FromQuery]int WorkspaceId){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _subtaskRepository.DeleteSubtaskAsync(id, WorkspaceId, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }
    }
}