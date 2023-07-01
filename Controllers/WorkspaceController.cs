
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]


    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceRepository _workspaceRepository;


        public WorkspaceController(IWorkspaceRepository workspaceRepository)
        {
            _workspaceRepository = workspaceRepository;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateWorkspace(WorkspaceDto workspaceDto){
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue("Name");

            var rs = await _workspaceRepository.CreateWorkspaceAsync(workspaceDto, userId, userName); 
            return Ok(rs);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetWorkspaceByUser(){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var rs = await _workspaceRepository.GetWorkspacesByUserAsync(userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }  
        [Authorize]
        [HttpGet("recently")]
        public async Task<IActionResult> GetWorkspaceRecently(){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var rs = await _workspaceRepository.GetWorkspaceRecentlyAsync(userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }   

        [Authorize]
        [HttpGet("{id}", Name = "WorkspaceById")]
        public async Task<IActionResult> GetWorkspaceById(int id){
            if (id != 0)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                try{
                    var rs = await _workspaceRepository.GetWorkspaceByIdAsync(id, userId); 
                    return Ok(rs);
                }
                catch{
                    return NotFound();
                }
            }
            return BadRequest();
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkspaceByUser(WorkspaceDto workspaceDto,int id){
            try{
                // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                workspaceDto.Id = id;     
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var rs = await _workspaceRepository.UpdateWorkspaceAsync(workspaceDto, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkspaceByUser(int id){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var rs = await _workspaceRepository.DeleteWorkspaceAsync(id, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("{id}/cards")]
        public async Task<IActionResult> GetCardsByWorkspaceId(int id){
            try{
                var rs = await _workspaceRepository.GetCardsOfWorkspaceAsync(id); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }   

        [Authorize]
        #region Member
        [HttpPost("{id}/Invite")]
        public async Task<IActionResult> InviteUserToWorkspace(int id, MemberWorkspaceDto member){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var rs = await _workspaceRepository.InviteMemberToWorkspaceAsync(id, userId, member); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpGet("Invite/Confirmed")]
        public async Task<RedirectResult> ConfirmMemberWorkspace(int workspaceId, string userId, int role){
            try{
                if(workspaceId >0 && userId != null && role > 0){
                    var rs = await _workspaceRepository.ConfirmMemberWorkspaceAsync(workspaceId, userId, role);
                    return RedirectPermanent(rs.Message);
                }
                return RedirectPermanent("https://localhost:7070/ConfirmEmailError.html");
            }
            catch{
                return RedirectPermanent("https://localhost:7070/ConfirmEmailError.html");;

            }
        }

        [Authorize]
        [HttpGet("{id}/Members")]
        public async Task<IActionResult> GetMembersById(int id){
            try{
                var rs = await _workspaceRepository.GetMembersOfWorkspaceAsync(id); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("{id}/MembersWithTasks")]
        public async Task<IActionResult> GetMembersWithTaskItemById(int id){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _workspaceRepository.GetMembersWithTaskItemAsync(id, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [Authorize]
        [HttpPost("{id}/LeaveWorkspace")]
        public async Task<IActionResult> LeaveOnWorkspace(int id){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                var rs = await _workspaceRepository.LeaveOnWorkspaceAsync(id, userId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [Authorize]
        [HttpPost("{id}/RemoveMember")]
        public async Task<IActionResult> RemoveMemberToWorkspace(int id, string memberId){
            try{
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
                if(userId == memberId)
                    return BadRequest();
                var rs = await _workspaceRepository.RemoveMemberToWorkspaceAsync(id, userId, memberId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }
        #endregion

    }
}