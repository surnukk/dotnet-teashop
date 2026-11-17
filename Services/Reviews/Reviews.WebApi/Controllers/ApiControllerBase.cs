using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults.Security;

namespace Reviews.WebApi.Controllers;

[ApiController]
public abstract class ApiControllerBase(ISender mediator) : ControllerBase
{
    protected ISender Mediator { get; } = mediator;
}