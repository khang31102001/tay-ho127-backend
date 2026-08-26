using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Organization.Application.Departments;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Organization.Api;

[ApiController]
[Route("api/v1/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [RequirePermission(OrganizationPermissions.DepartmentsView)]
    [ProducesResponseType<PagedResult<DepartmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DepartmentResponse>>> List(
        [FromQuery] PagedRequest request, [FromQuery] Guid? organizationId, CancellationToken cancellationToken)
    {
        return Ok(await _departmentService.ListAsync(request, organizationId, cancellationToken));
    }

    [HttpGet("tree")]
    [RequirePermission(OrganizationPermissions.DepartmentsView)]
    [ProducesResponseType<IReadOnlyList<DepartmentTreeNode>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentTreeNode>>> GetTree([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        return Ok(await _departmentService.GetTreeAsync(organizationId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(OrganizationPermissions.DepartmentsView)]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _departmentService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(OrganizationPermissions.DepartmentsCreate)]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DepartmentResponse>> Create([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _departmentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(OrganizationPermissions.DepartmentsUpdate)]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartmentResponse>> Update(Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _departmentService.UpdateAsync(id, request, cancellationToken));
    }
}
