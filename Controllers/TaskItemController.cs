
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
    // [Authorize]
    public class TaskItemController : ControllerBase
    {
        private readonly ITaskItemRepository _taskItemRepository;

        public TaskItemController(ITaskItemRepository taskItemRepository)
        {
            _taskItemRepository = taskItemRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTaskItem(TaskItemDto taskItemDto){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.CreateTaskItemAsync(taskItemDto, userId); 
                return Ok(rs);
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
        public async Task<IActionResult> UpdateTaskItemByUser(TaskItemDto TaskItemDto, int id){
            try{      
                var rs = await _taskItemRepository.UpdateTaskItemAsync(TaskItemDto, id); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPatch("{id}", Name = "PatchTaskItemById")]
        public async Task<IActionResult> PatchTaskItemByUser(JsonPatchDocument<TaskItem> patchTaskItem, int id){
            try{      
                var rs = await _taskItemRepository.PatchTaskItemAsync(patchTaskItem, id); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }


        [HttpPost("{id}/upload-file")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, int id){
            if (file!=null){
                var rs = await _taskItemRepository.UploadFileAsync(id, file);
                return Ok(rs);
            }
            return BadRequest();
        }

        [HttpDelete("{id}", Name = "DeleteTaskItemById")]
        public async Task<IActionResult> DeleteTaskItemByUser(int id){
            try{
                var rs = await _taskItemRepository.DeleteTaskItemAsync(id); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

    }
}