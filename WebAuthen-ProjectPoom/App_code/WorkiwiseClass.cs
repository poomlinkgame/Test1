using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using RepoDb;
using WebAuthen.Models;

namespace WebAuthen.App_code;

public class WorkiwiseClass(DynamicConnectionService dynamicConnection, IWebHostEnvironment env)
{
    private readonly DynamicConnectionService _dynamicConnection = dynamicConnection;
    private readonly IWebHostEnvironment _env = env;

    public async Task<bool> InsertWorkSpaceAsync(AuthorizationDto authorization, WorkSpaceDto workSpaceDto, string fileUrl, string fullPath)
    {
        string sql = @"
        INSERT INTO tbl_workspace_m_space
        (space_name, userweb_id, fitem_name, fitem_type, img_path, flg, create_by, create_date)
        SELECT @SpaceName,  @UserWebId, @FitemName, @FitemType, @ImgPath, @Flg, @CreateBy, @CreateDate
        WHERE NOT EXISTS (
            SELECT 1 FROM tbl_workspace_m_space WHERE space_name = @SpaceName AND userweb_id = @UserWebId AND ISNULL(flg, 0) <> 9
        )

        DECLARE @SpaceId INT = SCOPE_IDENTITY();
        select @SpaceId as SpaceId;
        ";

        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            var parameters = new
            {
                SpaceName = workSpaceDto.Space_Name,
                UserWebId = authorization.Id,
                FitemName = workSpaceDto.Img_Name,
                FitemType = workSpaceDto.Img_Type,
                ImgPath = fileUrl,
                Flg = 0,
                CreateBy = workSpaceDto.Create_By,
                CreateDate = DateTime.Now,
            };

            int space_id = await connection.ExecuteScalarAsync<int>(sql, param: parameters, transaction: transaction);

            if (space_id == 0)
            {
                transaction.Rollback();
                return false;
            }

            if (workSpaceDto.Tag_ids.Count > 0)
            {
                var tagIdsJson = JsonSerializer.Serialize(workSpaceDto.Tag_ids.Distinct());

                const string linkTagsSql = @"
                WITH tag_in AS (
                    SELECT DISTINCT CAST([value] AS INT) AS tags_id
                    FROM OPENJSON(@TagIdsJson)
                ),
                valid_tag AS (
                    -- (ถ้ามีตาราง master tags เช่น tbl_workspace_m_tags ให้กรองเฉพาะที่ยังไม่ถูกลบ)
                    SELECT t.tags_id
                    FROM tag_in t
                    JOIN tbl_workspace_m_tags mt ON mt.tags_id = t.tags_id AND ISNULL(mt.flg,0) <> 9
                )
                INSERT INTO tbl_workspace_m_space_tags_list (tags_id, space_id, flg, create_by, create_date)
                SELECT v.tags_id, @SpaceId, 0, @CreateBy, @CreateDate
                FROM valid_tag v
                LEFT JOIN tbl_workspace_m_space_tags_list x
                    ON x.space_id = @SpaceId
                    AND x.tags_id  = v.tags_id
                    AND ISNULL(x.flg,0) <> 9
                WHERE x.st_id IS NULL;";

                var linkParams = new
                {
                    TagIdsJson = tagIdsJson,
                    SpaceId = space_id,
                    CreateBy = workSpaceDto.Create_By,
                    CreateDate = DateTime.Now,
                };

                await connection.ExecuteNonQueryAsync(linkTagsSql, param: linkParams, transaction: transaction);
            }

            if (!string.IsNullOrEmpty(fullPath))
            {

                await Utils.Base64ToFile(workSpaceDto.Img!, fullPath);

            }

            transaction.Commit();
            return true;

        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }


    }

    public async Task<List<WorkspaceResult>> GetWorkSpaceAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        SELECT
            s.space_id,
            s.space_name,
            s.img_path,
            COALESCE((
                SELECT
                    '[' + STRING_AGG(CHAR(34) + STRING_ESCAPE(d.tags_name, 'json') + CHAR(34), ',') + ']'
                FROM (
                    SELECT DISTINCT mt.tags_name
                    FROM tbl_workspace_m_space_tags_list l
                    JOIN tbl_workspace_m_tags mt ON mt.tags_id = l.tags_id AND ISNULL(mt.flg,0) <> 9
                    WHERE l.space_id = s.space_id AND ISNULL(l.flg,0) <> 9
                ) d
            ), '[]') AS tags_name_json
        FROM tbl_workspace_m_space s
        WHERE userweb_id = @UserWebId AND ISNULL(s.flg,0) <> 9;
        ";

        var parameters = new { UserWebId = authorization.Id };
        var workspaces = await connection.ExecuteQueryAsync<dynamic>(sql, param: parameters, transaction: transaction);
        var data = workspaces.Select(r => new WorkspaceResult
        {
            space_id = (int)r.space_id,
            space_name = (string)r.space_name,
            img_path = (string)r.img_path,
            tags_name = string.IsNullOrEmpty((string)r.tags_name_json)
                ? []
                : (JsonSerializer.Deserialize<List<string>>((string)r.tags_name_json) ?? [])
        }).ToList();

        return data;
    }

    public async Task<List<dynamic>> GetTagsAsync(string Constr, int UserId)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        SELECT tags_id, tags_name
        FROM tbl_workspace_m_tags
        WHERE ISNULL(flg, 0) <> 9 AND userweb_id = @UserId
        ";

        var tags = await connection.ExecuteQueryAsync<dynamic>(sql, new { UserId }, transaction: transaction);
        return tags.ToList();
    }

    public async Task<bool> HaveTagAsync(AuthorizationDto authorization, string tag_name)
    {
        string sql = @"
        SELECT 1 FROM tbl_workspace_m_tags
        WHERE tags_name = @tag_name AND userweb_id = @userweb_id AND ISNULL(flg, 0) <> 9
        ";

        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        IDbTransaction transaction = await connection.BeginTransactionAsync();

        var parameters = new
        {
            tag_name,
            userweb_id = authorization.Id,
        };

        int? result = await connection.ExecuteScalarAsync<int?>(sql, param: parameters, transaction: transaction);
        transaction.Commit();
        return result.HasValue;
    }

    public async Task<bool> InsertTagAsync(AuthorizationDto authorization, string tagName, string createBy)
    {
        string sql = @"
        INSERT INTO tbl_workspace_m_tags
        (tags_name, userweb_id, flg, create_by, create_date)
        SELECT @tagName, @UserWebId, 0, @CreateBy, @CreateDate
        WHERE NOT EXISTS (
            SELECT 1 FROM tbl_workspace_m_tags WHERE tags_name = @tagName AND userweb_id = @UserWebId AND ISNULL(flg, 0) <> 9
        )
        ";

        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        var parameters = new
        {
            tagName,
            UserWebId = authorization.Id,
            createBy,
            CreateDate = DateTime.Now,
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, param: parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<bool> InsertFolderAsync(AuthorizationDto authorization, FolderWorkSpaceDto folderWorkSpaceDto)
    {
        string sql = @"
        INSERT INTO tbl_workspace_m_folder
        (folder_name, space_id, folder_color, create_by, create_date,flg)
        SELECT @FolderName, @SpaceId, @FolderColor, @CreateBy, @CreateDate, 1
        WHERE NOT EXISTS (
            SELECT 1 FROM tbl_workspace_m_folder WHERE folder_name = @FolderName AND space_id = @SpaceId AND ISNULL(flg, 0) <> 9
        )
        ";

        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        var parameters = new
        {
            FolderName = folderWorkSpaceDto.folder_name,
            SpaceId = folderWorkSpaceDto.space_id,
            FolderColor = folderWorkSpaceDto.folder_color,
            CreateBy = folderWorkSpaceDto.create_by,
            CreateDate = DateTime.Now,
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, param: parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetFolderByIdAsync(string Constr, int SpaceId)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        SELECT folder_id, folder_name, folder_color
        FROM tbl_workspace_m_folder
        WHERE space_id = @SpaceId AND ISNULL(flg, 0) <> 9
        ";

        var parameters = new { SpaceId };
        var folders = await connection.ExecuteQueryAsync<dynamic>(sql, param: parameters, transaction: transaction);
        return folders.ToList();
    }

    public async Task<JsonElement?> GetFoldersAsync(string Constr, int SpaceId)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        DECLARE @space_id INT = @SpaceId;
   
        SELECT
            fd.folder_name,
            fd.folder_color,
            JSON_QUERY(COALESCE((
                SELECT
                    dc.doc_name,
                    dc.doc_description
                FROM dbo.tbl_workspace_m_document AS dc
                WHERE dc.folder_id = fd.folder_id
                AND ISNULL(dc.flg, 0) <> 9
                ORDER BY dc.doc_name
                FOR JSON PATH
            ), '[]')) AS documents
        FROM dbo.tbl_workspace_m_folder AS fd
        WHERE fd.space_id = @space_id
        AND ISNULL(fd.flg, 0) <> 9
        ORDER BY fd.folder_name
        FOR JSON PATH, INCLUDE_NULL_VALUES;
        ";

        var parameters = new { SpaceId };
        var folders = await connection.ExecuteScalarAsync<string>(sql, param: parameters, transaction: transaction);
        JsonElement? json = string.IsNullOrEmpty(folders) ? null : JsonSerializer.Deserialize<JsonElement>(folders);
        return json;
    }

    public async Task<bool> HaveDocumentAsync(AuthorizationDto authorization, string doc_name, int folder_id)
    {
        string sql = @"
        SELECT 1 FROM tbl_workspace_m_document
        WHERE doc_name = @doc_name AND folder_id = @folder_id AND ISNULL(flg, 0) <> 9
        ";

        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        IDbTransaction transaction = await connection.BeginTransactionAsync();

        var parameters = new
        {
            doc_name,
            folder_id,
        };

        int? result = await connection.ExecuteScalarAsync<int?>(sql, param: parameters, transaction: transaction);
        transaction.Commit();
        return result.HasValue;
    }

    public async Task<bool> InsertDocumentAsync(AuthorizationDto authorization, DocumentDto documentDto, string baseUrl)
    {
        var writtenFiles = new List<string>();
        DataTable files = new(tableName: "dbo.FileUploadType");
        files.Columns.AddRange([
            new DataColumn("file_name", typeof(string)),
            new DataColumn("file_type", typeof(string)),
            new DataColumn("file_path", typeof(string)),
            new DataColumn("create_by", typeof(string)),
            new DataColumn("create_date", typeof(DateTime))
        ]);

        try
        {
            string sql = @"
            INSERT INTO tbl_workspace_m_document
                (doc_name, doc_description, folder_id, created_by, create_date, flg)
            VALUES (@doc_name, @doc_description, @folder_id, @create_by, SYSUTCDATETIME(),1);

            DECLARE @doc_id INT = SCOPE_IDENTITY(); 

            INSERT INTO tbl_workspace_m_document_file
                (file_name, file_type, file_path, doc_id, create_by, create_date,flg)
            SELECT  f.file_name, f.file_type, f.file_path,@doc_id, f.create_by, f.create_date,1
            FROM    @files AS f;
            ";

            string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
            using SqlConnection connection = new(connectionString);
            await connection.EnsureOpenAsync();
            IDbTransaction transaction = await connection.BeginTransactionAsync();

            if (documentDto.doc_attachment is not null && documentDto.doc_attachment.Count > 0)
            {
                DateTime now = DateTime.Now;
                string folder_name = "workspace_file";
                string folderPath = Path.Combine(_env.ContentRootPath, folder_name);

                foreach (var file in documentDto.doc_attachment)
                {
                    string fileName = $"{Guid.NewGuid():N}{file.file_type}";
                    string filePath = Path.Combine(folderPath, fileName);
                    await Utils.Base64ToFile(file.file_base64, filePath);

                    string fileUrl = $"{baseUrl}/{folder_name}/{fileName}";

                    files.Rows.Add(file.file_name, file.file_type, fileUrl, documentDto.create_by, now);
                    writtenFiles.Add(filePath);
                }
            }

            var parameters = new
            {
                documentDto.doc_name,
                documentDto.doc_description,
                documentDto.folder_id,
                documentDto.create_by,
                files
            };

            await connection.ExecuteNonQueryAsync(sql, param: parameters, transaction: transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception)
        {
            foreach (var p in writtenFiles)
                File.Delete(p);
            return false;
        }

    }

    public async Task<dynamic?> ConvertImageAsync(string Constr, string baseUrl, UploadImageDto uploadImageDto)
    {
        var writtenFiles = new List<string>();
        DataTable files = new(tableName: "dbo.ImageCenterType");
        files.Columns.AddRange([
            new DataColumn("m_name", typeof(string)),
            new DataColumn("m_type", typeof(string)),
            new DataColumn("m_path", typeof(string)),
        ]);

        try
        {
            string sql = @"
            DECLARE @InsertedWSI TABLE(m_id INT);

            INSERT INTO tbl_media_center
                (m_name, m_type, m_path, flg)
            OUTPUT INSERTED.m_id INTO @InsertedWSI(m_id)
            SELECT f.m_name, f.m_type, f.m_path, 1
            FROM    @files AS f;

            SELECT m_name, m_type, m_path FROM tbl_media_center WHERE m_id IN (SELECT m_id FROM @InsertedWSI);
            ";

            string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(Constr);
            using SqlConnection connection = new(connectionString);
            await connection.EnsureOpenAsync();
            IDbTransaction transaction = await connection.BeginTransactionAsync();

            if (uploadImageDto.Images is not null && uploadImageDto.Images.Count > 0)
            {
                DateTime now = DateTime.Now;
                string folder_name = "MediaCenter";
                string folderPath = Path.Combine(_env.ContentRootPath, "..", folder_name);

                foreach (var img in uploadImageDto.Images)
                {
                    string fileName = $"{Guid.NewGuid():N}{img.Img_Type}";
                    string filePath = Path.Combine(folderPath, fileName);
                    await Utils.Base64ToFile(img.Img_Base64, filePath);

                    string fileUrl = $"{baseUrl}/{folder_name}/{fileName}";

                    files.Rows.Add(img.Img_Name, img.Img_Type, fileUrl);
                    writtenFiles.Add(filePath);
                }
            }

            var parameters = new
            {
                files
            };

            var MediaData = await connection.ExecuteQueryAsync(sql, param: parameters, transaction: transaction);
            transaction.Commit();
            return MediaData;
        }
        catch (Exception)
        {
            foreach (var p in writtenFiles)
                File.Delete(p);
            return null;
        }
    }

    public async Task<List<dynamic>> GetDocumentAsync(string Constr, int folder_id)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        SELECT doc_id, doc_name, doc_description
        FROM tbl_workspace_m_document
        WHERE folder_id = @folder_id AND ISNULL(flg, 0) <> 9
        ";

        var parameters = new { folder_id };
        var documents = await connection.ExecuteQueryAsync<dynamic>(sql, param: parameters, transaction: transaction);
        return documents.ToList();
    }

    public async Task<List<dynamic>> GetDocumentFileAsync(string Constr, int doc_id)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
        SELECT file_id, file_name, file_type, file_path
        FROM tbl_workspace_m_document_file
        WHERE doc_id = @doc_id AND ISNULL(flg, 0) <> 9
        ";

        var parameters = new { doc_id };
        var files = await connection.ExecuteQueryAsync<dynamic>(sql, param: parameters, transaction: transaction);
        return files.ToList();
    }
}

