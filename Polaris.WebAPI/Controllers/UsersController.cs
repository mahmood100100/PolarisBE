using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polaris.Application.Common.Exceptions;
using Polaris.Application.Common.Response;
using Polaris.Application.Features.Users.Commands.DeleteUser;
using Polaris.Application.Features.Users.Commands.RegisterUser;
using Polaris.Application.Features.Users.Commands.UpdateUser;
using Polaris.Application.Features.Users.Queries.GetAllUsers;
using Polaris.Application.Features.Users.Queries.GetCurrentUser;
using Polaris.Application.Features.Users.Queries.GetUserById;
using Polaris.WebAPI.Common.Adapters;
using Polaris.WebAPI.Models.User;

namespace Polaris.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseApiController
    {
        private readonly IMapper _mapper;

        public UsersController(IMapper mapper)
        {
            this._mapper = mapper;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiValidationResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> Register([FromForm] RegisterUserRequest request)
        {
            try
            {
                var command = _mapper.Map<RegisterUserCommand>(request);

                var result = await Mediator.Send(command);

                var response = _mapper.Map<UserResponse>(result);

                return Ok(new ApiResponse(
                    StatusCode: 200,
                    Message: "User registered successfully",
                    Result: response
                ));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiValidationResponse(
                    Errors: new[] { ex.Message },
                    StatusCode: 400
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(
                    StatusCode: 500,
                    Message: $"An error occurred during registration: {ex.Message}"
                ));
            }
        }


        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiValidationResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdateUserRequest request)
        {
            try
            {
                var command = _mapper.Map<UpdateUserCommand>(request);
                command.Id = id;

                var result = await Mediator.Send(command);

                var response = _mapper.Map<UserResponse>(result);

                return Ok(new ApiResponse(
                    StatusCode: 200,
                    Message: "User updated successfully",
                    Result: response
                ));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new ApiResponse(403, ex.Message));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse(404, ex.Message));
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiValidationResponse(new[] { ex.Message }, 400));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, "An error occurred while updating user"));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var command = new DeleteUserCommand { Id = id };
                var result = await Mediator.Send(command);

                if (result is true)
                {
                    return Ok(new ApiResponse(
                        StatusCode: 200,
                        Message: "User deleted successfully"
                    ));
                }

                return BadRequest(new ApiResponse(400, "Failed to delete user"));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new ApiResponse(403, ex.Message));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse(404, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, $"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                var query = new GetAllUsersQuery
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm
                };

                var result = await Mediator.Send(query);

                var response = _mapper.Map<List<UserListItemResponse>>(result);

                return Ok(new ApiResponse(
                    StatusCode: 200,
                    Message: "Users retrieved successfully",
                    Result: response
                ));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new ApiResponse(403, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, $"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var query = new GetUserByIdQuery { Id = id };

                var result = await Mediator.Send(query);

                if (result == null)
                {
                    return NotFound(new ApiResponse(
                        StatusCode: 404,
                        Message: $"User with ID {id} not found"
                    ));
                }

                var response = _mapper.Map<UserDetailsResponse>(result);

                return Ok(new ApiResponse(
                    StatusCode: 200,
                    Message: "User retrieved successfully",
                    Result: response
                ));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new ApiResponse(403, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, $"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var query = new GetCurrentUserQuery();
                var result = await Mediator.Send(query);

                var response = _mapper.Map<UserDetailsResponse>(result);

                return Ok(new ApiResponse(200, "User retrieved successfully", response));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse(404, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse(500, $"An error occurred: {ex.Message}"));
            }
        }
    }
}
