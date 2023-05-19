
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
    [Authorize]

    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceRepository _workspaceRepository;


        public WorkspaceController(IWorkspaceRepository workspaceRepository)
        {
            _workspaceRepository = workspaceRepository;
        }

        // var l = new List<int>(){1,2,3, 4, 5, 6, 7, 8, 9, 10, 11};
        // string rs = JsonConvert.SerializeObject(l);
        // var s = JsonConvert.DeserializeObject<List<int>>(rs);
        // Console.WriteLine(s);


        [HttpPost]
        public async Task<IActionResult> CreateWorkspace(WorkspaceDto workspaceDto){
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue("Name");

            var rs = await _workspaceRepository.CreateWorkspaceAsync(workspaceDto, userId, userName); 
            return Ok(rs);
        }

        [HttpPost("{id}/invite")]
        public async Task<IActionResult> InviteUserToWorkspace(int id, string email){
            try{
                var rs = await _workspaceRepository.InviteUserToWorkspaceAsync(id, email); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpGet("invite/confirmed")]
        public async Task<RedirectResult> ConfirmMemberWorkspace(int workspaceId, string userId){
            try{
                var rs = await _workspaceRepository.ConfirmMemberWorkspaceAsync(workspaceId, userId);
                if(rs.IsSuccess) 
                    return RedirectPermanent(rs.Message);
                else
                    return RedirectPermanent("https://localhost:7070/ConfirmEmailError.html");;
            }
            catch{
                return RedirectPermanent("https://localhost:7070/ConfirmEmailError.html");;

            }
        }

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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkspaceByUser(int id){
            try{
                // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var rs = await _workspaceRepository.DeleteWorkspaceAsync(id); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }
    }
}