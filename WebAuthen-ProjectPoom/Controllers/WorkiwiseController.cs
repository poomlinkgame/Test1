using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using RepoDb;
using WebAuthen.App_code;
using WebAuthen.Models;

namespace WebAuthen.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkiwiseController(DynamicConnectionService dynamicConnection, WorkiwiseClass workiwise, IWebHostEnvironment env, HandleApiReturn handle) : ControllerBase
{
    private readonly IWebHostEnvironment _env = env;
    private readonly DynamicConnectionService _dynamicConnection = dynamicConnection;
    private readonly WorkiwiseClass _workiwise = workiwise;
    private readonly HandleApiReturn _handle = handle;

    [HttpGet("Version")]
    public IActionResult Version()
    {
        return Ok("1.0.0");
    }

    [HttpPost("CreateWorkSpace")]
    [Authorize]
    public async Task<IActionResult> CreateWorkSpace([FromBody] WorkSpaceDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            string fullPath = string.Empty;
            string fileUrl = string.Empty;

            if (!string.IsNullOrEmpty(body.Img) &&
                !string.IsNullOrEmpty(body.Img_Type) &&
                !string.IsNullOrEmpty(body.Img_Name))
            {

                string? ext = body.Img_Type switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    "application/pdf" => ".pdf",
                    _ => null
                };

                if (string.IsNullOrEmpty(ext)) return new ReturnDto("400", "ไม่รองรับไฟล์ประเภทนี้");

                string folderName = "workspace_file";
                string folderPath = Path.Combine(_env.ContentRootPath, folderName);
                Directory.CreateDirectory(folderPath);
                string fileName = $"{Guid.NewGuid():N}{ext}";
                fullPath = Path.Combine(folderPath, fileName);

                string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                fileUrl = $"{baseUrl}/{folderName}/{fileName}";
            }

