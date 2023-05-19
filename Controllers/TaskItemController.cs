
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaskItemController : ControllerBase
    {
        private readonly ITaskItemRepository _taskItemRepository;

        public TaskItemController(ITaskItemRepository taskItemRepository)
        {
            _taskItemRepository = taskItemRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTaskItem([FromQuery]int WorkspaceId, TaskItemDto taskItemDto){
            try{
                if(WorkspaceId > 0){
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _taskItemRepository.CreateTaskItemAsync(WorkspaceId, userId, taskItemDto); 
                    return Ok(rs);
                }
                return BadRequest();
            }
            catch{
                return BadRequest();
            }
        }

        [HttpGet("{id}", Name = "GetTaskItemById")]
        public async Task<IActionResult> GetTaskItemById(int id){
            if (id != 0)
            {
                try{
                    var rs = await _taskItemRepository.GetTaskItemByIdAsync(id); 
                    return Ok(rs);
                }
                catch{
                    return NotFound();
                }
            }
            return BadRequest();
        }

        [HttpPut("{id}", Name = "UpdateTaskItemById")]
        public async Task<IActionResult> UpdateTaskItemByUser(int id, [FromQuery]int WorkspaceId, TaskItemDto taskItemDto){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.UpdateTaskItemAsync(id, WorkspaceId, userId, taskItemDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPatch("{id}", Name = "PatchTaskItemById")]
        public async Task<IActionResult> PatchTaskItemByUser(int id, [FromQuery]int workspaceId, [FromBody] JsonPatchDocument<TaskItem> patchTaskItem){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.PatchTaskItemAsync(id, workspaceId, userId, patchTaskItem); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPost("{id}/MoveTask")]
        public async Task<IActionResult> MoveTaskItemByUser(int id, [FromQuery]int WorkspaceId,  MoveTaskDto moveTaskDto){
            try{  
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.MoveTaskItemAsync(id, WorkspaceId, userId, moveTaskDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPost("{id}/upload-file")]
        public async Task<IActionResult> UploadFile(int id, [FromQuery]int WorkspaceId,  [FromForm] IFormFile file){
            if (file!=null){
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.UploadFileAsync(id, WorkspaceId, userId, file);
                return Ok(rs);
            }
            return BadRequest();
        }

        [HttpDelete("{id}", Name = "DeleteTaskItemById")]
        public async Task<IActionResult> DeleteTaskItemByUser(int id, [FromQuery]int WorkspaceId){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.DeleteTaskItemAsync(id, WorkspaceId, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

    }
}