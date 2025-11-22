using Microsoft.AspNetCore.Mvc;
using WebAuthen.Models;

namespace WebAuthen.App_code;

public class HandleApiReturn : ControllerBase
{
    public async Task<IActionResult> ApiAsync(Func<Task<ReturnDto>> func)
    {
        try
        {
            ReturnDto result = await func();
            return Ok(result);
        }
        catch (Exception e)
        {
            return StatusCode(500, new ReturnDto(Code: "500", Message: e.Message));
        }
    }
}