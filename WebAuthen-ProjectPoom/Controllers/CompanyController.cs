using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAuthen.App_code;
using WebAuthen.Models;

namespace WebAuthen.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyController(HandleApiReturn handle, CompanyClass company) : ControllerBase
{
    private readonly HandleApiReturn _handle = handle;
    private readonly CompanyClass _company = company;

    [HttpGet("Version")]
    public IActionResult Version()
    {
        return Ok("1.0.1");
    }

    [HttpPost("SaveDataCompany")]
    [Authorize]
    public async Task<IActionResult> SaveDataCompany([FromBody] CompanyDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InserDataCompany(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลเรียบร้อย"),
                false => new ReturnDto(Code: "200", Message: "เลขประจำตัวผู้เสียภาษี/เลชบัตรประชาชนซ้ำ กรุณาตรวจสอบข้อมูลอีกครั้ง")
            };
        });
    }

    [HttpPut("EditDataCompany")]
    [Authorize]
    public async Task<IActionResult> EditDataCompany([FromBody] CompanyDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.EditDataCompany(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "แก้ไขข้อมูลเรียบร้อย"),
                false => new ReturnDto(Code: "200", Message: "เลขประจำตัวผู้เสียภาษี/เลขบัตรประชาชนซ้ำ กรุณาตรวจสอบข้อมูลอีกครั้ง")
            };
        });
    }

    [HttpGet("GetDataCompany")]
    [Authorize]
    public async Task<IActionResult> GetdataCompany()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.DataCompanyGetAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลบริษัทสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลบริษัท", Data: result);
            }
        });
    }

    [HttpDelete("DeleteDataCompany")]
    [Authorize]
    public async Task<IActionResult> DeleteSubAccount([FromBody] CompanyDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.DataCompanyDelete(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลได้")
            };
        });
    }

    [HttpGet("GetCompanyType")]
    [Authorize]
    public async Task<IActionResult> GetCompanyType()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetCompanyTypeAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลประเภทบริษัทสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลประเภทบริษัท", Data: result);
            }
        });
    }

    [HttpPost("SaveCompanyType")]
    [Authorize]
    public async Task<IActionResult> SaveCompanyType([FromBody] CompanyDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InsertCompanyTypeAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลประเภทบริษัทเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลประเภทบริษัทได้")
            };
        });
    }

    [HttpPut("EditCompanyType")]
    [Authorize]
    public async Task<IActionResult> EditCompanyType([FromBody] CompanyDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.EditCompanyTypeAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "แก้ไขข้อมูลประเภทบริษัทเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถแก้ไขข้อมูลประเภทบริษัทได้")
            };
        });
    }

    [HttpGet("GetCompVat")]
    [Authorize]

    public async Task<IActionResult> GetCompVat()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetCompVatAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลอัตราภาษีสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลอัตราภาษี", Data: result);
            }
        });
    }

    // --------------------------------------------------------------------------------------------------------------------------

    [HttpGet("GetDataDepartment")]
    [Authorize]
    public async Task<IActionResult> GetDataDepartmentAll()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetDataDepartmentAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลแผนกสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลแผนก", Data: result);
            }
        });
    }

    [HttpGet("GetDataDepartment/{comp_id}")]
    [Authorize]
    public async Task<IActionResult> GetDataDepartmentByComp(string comp_id)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetDataDepartmentAsync(authorization, comp_id);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลแผนกสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลแผนก", Data: result);
            }
        });
    }

    [HttpPost("SaveDataDepartment")]
    [Authorize]
    public async Task<IActionResult> SaveDataDepartment([FromBody] CompDepartmentDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InsertDepartmentAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลแผนกเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลแผนกได้")
            };
        });
    }

    [HttpPut("EditDataDepartment")]
    [Authorize]
    public async Task<IActionResult> EditDataDepartment([FromBody] CompDepartmentDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.EditDepartmentAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "แก้ไขข้อมูลแผนกเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถแก้ไขข้อมูลแผนกได้")
            };
        });
    }

    [HttpDelete("DeleteDepartment")]
    [Authorize]
    public async Task<IActionResult> DeleteDataDepartment([FromBody] CompdepartmentDeleteAllDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.DeleteDepartmentAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลแผนกสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลแผนกได้")
            };
        });
    }

    [HttpDelete("DeleteDepartmentCompany")]
    [Authorize]
    public async Task<IActionResult> DeleteDataDepartmentCompany([FromBody] CompdepartmentDeleteAllDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.DeleteDepartmentfromecompAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลแผนกบริษัทสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลแผนกบริษัทได้")
            };
        });
    }

    [HttpGet("GetDataPosition")]
    [Authorize]
    public async Task<IActionResult> GetDataPositionAll()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetDataPositionAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลตำแหน่งสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลตำแหน่ง", Data: result);
            }
        });
    }

    [HttpGet("GetDataPosition/{comp_id}")]
    [Authorize]
    public async Task<IActionResult> GetDataPositionByComp(string comp_id)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetDataPositionAsync(authorization, comp_id);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลตำแหน่งสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลตำแหน่ง", Data: result);
            }
        });
    }

    [HttpPost("SaveDataPosition")]
    [Authorize]
    public async Task<IActionResult> SaveDataPosition([FromBody] PositionDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.PositionInsertAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลตำแหน่งเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลตำแหน่งได้")
            };
        });
    }

    [HttpPut("EditDataPosition")]
    [Authorize]
    public async Task<IActionResult> EditDataPosition([FromBody] PositionDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.PositionEditAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "แก้ไขข้อมูลตำแหน่งเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถแก้ไขข้อมูลตำแหน่งได้")
            };
        });
    }

    [HttpDelete("DeleteDataPosition")]
    [Authorize]
    public async Task<IActionResult> DeleteDataPosition([FromBody] PositionDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.PositionDeleteAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลตำแหน่งสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลตำแหน่งได้")
            };
        });
    }

    [HttpDelete("DeleteDataPositionCompany")]
    [Authorize]
    public async Task<IActionResult> DeleteDataPositionCompany([FromBody] PositionDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.PositionDeleteFromCompAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลตำแหน่งบริษัทสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลตำแหน่งบริษัทได้")
            };
        });
    }

    [HttpGet("GetDataStructure/{comp_id}")]
    [Authorize]
    public async Task<IActionResult> GetDataStructure(int comp_id)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetCompStructureAsync(authorization, comp_id);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลโครงสร้างสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลโครงสร้าง", Data: result);
            }
        });
    }

    [HttpPost("SaveDataStructure")]
    [Authorize]
    public async Task<IActionResult> SaveDataStructure([FromBody] CompStructureDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InsertCompStructureAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลโครงสร้างเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลโครงสร้างได้")
            };
        });
    }

    [HttpPut("EditDataStructure")]
    [Authorize]
    public async Task<IActionResult> EditDataStructure([FromBody] CompStructureDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.EditCompStructureAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "แก้ไขข้อมูลโครงสร้างเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถแก้ไขข้อมูลโครงสร้างได้")
            };
        });
    }

    [HttpDelete("DeleteDataStructure")]
    [Authorize]
    public async Task<IActionResult> DeleteDataStructure([FromBody] CompStructureDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.DeleteCompStructureAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลโครงสร้างสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลโครงสร้างได้")
            };
        });
    }

    [HttpGet("GetMainCalendar")]
    [Authorize]
    public async Task<IActionResult> GetMainCalendar()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetMainCalendarAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลปฏิทินสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลปฏิทิน", Data: result);
            }
        });
    }

    [HttpPost("SaveMainCalendar")]
    [Authorize]
    public async Task<IActionResult> SaveMainCalendar([FromBody] MainCalendarDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InsertMainCalendarAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลปฏิทินเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลปฏิทินได้")
            };
        });
    }

    [HttpPut("EditMainCalendar")]
    [Authorize]
    public async Task<IActionResult> EditMainCalendar([FromBody] MainCalendarDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.EditMainCalendarAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "แก้ไขข้อมูลปฏิทินเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถแก้ไขข้อมูลปฏิทินได้")
            };
        });
    }

    [HttpDelete("DeleteMainCalendar")]
    [Authorize]
    public async Task<IActionResult> DeleteMainCalendar([FromBody] MainCalendarDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.DeleteMainCalendarAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลปฏิทินสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลปฏิทินได้")
            };
        });
    }

    [HttpGet("GetHolidayType")]
    [Authorize]
    public async Task<IActionResult> GetHolidayType()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetHolidayTypeAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลประเภทวันหยุดสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลประเภทวันหยุด", Data: result);
            }
        });
    }

    [HttpPost("SaveHolidayType")]
    [Authorize]
    public async Task<IActionResult> SaveHolidayType([FromBody] holidaytypeDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InsertHolidayTypeAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลประเภทวันหยุดเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลประเภทวันหยุดได้")
            };
        });
    }

    [HttpPut("EditHolidayType")]
    [Authorize]
    public async Task<IActionResult> EditHolidayType([FromBody] holidaytypeDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.EditHolidayTypeAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "แก้ไขข้อมูลประเภทวันหยุดเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถแก้ไขข้อมูลประเภทวันหยุดได้")
            };
        });
    }

    [HttpDelete("DeleteHolidayType")]
    [Authorize]
    public async Task<IActionResult> DeleteHolidayType([FromBody] holidaytypeDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.DeleteHolidayTypeAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลประเภทวันหยุดสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลประเภทวันหยุดได้")
            };
        });
    }

    [HttpPost("GetHoliday")]
    [Authorize]
    public async Task<IActionResult> GetHoliday([FromBody] holidayDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetHolidayAsync(authorization, body);

            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลวันหยุดสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลวันหยุด", Data: result);
            }
        });
    }

    [HttpPost("SaveHoliday")]
    [Authorize]
    public async Task<IActionResult> SaveHoliday([FromBody] holidayDto body)

    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InsertHolidayAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลวันหยุดเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลวันหยุดได้")
            };
        });
    }

    [HttpDelete("DeleteHoliday")]
    [Authorize]
    public async Task<IActionResult> DeleteHoliday([FromBody] holidayDto body)

    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.DeleteHolidayAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลวันหยุดสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลวันหยุดได้")
            };
        });
    }

    [HttpPost("SaveWorkshift")]
    [Authorize]
    public async Task<IActionResult> SaveWorkshift([FromBody] WorkshiftRequestDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InsertWorkshiftAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลกะเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลกะได้ อาจมีชื่อเวรซ้ำ กรุณาตรวจสอบข้อมูลอีกครั้ง")
            };
        });
    }

    [HttpGet("Workshift")]
    [Authorize]
    public async Task<IActionResult> GetWorkshift()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetWorkshiftAsync(authorization.Constr, authorization.Id);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลกะสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลกะ", Data: result);
            }
        });
    }

    [HttpGet("WorkshiftDetail/{ws_id}")]
    [Authorize]
    public async Task<IActionResult> WorkshiftDetail(int ws_id)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            JsonElement? result = await _company.GetWorkshiftDetailAsync(authorization.Constr, authorization.Id, ws_id);
            if (result != null)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลกะสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลกะ", Data: result);
            }
        });
    }

    [HttpPost("SavePersonal")]
    [Authorize]
    public async Task<IActionResult> SavePersonal([FromBody] personalDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.InsertpersonalAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "บันทึกข้อมูลพนักงานเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลพนักงานได้ อาจมีชื่อผู้ใช้งานซ้ำ กรุณาตรวจสอบข้อมูลอีกครั้ง")
            };
        });
    }

    [HttpGet("GetPersonal")]
    [Authorize]
    public async Task<IActionResult> GetPersonal()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetpersonalAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลพนักงานสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลพนักงาน", Data: result);
            }
        });
    }

    [HttpPut("EditPersonal")]
    [Authorize]
    public async Task<IActionResult> EditPersonal([FromBody] personalDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.EditpersonalAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "แก้ไขข้อมูลพนักงานเรียบร้อย"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถแก้ไขข้อมูลพนักงานได้")
            };
        });
    }

    [HttpDelete("DeletePersonal")]
    [Authorize]
    public async Task<IActionResult> DeletePersonal([FromBody] personalDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _company.DeletepersonalAsync(authorization, body);

            return result switch
            {
                true => new ReturnDto(Code: "200", Message: "ลบข้อมูลพนักงานสำเร็จ"),
                false => new ReturnDto(Code: "500", Message: "ไม่สามารถลบข้อมูลพนักงานได้")
            };
        });
    }

    [HttpGet("GetStatus")]
    [Authorize]

    public async Task<IActionResult> GetStatus()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetStatusAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลสถานะสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลสถานะ", Data: result);
            }
        });
    }

    [HttpGet("GetEducation")]
    [Authorize]

    public async Task<IActionResult> GetEducation()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> result = await _company.GetEducationAsync(authorization);
            if (result.Count > 0)
            {
                return new ReturnDto(Code: "200", Message: "เรียกดูข้อมูลการศึกษาสำเร็จ", Data: result);
            }
            else
            {
                return new ReturnDto(Code: "404", Message: "ไม่พบข้อมูลการศึกษา", Data: result);
            }
        });
    }
}