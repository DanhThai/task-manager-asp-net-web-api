using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabelController : ControllerBase
    {
        private readonly ILabelRepository _labelRepository;

        public LabelController(ILabelRepository LabelRepository)
        {
            _labelRepository = LabelRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLabel(LabelDto LabelDto){
            try{
                var rs = await _labelRepository.CreateLabelAsync(LabelDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLabelsByWorkspace(int workspaceId){
            try{
                var rs = await _labelRepository.GetListLabelByWorkspaceIdAsync(workspaceId); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }

        [HttpPut("{id}", Name = "UpdateLabelById")]
        public async Task<IActionResult> UpdateLabel(int id, LabelDto LabelDto){
            try{    
                var rs = await _labelRepository.UpdateLabelAsync(id, LabelDto); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }


        [HttpDelete("{id}", Name = "DeleteLabelById")]
        public async Task<IActionResult> DeleteLabelByUser(int id){
            try{
                var rs = await _labelRepository.DeleteLabelAsync(id); 
                return Ok(rs);
            }
            catch{
                return BadRequest();
            }
        }
    }
}