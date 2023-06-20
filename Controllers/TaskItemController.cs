
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
        public async Task<IActionResult> CreateTaskItem([FromQuery]int workspaceId, TaskItemDto taskItemDto){
            try{
                if(workspaceId > 0){
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _taskItemRepository.CreateTaskItemAsync(workspaceId, userId, taskItemDto); 
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
        public async Task<IActionResult> UpdateTaskItemByUser(int id, [FromQuery]int workspaceId, TaskItemDto taskItemDto){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.UpdateTaskItemAsync(id, workspaceId, userId, taskItemDto); 
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
        public async Task<IActionResult> MoveTaskItemByUser(int id, [FromQuery]int workspaceId,  MoveTaskDto moveTaskDto){
            try{  
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.MoveTaskItemAsync(id, workspaceId, userId, moveTaskDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPost("{id}/upload-file")]
        public async Task<IActionResult> UploadFile(int id, [FromQuery]int workspaceId,  [FromForm] IFormFile file){
            if (file!=null){
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.UploadFileAsync(id, workspaceId, userId, file);
                return Ok(rs);
            }
            return BadRequest();
        }

        [HttpDelete("{id}", Name = "DeleteTaskItemById")]
        public async Task<IActionResult> DeleteTaskItemByUser(int id, [FromQuery]int workspaceId){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.DeleteTaskItemAsync(id, workspaceId, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }
        
        
        
        [HttpPost("{id}/AssignMember")]
        public async Task<IActionResult> AssignMemberToTaskItem(int id, [FromQuery]int workspaceId, [FromBody] List<MemberTaskDto> memberTaskDtos){
            try{
                if(workspaceId >0){    
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _taskItemRepository.AssignMemberAsync(id, workspaceId, userId, memberTaskDtos); 
                    return Ok(rs);
                }
                return BadRequest();
            }
            catch{
                return BadRequest();
            }
        }

        [HttpGet("Member")]
        public async Task<IActionResult> GetTaskItemByMember(string memberId, PRIORITY_ENUM? priority, bool? isComplete, bool? desc){
            try{
                var rs = await _taskItemRepository.GetTasksItemByMemberAsync(memberId, priority, isComplete, desc); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPost("ExtendDueDate")]
        public async Task<IActionResult> RequestExtendDueDateByMember([FromQuery]int workspaceId, [FromBody] MemberTaskDto memberTaskDto){
            try{
                if(workspaceId >0){    
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _taskItemRepository.ExtendDueDateByMemberAsync(workspaceId, userId, memberTaskDto); 
                    return Ok(rs);
                }
                return BadRequest();
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPost("AcceptDueDate")]
        public async Task<IActionResult> AcceptExtendDueDate([FromQuery]int workspaceId, [FromBody] MemberTaskDto memberTaskDto){
            try{
                if(workspaceId >0){    
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _taskItemRepository.AcceptExtendDueDateAsync(workspaceId, userId, memberTaskDto); 
                    return Ok(rs);
                }
                return BadRequest();
            }
            catch{
                return BadRequest();
            }
        }
        [HttpPost("RejectDueDate")]
        public async Task<IActionResult> RejectExtendDueDate([FromQuery]int workspaceId, int memberTaskId){
            try{
                if(workspaceId >0){    
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _taskItemRepository.RejectExtendDueDateAsync(workspaceId, userId, memberTaskId); 
                    return Ok(rs);
                }
                return BadRequest();
            }
            catch{
                return BadRequest();
            }
        }

        // [HttpPost("RemoveMember")]
        // public async Task<IActionResult> RemoveMemberToTaskItem([FromQuery]int workspaceId, [FromBody] MemberTaskDto memberTaskDto){
        //     try{
        //         if(workspaceId >0){    
        //             var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
        //             var rs = await _taskItemRepository.RemoveMemberAsync(workspaceId, userId, memberTaskDto); 
        //             return Ok(rs);
        //         }
        //         return BadRequest();
        //     }
        //     catch{
        //         return BadRequest();
        //     }
        // }

        #region Comment
        [HttpPost("Comment")]
        public async Task<IActionResult> CreateCommentInTaskItem([FromQuery]int workspaceId, CommentDto commentDto){
            try{
                if(workspaceId > 0){
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                    var rs = await _taskItemRepository.CreateCommentAsync(workspaceId, userId, commentDto); 
                    return Ok(rs);
                }
                return BadRequest();
            }
            catch{
                return BadRequest();
            }
        }


        [HttpPut("Comment/{id}", Name = "UpdateCommentById")]
        public async Task<IActionResult> UpdateCommentInTaskItem(int id, CommentDto commentDto){
            try{    
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.EditCommentAsync(id, userId, commentDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpDelete("Comment/{id}", Name = "DeleteCommentById")]
        public async Task<IActionResult> DeleteCommentInTaskItem(int id){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _taskItemRepository.DeleteCommentAsync(id, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        #endregion

        [HttpPost("{id}/AddLabels")]
        public async Task<IActionResult> AddLabelToTaskItem(int id, List<LabelDto> labelDtos){
            try{   
                var rs = await _taskItemRepository.AddLabelToTaskItemAsync(id, labelDtos); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }


    }
}