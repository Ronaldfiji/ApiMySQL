using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Repository.Contracts;
using SharedModel.Dtos;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Concurrent;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AuctionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DesignationController : ControllerBase
    {
        private readonly IDesignationRepository designationRespository;
        private ILogger<DesignationController> logger;

        public DesignationController(IDesignationRepository _designationRepository, ILogger<DesignationController> _logger)
        {
            designationRespository = _designationRepository;
            logger = _logger;           
        }

        // GET api/<DesignationController>GetDesignation/5
        [HttpGet("GetDesignation/{id:int}")]
        public async Task<ActionResult> GetItem(int id)
        {
            var httpResponse = new HttpResponseMessage();
            try
            {
                var designationDto = await designationRespository.GetById(id);

                if (designationDto == null)
                {
                    return NotFound();
                }               
                return Ok(designationDto);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // GET: api/<DesignationController/GetDesignations>
        [HttpGet("GetDesignations")]
        public async Task<ActionResult> Get([FromQuery] PagingRequestDto pagingRequestDto)
        {
            try
            {
                //pagingRequestDto.RedisCacheExpiry = 30;
                var designationsDtos = await this.designationRespository.GetDesignations(pagingRequestDto);

                if (designationsDtos == null)
                {
                    return NotFound();
                }              

                var pagedResponse = new PagingResponse<DesignationDto>
                {
                    Items = designationsDtos.ToList(),
                    MetaData = designationsDtos.MetaData,
                };
                return Ok(pagedResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);

            }
        }

        // GET: api/<DesignationController/GetDesignations>
        [HttpGet("GetDesignationsFiltered")]
        public async Task<ActionResult> GetDesignationsFiltered([FromQuery] PagingRequestDto pagingRequestDto)
        {
            try
            {             
                var designationsDtos = await this.designationRespository.GetDesignationsFiltered(pagingRequestDto);
               
                if (designationsDtos == null)
                {
                    return NotFound();
                }
           
                var pagedResponse = new PagingResponse<DesignationDto>
                {
                    Items = designationsDtos.ToList(),
                    MetaData = designationsDtos.MetaData,
                };
          
                return Ok(pagedResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);

            }
        }

        // POST: api/<DesignationController/Create>        
        [HttpPost("Create")]
        public async Task<ActionResult> PostItem([FromBody] DesignationDto designationDto)
        {
            try
            {
                if (designationDto == null || !ModelState.IsValid)
                {
                    return BadRequest($"{nameof(designationDto)} cannot be null or empty !");
                }

                if (Request.HttpContext.Connection.RemoteIpAddress?.ToString() != null)
                {
                    designationDto.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                }

                var designation = await designationRespository.Find(d => d.JobTitle.ToLower() == designationDto.JobTitle.ToLower());
                if (designation != null && designation.Any())
                {
                    ModelState.AddModelError("Job title", "Job title already exist in database !");
                    return StatusCode(StatusCodes.Status409Conflict, $"Job title exist in database : {designationDto.JobTitle} . Provide different job title !");
                }
                var newDesignation = await this.designationRespository.CreateDesignation(designationDto);

                if (newDesignation == null)
                {
                    throw new Exception($"Something went wrong when attempting to create object (Item: ({newDesignation?.Id})");
                }       

                return CreatedAtAction(nameof(GetItem), new { id = newDesignation.Id }, newDesignation);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("duplicate key row in object"))
                {
                    return BadRequest(ex.Message);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        //PATCH: api/DesignationController/UpdateDesignation{int id, designationDto}           
        [HttpPatch("Update/{id:int}")]
        public async Task<ActionResult> UpdateDesignation(int id, [FromBody] DesignationDto designationDto)
        {
            try
            {
                if (designationDto == null || !ModelState.IsValid)
                {
                    return BadRequest($"{nameof(designationDto)} cannot be null or empty !");
                }

                if (id != designationDto.Id)
                    return BadRequest("Designation ID mismatch !");

                if (Request.HttpContext.Connection.RemoteIpAddress?.ToString() != null)
                {
                    designationDto.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                }

                designationDto.ModifiedDate = DateTime.UtcNow.AddHours(12);

                var updatedDesignation = await this.designationRespository.UpdateDesignation(designationDto);

                if (updatedDesignation == null)
                {
                    return NotFound();
                }
                //var designationDto_ = await designationRespository.GetById(id);               
                return Ok(updatedDesignation);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("duplicate key row in object"))
                {
                    return BadRequest(ex.Message);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }

        // DELETE api/<DesignationController>/Delete/5     
        [HttpDelete("Delete/{id:int}")]
        public async Task<ActionResult> DeleteItem(int id)
        {
            try
            {
                var deletedItem = await designationRespository.DeleteDesignation(id);

                if (deletedItem == null)
                {
                    return NotFound($"Failed to delete object {nameof(DesignationDto)} with id : {id}");
                }             

                return Ok(deletedItem); // or ' return NoContent();'

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Delete: api/DesignationController/Delete/List<customer>
        [HttpPost("DeleteItems")]
        public async Task<ActionResult> DeleteDesignations([FromBody] IEnumerable<DesignationDto> designationDtos)
        {
            try
            {                

                if (designationDtos == null || !designationDtos.Any() || !ModelState.IsValid)
                {
                    return BadRequest($"Entity to delete {nameof(designationDtos)} cannot be null or empty !");
                }

                foreach(var des in designationDtos)
                {
                    var itemExist = await designationRespository.GetById(des.Id);
                    if (itemExist == null)
                    {                       
                        return NotFound($"Item to be deleted does not exist with id :  {des.Id}");
                    }
                }

                var deleteDesignations = await designationRespository.DeleteDesignations(designationDtos.ToList());

                if (deleteDesignations == false)
                {
                    return NotFound();
                }

                return Ok(new { message = "Records deleted successfully", StatusCode = 200, Status = deleteDesignations });
            }

            catch (Exception ex)
            {
                if (ex.Message.Contains("modified or deleted"))
                {
                    return BadRequest(ex.Message);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }



    }
}

/*var model = JsonSerializer.Serialize(pagingRequestDto.FilterList, new JsonSerializerOptions
{
    WriteIndented = true,
    ReferenceHandler = ReferenceHandler.IgnoreCycles
});
Console.WriteLine("Filter list is : " + model);
*/