            bool result = await _workiwise.InsertWorkSpaceAsync(authorization, body, fileUrl, fullPath);
            return result ? new ReturnDto("200", "บันทึกข้อมูลสำเร็จ") : new ReturnDto("500", "ไม่สามารถบันทึกข้อมูลได้ ชื่อพื้นที่ทำงานนี้มีอยู่แล้ว");
        });
    }

    [HttpGet("WorkSpaces")]
    [Authorize]
    public async Task<IActionResult> GetWorkSpace()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<WorkspaceResult> workspaces = await _workiwise.GetWorkSpaceAsync(authorization);
            return new ReturnDto(Code: "200", Data: workspaces);
        });
    }

    [HttpGet("Tags")]
    [Authorize]
    public async Task<IActionResult> GetTags()
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> tags = await _workiwise.GetTagsAsync(authorization.Constr, authorization.Id);
            return new ReturnDto(Code: "200", Data: tags);
        });
    }

    [HttpPost("AddTag")]
    [Authorize]
    public async Task<IActionResult> CreateTag([FromBody] TagDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool isHave = await _workiwise.HaveTagAsync(authorization, body.Tags_Name);
            if (isHave) return new ReturnDto(Code: "400", Message: "ชื่อนี้มีอยู่แล้ว");

            bool result = await _workiwise.InsertTagAsync(authorization, body.Tags_Name, body.Create_By);
            return result ? new ReturnDto(Code: "200", Message: "บันทึกข้อมูลสำเร็จ") : new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลได้");
        });
    }

    [HttpPost("CreateFolder")]
    [Authorize]
    public async Task<IActionResult> CreateFolderWorkSpace([FromBody] FolderWorkSpaceDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            bool result = await _workiwise.InsertFolderAsync(authorization, body);
            return result ? new ReturnDto(Code: "200", Message: "บันทึกข้อมูลสำเร็จ") : new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลได้ ชื่อโฟลเดอร์นี้มีอยู่แล้ว");
        });
    }

    [HttpGet("Folders/{spaceId:int}")]
    [Authorize]
    public async Task<IActionResult> GetFolder([FromRoute] int spaceId)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> folders = await _workiwise.GetFolderByIdAsync(authorization.Constr, spaceId);
            return new ReturnDto(Code: "200", Data: folders);
        });
    }

    [HttpGet("AllFolders/{spaceId:int}")]
    [Authorize]
    public async Task<IActionResult> GetAllFolder([FromRoute] int spaceId)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            JsonElement? folders = await _workiwise.GetFoldersAsync(authorization.Constr, spaceId);
            return folders != null ? new ReturnDto(Code: "200", Data: folders) : new ReturnDto(Code: "500", Message: "ไม่สามารถดึงข้อมูลได้", Data: Array.Empty<object>());
        });
    }

    [HttpPost("AddDocument")]
    [Authorize]
    public async Task<IActionResult> CreateDocumnet([FromBody] DocumentDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;

            bool isHave = await _workiwise.HaveDocumentAsync(authorization, body.doc_name, body.folder_id);
            if (isHave) return new ReturnDto(Code: "400", Message: "ชื่อเอกสารนี้มีอยู่แล้วในโฟลเดอร์นี้");

            string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            bool result = await _workiwise.InsertDocumentAsync(authorization, body, baseUrl);
            return result ? new ReturnDto(Code: "200", Message: "บันทึกข้อมูลสำเร็จ") : new ReturnDto(Code: "500", Message: "ไม่สามารถบันทึกข้อมูลได้");
        });
    }

    [HttpPost("ConvertImage")]
    public async Task<IActionResult> ConvertImage([FromBody] UploadImageDto body)
    {
        return await _handle.ApiAsync(async () =>
        {
            // AuthorizationDto authorization = User.GetAuthorization()!;

            string baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            dynamic? imageUrl = await _workiwise.ConvertImageAsync("045", baseUrl, body);
            return imageUrl != null ? new ReturnDto(Code: "200", Data: imageUrl) : new ReturnDto(Code: "500", Message: "ไม่สามารถแปลงรูปภาพได้", Data: Array.Empty<object>());
        });
    }

    [HttpGet("Documents/{folderId:int}")]
    [Authorize]
    public async Task<IActionResult> GetDocument([FromRoute] int folderId)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> documents = await _workiwise.GetDocumentAsync(authorization.Constr, folderId);
            return new ReturnDto(Code: "200", Data: documents);
        });
    }

    [HttpGet("Document/files/{docId:int}")]
    [Authorize]
    public async Task<IActionResult> GetDocumentFile([FromRoute] int docId)
    {
        return await _handle.ApiAsync(async () =>
        {
            AuthorizationDto authorization = User.GetAuthorization()!;
            List<dynamic> files = await _workiwise.GetDocumentFileAsync(authorization.Constr, docId);
            return new ReturnDto(Code: "200", Data: files);
        });
    }

    [HttpPut("UpdateFolder")]
    [Authorize]
    public async Task<IActionResult> UpdateFolder([FromBody] FolderUpdateDto body)
    {
        AuthorizationDto authorization = User.GetAuthorization()!;
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new ReturnDto(Code: "404", Message: "ไม่พบการเชื่อมต่อฐานข้อมูล"));
        }
        using SqlConnection connection = new(connectionString);


        string sql = @"UPDATE tbl_workspace_m_folder
        SET folder_name = @folder_name,
            space_id = @space_id,
            folder_coler = @folder_coler,
            update_by = @update_by,
            update_date = @update_date
        WHERE folder_id = @folder_id
        ";

        var parameters = new
        {
            body.folder_name,
            body.space_id,
            body.folder_coler,
            body.folder_id,
            body.update_by,
            update_date = DateTime.Now
        };
        int rows = connection.ExecuteNonQuery(sql, parameters);
        if (rows > 0)
        {
            return Ok(new ReturnDto(Code: "200", Message: "อัพเดทข้อมูลสำเร็จ"));
        }
        else
        {
            return StatusCode(500, new ReturnDto(Code: "500", Message: "ไม่สามารถอัพเดทข้อมูลได้"));
        }
    }

    [HttpPut("UpdateFile")]
    [Authorize]
    public async Task<IActionResult> UpdateFile([FromBody] FileUpdateDto body)

    {
        AuthorizationDto authorization = User.GetAuthorization()!;
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new ReturnDto(Code: "404", Message: "ไม่พบการเชื่อมต่อฐานข้อมูล"));
        }
        using SqlConnection connection = new(connectionString);

        string sql = @"UPDATE tbl_workspace_m_file
        SET file_name = @file_name,
            file_html = @file_html,
            folder_id = @folder_id,
            update_by = @update_by,
            update_date = @update_date
        WHERE file_id = @file_id
        ";

        var parameters = new
        {
            body.file_name,
            body.file_html,
            body.folder_id,
            body.file_id,
            body.update_by,
            update_date = DateTime.Now
        };
        int rows = connection.ExecuteNonQuery(sql, parameters);
        if (rows > 0)
        {
            return Ok(new ReturnDto(Code: "200", Message: "อัพเดทข้อมูลสำเร็จ"));
        }
        else
        {
            return StatusCode(500, new ReturnDto(Code: "500", Message: "ไม่สามารถอัพเดทข้อมูลได้"));
        }
    }



}