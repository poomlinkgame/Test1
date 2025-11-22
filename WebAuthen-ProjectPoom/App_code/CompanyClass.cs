using System.Data;
using System.Text.Json;
using System.Transactions;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Data.SqlClient;
using RepoDb;
using RepoDb.Extensions;
using WebAuthen.Models;

namespace WebAuthen.App_code;

public class CompanyClass(DynamicConnectionService dynamicConnection)
{
    private readonly DynamicConnectionService _dynamicConnection = dynamicConnection;

    public async Task<bool> CompanyExists(AuthorizationDto authorization, string CompTaxId, string CompBranch)
    {
        try
        {
            string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
            string sql = @"
            SELECT comp_taxid
            FROM tbl_m_company_master
            WHERE ISNULL(comp_flg,0) NOT IN (9) AND comp_taxid = @comp_taxid AND comp_branch = @comp_branch";

            using SqlConnection connection = new(connectionString);
            using IDbTransaction transaction = connection.EnsureOpen().BeginTransaction();

            var parameters = new
            {
                comp_taxid = CompTaxId,
                comp_branch = CompBranch
            };

            List<dynamic> data = connection.ExecuteQuery(sql, parameters, transaction: transaction).ToList();
            transaction.Commit();

            if (data.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> InserDataCompany(AuthorizationDto authorization, CompanyDto company)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        string sql = @"
            DECLARE  @numrun nvarchar(2) = ( SELECT FORMAT ( CASE WHEN MAX(comp_no) is  null then 1 else  MAX(comp_no) +1  end  ,'d2') AS IDs FROM tbl_m_company_master );

            INSERT INTO tbl_m_company_master (comp_no,comp_name_th,comp_short_name,comp_address,comp_contract, comp_tel, comp_fax,comp_taxid, comp_flg, comp_branch, comp_web, comp_vat, comp_commercial_no,created_date,comp_image,comp_ssoid,userweb_id,comp_type_id,comp_emp_amount,created_by)
            SELECT @numrun,@CompNameTh,@CompShortName,@CompAddress,@CompContract,@CompTel, @CompFax,@CompTaxId, 1, @CompBranch, @CompWeb, @CompVat, @CompCommercialNo,getdate(),@CompImage,@CompSsoId,@userweb_id, @CompTypeId, @CompEmpAmount,@created_by
            WHERE NOT EXISTS (
                SELECT 1 FROM tbl_m_company_master
                WHERE (comp_taxid IS NOT NULL AND comp_taxid = @CompTaxId AND comp_taxid <> '' )AND comp_branch = @CompBranch  And ISNULL(comp_flg,0) <> 9
            )
            ";

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        byte[]? CompImage = string.IsNullOrEmpty(company.COMP_IMAGE) ? null : Convert.FromBase64String(company.COMP_IMAGE);

        var parameters = new
        {
            CompNameTh = company.COMP_NAME_TH,
            CompShortName = company.COMP_SHORT_NAME,
            CompContract = company.COMP_CONTRACT,
            CompAddress = company.COMP_ADDRESS,
            CompTel = company.COMP_TEL,
            CompFax = company.COMP_FAX,
            CompTaxId = company.COMP_TAX_ID,
            CompBranch = company.COMP_BRANCH,
            CompWeb = company.COMP_WEB,
            CompVat = company.COMP_VAT,
            CompCommercialNo = company.COMP_COMMERCIAL_NO,
            CompImage,
            CompSsoId = company.COMP_SSO_ID,
            userweb_id = authorization.Id,
            CompTypeId = company.COMP_TYPE_ID,
            CompEmpAmount = company.COMP_EMP_AMOUNT,
            company.created_by,
            created_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;

    }
    public async Task<bool> EditDataCompany(AuthorizationDto authorization, CompanyDto company)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_m_company_master
            SET comp_name_th = @CompNameTh,
                comp_short_name = @CompShortName,
                comp_address = @CompAddress,
                comp_contract = @CompContract,
                comp_tel = @CompTel,
                comp_fax = @CompFax,
                comp_web = @CompWeb,
                comp_vat = @CompVat,
                comp_commercial_no = @CompCommercialNo,
                comp_image = @CompImage,
                comp_ssoid = @CompSsoId,
                comp_type_id = @CompTypeId,
                comp_emp_amount = @CompEmpAmount,
                update_by = @update_by,
                update_date = @update_date,
                comp_taxid = @CompTaxId,
                comp_branch = @CompBranch
            WHERE userweb_id = @userweb_id AND comp_id = @comp_id AND ISNULL(comp_flg,0) <> 9
            AND NOT EXISTS (
                SELECT 1
                FROM tbl_m_company_master t
                WHERE t.comp_taxid = @CompTaxId
                AND ISNULL(t.comp_flg,0) <> 9
                AND t.comp_id <> @comp_id)";

        byte[]? CompImage = string.IsNullOrEmpty(company.COMP_IMAGE) ? null : Convert.FromBase64String(company.COMP_IMAGE);

        var parameters = new
        {
            CompNameTh = company.COMP_NAME_TH,
            CompShortName = company.COMP_SHORT_NAME,
            CompAddress = company.COMP_ADDRESS,
            CompContract = company.COMP_CONTRACT,
            CompTel = company.COMP_TEL,
            CompFax = company.COMP_FAX,
            CompWeb = company.COMP_WEB,
            CompVat = company.COMP_VAT,
            CompCommercialNo = company.COMP_COMMERCIAL_NO,
            CompImage,
            CompSsoId = company.COMP_SSO_ID,
            CompTaxId = company.COMP_TAX_ID,
            CompBranch = company.COMP_BRANCH,
            CompTypeId = company.COMP_TYPE_ID,
            CompEmpAmount = company.COMP_EMP_AMOUNT,
            userweb_id = authorization.Id,
            company.update_by,
            update_date = DateTime.Now,
            company.comp_id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> DataCompanyGetAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);

        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT comp_id,comp_no,comp_name_th,comp_short_name,comp_address,comp_contract, comp_tel, comp_fax,comp_taxid,comp_branch, comp_web, 
            comp_vat,comp_commercial_no,comp_image,comp_ssoid,t.comp_type_name,comp_emp_amount
            FROM tbl_m_company_master m
            INNER JOIN tbl_m_company_type t ON m.comp_type_id = t.comp_type_id
            WHERE userweb_id = @userweb_id AND ISNULL(comp_flg,0) NOT IN (9)";

        var parameters = new
        {
            userweb_id = authorization.Id
        };

        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return result.ToList();
    }

    public async Task<bool> DataCompanyDelete(AuthorizationDto authorization, CompanyDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"UPDATE tbl_m_company_master
                       SET comp_flg = 9, update_by = @update_by, update_date = @update_date
                       WHERE comp_id = @comp_id AND userweb_id = @userweb_id AND ISNULL(comp_flg,0) <> 9";
        var parameters = new
        {
            body.comp_id,
            userweb_id = authorization.Id,
            body.update_by,
            update_date = DateTime.Now
        };
        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetCompanyTypeAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT comp_type_id, comp_type_name
            FROM tbl_m_company_type";

        var result = await connection.ExecuteQueryAsync(sql, transaction: transaction);
        transaction.Commit();

        return result.ToList();
    }

    public async Task<bool> InsertCompanyTypeAsync(AuthorizationDto authorization, CompanyDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            INSERT INTO tbl_m_company_type (comp_type_name)
            VALUES (@comp_type_name)";

        var parameters = new
        {
            body.COMP_TYPE_NAME
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> EditCompanyTypeAsync(AuthorizationDto authorization, CompanyDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_m_company_type
            SET comp_type_name = @comp_type_name
            WHERE comp_type_id = @comp_type_id";

        var parameters = new
        {
            body.COMP_TYPE_NAME,
            comp_type_id = body.COMP_TYPE_ID
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetCompVatAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT comvat_id, comvat_type,comvat_name
            FROM tbl_m_company_vat
            WHERE ISNULL(comvat_flg,0) <> 9";

        var result = await connection.ExecuteQueryAsync(sql, transaction: transaction);
        transaction.Commit();

        return result.ToList();
    }

    public async Task<bool> InsertDepartmentAsync(AuthorizationDto authorization, CompDepartmentDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            MERGE INTO tbl_hrm_m_org_department AS tgt
            USING (SELECT
                @parent_id AS parent_id,
                @dep_name AS dep_name,
                @comp_id AS comp_id,
                @flg AS flg,
                @created_by AS created_by,
                @created_date as created_date,
                @userweb_id AS userweb_id
            ) AS src
            ON tgt.dep_name = src.dep_name AND ISNULL(tgt.flg, 0) <> 9 
            AND tgt.userweb_id = src.userweb_id

            WHEN MATCHED  AND ',' + tgt.comp_id + ',' NOT LIKE  '%,' + src.comp_id + ',%'
            THEN UPDATE SET 
                tgt.comp_id = tgt.comp_id + ',' +  src.comp_id,
                tgt.update_by = src.created_by,
                tgt.update_date = src.created_date

            WHEN NOT MATCHED 
            THEN INSERT(parent_id, dep_name, comp_id,flg,created_by, created_date,userweb_id)
            VALUES (src.parent_id, src.dep_name, src.comp_id, src.flg, src.created_by, src.created_date,src.userweb_id);
        ";

        // string sql = @"
        //     INSERT INTO tbl_hrm_m_org_department 
        //             (parent_id, dep_name, comp_id,flg,created_by, created_date)
        //     SELECT @parent_id, @dep_name, @comp_id,1,@created_by, @created_date
        //     WHERE NOT EXISTS (
        //         SELECT 1 FROM tbl_hrm_m_org_department
        //         WHERE dep_name = @dep_name AND ',' + comp_id + ',' LIKE '%,' + @comp_id + ',%' AND ISNULL(flg, 0) <> 9
        //     )";

        var comp_id = body.comp_id.ToString();

        var parameters = new
        {
            body.parent_id,
            body.dep_name,
            comp_id,
            body.created_by,
            created_date = DateTime.Now,
            flg = 1,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> EditDepartmentAsync(AuthorizationDto authorization, CompDepartmentDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_org_department
            SET parent_id = @parent_id,
                dep_name = @dep_name,
                update_by = @update_by,
                update_date = @update_date
            WHERE dep_id = @dep_id AND userweb_id = @userweb_id AND ISNULL(flg, 0) <> 9 
            ";

        var parameters = new
        {
            body.parent_id,
            body.dep_name,
            body.dep_id,
            body.update_by,
            update_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetDataDepartmentAsync(AuthorizationDto authorization, string? comp_id = null)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = "";
        if (comp_id == null)
        {
            sql = @"
            SELECT dep_id, parent_id, dep_name
            FROM tbl_hrm_m_org_department
            WHERE ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id
            ";
        }
        else
        {
            sql = @"
            SELECT dep_id, parent_id, dep_name
            FROM tbl_hrm_m_org_department
            WHERE ',' + comp_id + ',' LIKE '%,' + @comp_id + ',%' AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id
            ";
        }

        var parameters = new
        {
            comp_id,
            userweb_id = authorization.Id
        };

        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result.ToList();

    }

    public async Task<bool> DeleteDepartmentAsync(AuthorizationDto authorization, CompdepartmentDeleteAllDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_org_department
            SET flg = 9, update_by = @update_by, update_date = @update_date
            WHERE dep_id = @dep_id AND userweb_id = @userweb_id AND ISNULL(flg, 0) <> 9";

        var parameters = new
        {
            body.dep_id,
            body.update_by,
            userweb_id = authorization.Id,
            update_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteDepartmentfromecompAsync(AuthorizationDto authorization, CompdepartmentDeleteAllDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"UPDATE d
            SET comp_id = NULLIF((
                SELECT STRING_AGG(x.v, ',') WITHIN GROUP (ORDER BY TRY_CONVERT(int, x.v))
                FROM (
                    SELECT DISTINCT LTRIM(RTRIM(value)) AS v
                    FROM STRING_SPLIT(d.comp_id, ',')
                    WHERE LTRIM(RTRIM(value)) <> ''
                    AND LTRIM(RTRIM(value)) <> @comp_id
                ) AS x
            ), '')
            FROM dbo.tbl_hrm_m_org_department AS d
            WHERE d.dep_id = @dep_id AND d.userweb_id = @userweb_id AND ISNULL(d.flg,0) <> 9";

        var parameters = new
        {
            body.dep_id,
            comp_id = body.comp_id.ToString(),
            body.update_by,
            userweb_id = authorization.Id,
            update_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> PositionInsertAsync(AuthorizationDto authorization, PositionDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
                MERGE INTO tbl_hrm_m_org_position AS tgt
                USING (SELECT
                    @pos_name_th AS pos_name_th,
                    @comp_id AS comp_id,
                    @flg AS flg,
                    @created_by AS created_by,
                    @created_date as created_date,
                    @userweb_id AS userweb_id
                ) AS src
                ON tgt.pos_name_th = src.pos_name_th AND ISNULL(tgt.flg, 0) <> 9 
                AND tgt.userweb_id = src.userweb_id

                WHEN MATCHED  AND ',' + tgt.comp_id + ',' NOT LIKE  '%,' + src.comp_id + ',%'
                THEN UPDATE SET 
                    tgt.comp_id = tgt.comp_id + ',' +  src.comp_id,
                    tgt.update_by = src.created_by,
                    tgt.update_date = src.created_date

                WHEN NOT MATCHED 
                THEN INSERT(pos_name_th, comp_id,flg, created_by, created_date,userweb_id)
                VALUES (src.pos_name_th, src.comp_id, src.flg, src.created_by, src.created_date,src.userweb_id);
            ";

        var comp_id = body.comp_id.ToString();

        var parameters = new
        {
            body.pos_name_th,
            body.created_by,
            comp_id,
            flg = 1,
            created_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetDataPositionAsync(AuthorizationDto authorization, string? comp_id = null)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();
        string sql = "";

        if (comp_id == null)
        {
            sql = @"
            SELECT pos_id, pos_name_th
            FROM tbl_hrm_m_org_position
            WHERE ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id
            ";
        }
        else
        {
            sql = @"
            SELECT pos_id, pos_name_th
            FROM tbl_hrm_m_org_position
            WHERE ',' + comp_id + ',' LIKE '%,' + @comp_id + ',%' AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id
        ";
        }

        var parameters = new
        {
            comp_id,
            userweb_id = authorization.Id
        };

        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result.ToList();
    }

    public async Task<bool> PositionEditAsync(AuthorizationDto authorization, PositionDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_org_position
            SET pos_name_th = @pos_name_th,
                update_by = @update_by,
                update_date = @update_date
            WHERE pos_id = @pos_id AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id";

        var parameters = new
        {
            body.pos_name_th,
            body.pos_id,
            body.update_by,
            update_date = DateTime.Now,
            userweb_id = authorization.Id
        };
        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<bool> PositionDeleteAsync(AuthorizationDto authorization, PositionDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_org_position
            SET flg = 9, update_by = @update_by, update_date = @update_date
            WHERE pos_id = @pos_id AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id";

        var parameters = new
        {
            body.pos_id,
            body.update_by,
            update_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> PositionDeleteFromCompAsync(AuthorizationDto authorization, PositionDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"UPDATE p
            SET comp_id = NULLIF((
                SELECT STRING_AGG(x.v, ',') WITHIN GROUP (ORDER BY TRY_CONVERT(int, x.v))
                FROM (
                    SELECT DISTINCT LTRIM(RTRIM(value)) AS v
                    FROM STRING_SPLIT(p.comp_id, ',')
                    WHERE LTRIM(RTRIM(value)) <> ''
                    AND LTRIM(RTRIM(value)) <> @comp_id
                ) AS x
            ), '')
            FROM dbo.tbl_hrm_m_org_position AS p
            WHERE p.pos_id = @pos_id AND p.userweb_id = @userweb_id AND ISNULL(p.flg,0) <> 9";

        var parameters = new
        {
            body.pos_id,
            comp_id = body.comp_id.ToString(),
            body.update_by,
            update_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetCompStructureAsync(AuthorizationDto authorization, int comp_id)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT 
                s.org_id,
                s.parent_id,
                d.dep_name,
                p.pos_name_th
            FROM tbl_hrm_m_org_structure s
            JOIN tbl_hrm_m_org_department d ON s.dep_id = d.dep_id
            JOIN tbl_hrm_m_org_position p ON s.pos_id = p.pos_id
            WHERE s.comp_id = @comp_id 
            AND ISNULL(s.flg, 0) <> 9
            AND ISNULL(d.flg, 0) <> 9
            AND ISNULL(p.flg, 0) <> 9";

        var parameters = new
        {
            comp_id
        };

        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result.ToList();
    }

    public async Task<bool> InsertCompStructureAsync(AuthorizationDto authorization, CompStructureDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            INSERT INTO tbl_hrm_m_org_structure 
            (parent_id, pos_id, dep_id, comp_id,flg,created_by, created_date)
            VALUES 
            (@parent_id, @pos_id, @dep_id, @comp_id,1,@created_by, @created_date)";

        var parameters = new
        {
            body.parent_id,
            body.pos_id,
            body.dep_id,
            body.comp_id,
            body.created_by,
            created_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> EditCompStructureAsync(AuthorizationDto authorization, CompStructureDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_org_structure
            SET parent_id = @parent_id,
                pos_id = @pos_id,
                dep_id = @dep_id,
                update_by = @update_by,
                update_date = @update_date
            WHERE org_id = @org_id AND ISNULL(flg, 0) <> 9";

        var parameters = new
        {
            body.parent_id,
            body.pos_id,
            body.dep_id,
            body.org_id,
            body.update_by,
            update_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteCompStructureAsync(AuthorizationDto authorization, CompStructureDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_org_structure
            SET flg = 9, update_by = @update_by, update_date = @update_date
            WHERE org_id = @org_id AND ISNULL(flg, 0) <> 9";

        var parameters = new
        {
            body.org_id,
            body.update_by,
            update_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetMainCalendarAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"WITH a AS (
                    SELECT c_id , c_name , c_parent_id
                    FROM tbl_hrm_m_calendar 
                    WHERE c_parent_id IS NULL AND userweb_id = @userweb_id AND ISNULL(flg, 0) <> 9

                    UNION ALL 

                    SELECT c.c_id , c.c_name , c.c_parent_id
                    FROM a
                    INNER JOIN tbl_hrm_m_calendar c ON c.c_parent_id = a.c_id
                    WHERE ISNULL(c.flg,0) <> 9
                    )
                    SELECT * 
                    FROM a";
        var parameters = new
        {
            userweb_id = authorization.Id
        };

        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result.ToList();
    }

    public async Task<bool> InsertMainCalendarAsync(AuthorizationDto authorization, MainCalendarDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            INSERT INTO tbl_hrm_m_calendar 
            (c_name, c_parent_id,flg, created_by, created_date,userweb_id)
            SELECT 
            @c_name, @c_parent_id,0, @created_by, @created_date,@userweb_id
            WHERE NOT EXISTS (
                SELECT 1 FROM tbl_hrm_m_calendar
                WHERE c_name = @c_name AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id
            )";

        var parameters = new
        {
            body.c_name,
            body.c_parent_id,
            body.created_by,
            created_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> EditMainCalendarAsync(AuthorizationDto authorization, MainCalendarDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_calendar
            SET c_name = @c_name,
                c_parent_id = @c_parent_id,
                update_by = @update_by,
                update_date = @update_date
            WHERE c_id = @c_id AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id";

        var parameters = new
        {
            body.c_name,
            body.c_parent_id,
            body.c_id,
            body.update_by,
            update_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteMainCalendarAsync(AuthorizationDto authorization, MainCalendarDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_calendar
            SET flg = 9, update_by = @update_by, update_date = @update_date
            WHERE c_id = @c_id AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id";

        var parameters = new
        {
            body.c_id,
            body.update_by,
            update_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetHolidayTypeAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT h_type_id, h_type_name, is_holiday, h_color
            FROM tbl_hrm_m_holiday_type
            WHERE ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id";

        var parameters = new
        {
            userweb_id = authorization.Id
        };

        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result.ToList();
    }

    public async Task<bool> InsertHolidayTypeAsync(AuthorizationDto authorization, holidaytypeDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            INSERT INTO tbl_hrm_m_holiday_type 
            (h_type_name, is_holiday,flg, h_color, created_by, created_date,userweb_id)
            SELECT
            @h_type_name, @is_holiday,0, @h_color, @created_by, @created_date, @userweb_id
            WHERE NOT EXISTS (
                SELECT 1 FROM tbl_hrm_m_holiday_type
                WHERE h_type_name = @h_type_name AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id
            )";

        var parameters = new
        {
            body.h_type_name,
            body.is_holiday,
            body.h_color,
            body.created_by,
            created_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> EditHolidayTypeAsync(AuthorizationDto authorization, holidaytypeDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_holiday_type
            SET h_type_name = @h_type_name,
                is_holiday = @is_holiday,
                h_color = @h_color,
                update_by = @update_by,
                update_date = @update_date
            WHERE h_type_id = @h_type_id AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id";

        var parameters = new
        {
            body.h_type_name,
            body.is_holiday,
            body.h_color,
            body.h_type_id,
            body.update_by,
            update_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteHolidayTypeAsync(AuthorizationDto authorization, holidaytypeDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_m_holiday_type
            SET flg = 9, update_by = @update_by, update_date = @update_date
            WHERE h_type_id = @h_type_id AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id";

        var parameters = new
        {
            body.h_type_id,
            body.update_by,
            update_date = DateTime.Now,
            userweb_id = authorization.Id
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<dynamic> GetHolidayAsync(AuthorizationDto authorization, holidayDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT 
                h.h_id, 
                h.h_date, 
                h.h_detail,
                t.h_type_id, 
                t.h_type_name,
                t.h_color
            FROM tbl_hrm_m_holiday h
                INNER JOIN tbl_hrm_m_holiday_type t ON h.h_type_id = t.h_type_id
                INNER JOIN tbl_hrm_m_calendar c ON c.c_id = h.c_id
            WHERE c.c_id = @c_id
                AND ISNULL(h.flg, 0) <> 9
                ORDER BY h.h_date
                    ";

        var parameters = new
        {
            body.c_id
        };

        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return result;
    }

    public async Task<bool> InsertHolidayAsync(AuthorizationDto authorization, holidayDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        /* string sql = @"
               DECLARE   @arrival_date date = @Start_Date,@leaving_date date = @End_Date;

               UPDATE tbl_hrm_m_holiday set flg = 9 
               from tbl_hrm_m_holiday 
               where cast(h_date as date) between @Start_Date and @End_Date and isnull(flg,0) != 9
               and ( c_id = @Calendar_ID or c_id in (select c_id from tbl_hrm_m_calendar where c_parent_id = @Calendar_ID));

               WITH cte AS (
                   SELECT @Calendar_ID as c_id ,  CAST(@arrivaldate as date) as date
                   UNION ALL
                   SELECT @Calendar_ID as c_id , CAST(DATEADD(day,1,date) as date) as date
                   FROM cte
                   WHERE date  < @leaving_date
               ), ctes as (
                   SELECT cte.c_id , cte.date
                   FROM cte 
                   UNION ALL
                   SELECT tbl_hrm_m_calendar.c_id , cte.date
                   FROM cte inner join tbl_hrm_m_calendar on cte.c_id = tbl_hrm_m_calendar.c_parent_id
               )

               INSERT into  tbl_hrm_m_holiday (c_id, h_date, h_detail, flg, Created_by, Created_date, h_type_id)
               SELECT ctes.c_id , ctes.date , @CalenSpecial_Detail, 0, @Created_by, getdate() , @CalenWKType_ID
               FROM ctes 
               left join tbl_hrm_m_holiday on ctes.c_id = tbl_hrm_m_holiday.c_id and cast(ctes.date as date) = cast(tbl_hrm_m_holiday.h_date as date) and isnull(tbl_hrm_m_holiday.flg , 0) != 9
               where tbl_hrm_m_holiday.h_date is null 
               order by ctes.c_id , date
               OPTION (MAXRECURSION 0)"; */
        string sql = @"
            
            DECLARE @new_holiday TABLE(c_id INT, h_date DATE);

            WITH date_list AS (
                SELECT TRY_CAST(value AS DATE) AS h_date
                FROM STRING_SPLIT(@DateListString, ',')
                WHERE TRY_CAST(value AS DATE) IS NOT NULL
            ), all_cals AS (
                SELECT @Calendar_ID AS c_id, h_date FROM date_list
                UNION ALL
                SELECT c.c_id, d.h_date
                FROM tbl_hrm_m_calendar c
                JOIN date_list d ON 1=1
                WHERE c.c_parent_id = @Calendar_ID
            )

            INSERT INTO @new_holiday
            SELECT * FROM all_cals;

            UPDATE tbl_hrm_m_holiday
            SET flg = 9, update_by = @update_by, update_date = GETDATE()
            FROM tbl_hrm_m_holiday h
            JOIN @new_holiday a ON a.c_id = h.c_id AND CAST(h.h_date AS DATE) = a.h_date
            WHERE ISNULL(h.flg, 0) <> 9;

            INSERT INTO tbl_hrm_m_holiday (c_id, h_date, h_detail, flg, Created_by, Created_date, h_type_id)
            SELECT a.c_id, a.h_date, @h_detail, 0, @Created_by, GETDATE(), @h_type_id
            FROM @new_holiday a
            LEFT JOIN tbl_hrm_m_holiday h
                ON h.c_id = a.c_id AND CAST(h.h_date AS DATE) = a.h_date AND ISNULL(h.flg, 0) <> 9
            WHERE h.h_date IS NULL
            ORDER BY a.c_id, a.h_date;
        ";
        var parameters = new
        {
            Calendar_ID = body.c_id,
            DateListString = body.h_date,
            body.h_type_id,
            body.h_detail,
            body.created_by,
            body.update_by
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteHolidayAsync(AuthorizationDto authorization, holidayDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"WITH a AS (
                    SELECT c_id
                    FROM tbl_hrm_m_calendar 
                    WHERE c_id = @c_id
                    UNION ALL 
                    SELECT c.c_id 
                    FROM tbl_hrm_m_calendar c
                    WHERE c.c_parent_id = @c_id
                )

                UPDATE tbl_hrm_m_holiday
                SET flg = 9 , update_by = @update_by, update_date = @update_date
                WHERE c_id IN (Select * FROM a) AND h_date = @h_date AND ISNULL(flg,0) <> 9;";

        var parameters = new
        {
            body.c_id,
            body.h_date,
            body.update_by,
            update_date = DateTime.Now
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();

        return rowsAffected > 0;
    }

    public async Task<bool> InsertWorkshiftAsync(AuthorizationDto authorization, WorkshiftRequestDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        DataTable wsItemTable = new(tableName: "dbo.workshift_item");
        DataTable timeBreakTable = new(tableName: "dbo.time_break");
        DataTable otBreakTable = new(tableName: "dbo.ot_break");

        wsItemTable.Columns.AddRange([
            new DataColumn("ws_id", typeof(int)),
            new DataColumn("wsi_set", typeof(int)),
            new DataColumn("time_in", typeof(TimeSpan)),
            new DataColumn("time_in_day", typeof(int)),
            new DataColumn("time_out", typeof(TimeSpan)),
            new DataColumn("time_out_day", typeof(int)),
            new DataColumn("time_hour", typeof(int)),
            new DataColumn("time_minute", typeof(int)),
        ]);

        timeBreakTable.Columns.AddRange(
        [
            new DataColumn("wsi_id", typeof(int)),
            new DataColumn("tb_in", typeof(TimeSpan)),
            new DataColumn("tb_in_day", typeof(int)),
            new DataColumn("tb_out", typeof(TimeSpan)),
            new DataColumn("tb_out_day", typeof(int)),
            new DataColumn("auto_cut", typeof(int)),
            new DataColumn("tb_start_hour", typeof(int)),
            new DataColumn("tb_start_minute", typeof(int)),
            new DataColumn("tb_minute", typeof(int)),
        ]);

        otBreakTable.Columns.AddRange(
        [
            new DataColumn("ot_break_type", typeof(int)),
            new DataColumn("ws_id", typeof(int)),
            new DataColumn("ot_break_in", typeof(TimeSpan)),
            new DataColumn("ot_break_in_day", typeof(int)),
            new DataColumn("ot_break_out", typeof(TimeSpan)),
            new DataColumn("ot_break_out_day", typeof(int)),
            new DataColumn("auto_cut", typeof(int)),
            new DataColumn("ot_break_minute", typeof(int)),
            new DataColumn("ot_break_strart_hour", typeof(int)),
            new DataColumn("ot_break_start_minute", typeof(int))
        ]);

        string wsSql = @"
            DECLARE @NextNo int
            SELECT @NextNo = ISNULL(MAX(CAST(SUBSTRING(ws_code, 3,4) AS int)), 0) + 1
            FROM tbl_hrm_m_workshift WITH (UPDLOCK, HOLDLOCK)

            DECLARE @runorderno NVARCHAR(20) = 'WS' + FORMAT(@NextNo, 'd4');

            INSERT INTO tbl_hrm_m_workshift 
            (ws_code, ws_name, ws_type, ws_color, ws_remark, auto_time, flg, created_by, created_date,userweb_id)
            SELECT 
            @runorderno, @ws_name, @ws_type, @ws_color, @ws_remark, @auto_time, @flg, @created_by, GETDATE(), @userweb_id
            WHERE NOT EXISTS (
                SELECT 1 FROM tbl_hrm_m_workshift
                WHERE ws_name = @ws_name AND ISNULL(flg, 0) <> 9 AND userweb_id = @userweb_id
            );

            SELECT SCOPE_IDENTITY();
        ";

        var wsParameters = new
        {
            body.ws_name,
            body.ws_type,
            body.ws_color,
            body.ws_remark,
            body.auto_time,
            flg = 0,
            created_by = body.username,
            userweb_id = authorization.Id
        };

        int newWsId = await connection.ExecuteScalarAsync<int>(wsSql, wsParameters, transaction: transaction);

        if (newWsId == 0)
        {
            transaction.Rollback();
            return false;
        }

        if (!body.ot_breaks.IsNullOrEmpty())
        {
            foreach (var otb in body.ot_breaks!)
            {
                TimeSpan? otbIn = otb.ot_break_in.IsNullOrEmpty() ? null : TimeSpan.Parse(otb.ot_break_in!);
                TimeSpan? otbOut = otb.ot_break_out.IsNullOrEmpty() ? null : TimeSpan.Parse(otb.ot_break_out!);
                otBreakTable.Rows.Add(
                    otb.ot_break_type,
                    newWsId,
                    otbIn,
                    otb.ot_break_in_day,
                    otbOut,
                    otb.ot_break_out_day,
                    otb.auto_cut,
                    otb.ot_break_minute,
                    otb.ot_break_start_hour,
                    otb.ot_break_start_minute
                );
            }

            string otbSql = @"
                INSERT INTO tbl_hrm_m_ot_break 
                (ws_id, ot_break_type, ot_break_in, ot_break_in_day, ot_break_out, ot_break_out_day, auto_cut, created_by, created_date, flg)
                SELECT ws_id, ot_break_type, ot_break_in, ot_break_in_day, ot_break_out, ot_break_out_day, auto_cut, @created_by, GETDATE(), @flg
                FROM @otBreakTable;
            ";

            var otbParameters = new
            {
                otBreakTable,
                flg = 0,
                created_by = body.username,
            };

            int rowsAffected = await connection.ExecuteNonQueryAsync(otbSql, otbParameters, transaction: transaction);

        }

        if (!body.shift_items.IsNullOrEmpty())
        {
            foreach (var wsi in body.shift_items!)
            {
                TimeSpan? timeIn = wsi.time_in.IsNullOrEmpty() ? null : TimeSpan.Parse(wsi.time_in!);
                TimeSpan? timeOut = wsi.time_out.IsNullOrEmpty() ? null : TimeSpan.Parse(wsi.time_out!);
                wsItemTable.Rows.Add(
                    newWsId,
                    wsi.wsi_set,
                    timeIn,
                    wsi.time_in_day,
                    timeOut,
                    wsi.time_out_day,
                    wsi.time_hours,
                    wsi.time_minute
                );

                if (!wsi.time_breaks.IsNullOrEmpty())
                {
                    foreach (var tb in wsi.time_breaks!)
                    {
                        TimeSpan? tbIn = tb.tb_in.IsNullOrEmpty() ? null : TimeSpan.Parse(tb.tb_in!);
                        TimeSpan? tbOut = tb.tb_out.IsNullOrEmpty() ? null : TimeSpan.Parse(tb.tb_out!);
                        timeBreakTable.Rows.Add(
                            wsi.wsi_set,
                            tbIn,
                            tb.tb_in_day,
                            tbOut,
                            tb.tb_out_day,
                            tb.auto_cut,
                            tb.tb_start_hour,
                            tb.tb_start_minute,
                            tb.tb_minute
                        );
                    }
                }
            }

            string wsiSql = @"
                DECLARE @InsertedWSI TABLE(wsi_id INT, wsi_set INT);

                INSERT INTO tbl_hrm_m_workshift_item 
                (ws_id, wsi_set, time_in, time_in_day, time_out, time_out_day, created_by, created_date, flg)
                OUTPUT INSERTED.wsi_id, INSERTED.wsi_set INTO @InsertedWSI(wsi_id, wsi_set)
                SELECT ws_id, wsi_set, time_in, time_in_day, time_out, time_out_day, @created_by, GETDATE(), @flg
                FROM @wsItemTable;

                INSERT INTO tbl_hrm_m_timebreak 
                (wsi_id, tb_in, tb_in_day, tb_out, tb_out_day, auto_cut, created_by, created_date, flg)
                SELECT i.wsi_id, t.tb_in, t.tb_in_day, t.tb_out, t.tb_out_day, t.auto_cut, @created_by, GETDATE(), @flg
                FROM @timeBreakTable t
                JOIN @InsertedWSI i ON t.wsi_id = i.wsi_set;
            ";

            var wsiParameters = new
            {
                wsItemTable,
                timeBreakTable,
                flg = 0,
                created_by = body.username
            };

            int rowsAffected = await connection.ExecuteNonQueryAsync(wsiSql, wsiParameters, transaction: transaction);
        }


        transaction.Commit();
        return true;
    }

    public async Task<JsonElement?> GetWorkshiftDetailAsync(string constr, int userweb_id, int ws_id)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            DECLARE @ws_id INT = @p_ws_id;
            DECLARE @userweb_id INT = @p_userweb_id;

            SELECT
            (
                SELECT
                    ws.ws_name                             AS [ws_name],
                    ws.ws_color                            AS [ws_color],
                    /* ถ้า ws_color เก็บเป็น INT แล้วอยากแปลงเป็น 0xAARRGGBB:
                    CONCAT('0x', RIGHT('00000000' + CONVERT(varchar(8), CONVERT(varbinary(4), ws.ws_color), 2), 8)) AS [ws_color], */
                    ws.ws_type                             AS [ws_type],
                    ws.ws_remark                           AS [ws_remark],
                    ws.auto_time                           AS [auto_time],

                    /* shift_items [] */
                    (
                        SELECT
                            wsi.wsi_id                                    AS [wsi_id],
                            wsi.wsi_set                                   AS [wsi_set],
                            CONVERT(varchar(8), wsi.time_in ,108)         AS [time_in],
                            CONVERT(varchar(8), wsi.time_out,108)         AS [time_out],
                            wsi.time_in_day                               AS [time_in_day],
                            wsi.time_out_day                              AS [time_out_day],

                            /* time_breaks [] ต่อ item */
                            (
                                SELECT
                                    tb.tb_id                              AS [tb_id],
                                    CONVERT(varchar(8), tb.tb_in ,108)    AS [tb_in],
                                    CONVERT(varchar(8), tb.tb_out,108)    AS [tb_out],
                                    tb.auto_cut                           AS [auto_cut],
                                    tb.tb_in_day                          AS [tb_in_day],
                                    tb.tb_out_day                         AS [tb_out_day]
                                FROM dbo.tbl_hrm_m_timebreak AS tb
                                WHERE tb.wsi_id = wsi.wsi_id AND ISNULL(tb.flg,0) <> 9
                                ORDER BY tb.tb_in, tb.tb_out
                                FOR JSON PATH
                            ) AS [time_breaks]
                        FROM dbo.tbl_hrm_m_workshift_item AS wsi
                        WHERE wsi.ws_id = ws.ws_id AND ISNULL(wsi.flg,0) <> 9
                        ORDER BY wsi.wsi_set
                        FOR JSON PATH
                    ) AS [shift_items],

                    /* ot_breaks [] */
                    (
                        SELECT
                            ot.ot_break_id                            AS [ot_break_id],
                            CONVERT(varchar(8), ot.ot_break_in ,108)  AS [ot_break_in],
                            CONVERT(varchar(8), ot.ot_break_out,108)  AS [ot_break_out],
                            ot.ot_break_type                          AS [ot_break_type],
                            ot.auto_cut                               AS [auto_cut],
                            ot.ot_break_in_day                        AS [ot_break_in_day],
                            ot.ot_break_out_day                       AS [ot_break_out_day]
                        FROM dbo.tbl_hrm_m_ot_break AS ot
                        WHERE ot.ws_id = ws.ws_id AND ISNULL(ot.flg,0) <> 9
                        ORDER BY ot.ot_break_in, ot.ot_break_out
                        FOR JSON PATH
                    ) AS [ot_breaks]

                FROM dbo.tbl_hrm_m_workshift AS ws
                WHERE ws.ws_id = @ws_id AND ws.userweb_id = @userweb_id AND ISNULL(ws.flg,0) <> 9
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ) AS json;
        ";

        var parameters = new
        {
            p_ws_id = ws_id,
            p_userweb_id = userweb_id
        };

        string? result = await connection.ExecuteScalarAsync<string>(sql, parameters, transaction: transaction);
        transaction.Commit();

        JsonElement? json = string.IsNullOrEmpty(result) ? null : JsonSerializer.Deserialize<dynamic>(result)!;

        return json;
    }

    public async Task<dynamic> GetWorkshiftAsync(string constr, int userweb_id)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT 
                ws_id,
                ws_name,
                CASE ws_type
                    WHEN 1 then N'กะปกติ'
                    WHEN 2 then N'ระบุจำนวนชั่วโมง'
                    ELSE N'ไม่ระบุ'
                END AS ws_type,
                ws_color
            FROM tbl_hrm_m_workshift
            WHERE userweb_id = @userweb_id AND ISNULL(flg, 0) <> 9
        ";

        var parameters = new
        {
            userweb_id
        };

        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result;
    }

    public async Task<bool> InsertpersonalAsync(AuthorizationDto authorization, personalDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            INSERT INTO tbl_hrm_personal 
            (title_id, id_stamp, personal_code,firstname_th,lastname_th,id_card,flg,sex,nationality,race,religion,dateofbirth
            ,tel,bankacc,bank_id,status_id,address,address2,disability_id,emptype_id,date_start,date_out,org_id,cost_id,user_id,edu_id
            ,edu_details,hosp_right,sso_no,tax_no,nickname,created_by,created_date,userweb_id)
            SELECT
            @title_id, @id_stamp, @personal_code,@firstname_th,@lastname_th,@id_card,0,@sex,@nationality,@race,@religion,@dateofbirth
            ,@tel,@bankacc,@bank_id,@status_id,@address,@address2,@disability_id,@emptype_id,@date_start,@date_out,@org_id,@cost_id,@user_id,@edu_id
            ,@edu_details,@hosp_right,@sso_no,@tax_no,@nickname,@created_by,GETDATE(), @userweb_id
            WHERE NOT EXISTS (
                SELECT 1 FROM tbl_hrm_personal
                WHERE personal_code = @personal_code AND ISNULL(flg, 0) <> 9 
            )";

        var parameters = new
        {
            body.title_id,
            body.id_stamp,
            body.personal_code,
            body.firstname_th,
            body.lastname_th,
            body.id_card,
            body.dateofbirth,
            body.tel,
            body.sex,
            body.nationality,
            body.race,
            body.religion,
            body.bankacc,
            body.bank_id,
            body.status_id,
            body.address,
            body.address2,
            body.disability_id,
            body.emptype_id,
            body.date_start,
            body.date_out,
            body.org_id,
            body.cost_id,
            body.user_id,
            body.edu_id,
            body.edu_details,
            body.hosp_right,
            body.sso_no,
            body.tax_no,
            body.nickname,
            userweb_id = authorization.Id,
            body.created_by,
            body.update_by
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<List<dynamic>> GetpersonalAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT personal_id,title_id, id_stamp, personal_code,firstname_th,lastname_th,id_card,flg,sex,nationality,race,religion,dateofbirth
            ,tel,bankacc,bank_id,status_id,address,address2,disability_id,emptype_id,date_start,date_out,org_id,cost_id,user_id,edu_id
            ,edu_details,hosp_right,sso_no,tax_no,nickname
            FROM tbl_hrm_personal
            WHERE userweb_id = @userweb_id AND ISNULL(flg, 0) <> 9 ";

        var parameters = new
        {
            userweb_id = authorization.Id
        };
        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result.ToList();
    }

    public async Task<bool> EditpersonalAsync(AuthorizationDto authorization, personalDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_personal
            SET title_id = @title_id,
                id_stamp = @id_stamp,
                personal_code = @personal_code,
                firstname_th = @firstname_th,
                lastname_th = @lastname_th,
                id_card = @id_card,
                sex = @sex,
                nationality = @nationality,
                race = @race,
                religion = @religion,
                dateofbirth = @dateofbirth,
                tel = @tel,
                bankacc = @bankacc,
                bank_id = @bank_id,
                status_id = @status_id,
                address = @address,
                address2 = @address2,
                disability_id = @disability_id,
                emptype_id = @emptype_id,
                date_start = @date_start,
                date_out = @date_out,
                org_id = @org_id,
                cost_id = @cost_id,
                user_id = @user_id,
                edu_id = @edu_id,
                edu_details = @edu_details,
                hosp_right = @hosp_right,
                sso_no = @sso_no,
                tax_no = @tax_no,
                nickname = @nickname,
                update_by = @update_by,
                update_date = GETDATE()
            WHERE personal_id = @personal_id AND userweb_id = @userweb_id AND ISNULL(flg, 0) <> 9";

        var parameters = new
        {
            body.personal_id,
            body.title_id,
            body.id_stamp,
            body.personal_code,
            body.firstname_th,
            body.lastname_th,
            body.id_card,
            body.dateofbirth,
            body.tel,
            body.sex,
            body.nationality,
            body.race,
            body.religion,
            body.bankacc,
            body.bank_id,
            body.status_id,
            body.address,
            body.address2,
            body.disability_id,
            body.emptype_id,
            body.date_start,
            body.date_out,
            body.org_id,
            body.cost_id,
            body.user_id,
            body.edu_id,
            body.edu_details,
            body.hosp_right,
            body.sso_no,
            body.tax_no,
            body.nickname,
            body.update_by,
            userweb_id = authorization.Id,
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }

    public async Task<bool> DeletepersonalAsync(AuthorizationDto authorization, personalDto body)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            UPDATE tbl_hrm_personal
            SET flg = 9, update_by = @update_by, update_date = GETDATE()
            WHERE personal_id = @personal_id AND userweb_id = @userweb_id AND ISNULL(flg, 0) <> 9";

        var parameters = new
        {
            body.personal_id,
            body.update_by,
            userweb_id = authorization.Id,
        };

        int rowsAffected = await connection.ExecuteNonQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return rowsAffected > 0;
    }


    public async Task<List<dynamic>> GetStatusAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT status_id,status_name
            FROM tbl_hrm_m_status
            WHERE ISNULL(flg, 0) <> 9 ";

        var parameters = new
        {
            userweb_id = authorization.Id
        };
        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result.ToList();
    }

    public async Task<List<dynamic>> GetEducationAsync(AuthorizationDto authorization)
    {
        string? connectionString = await _dynamicConnection.GetConnectionStringByNameAsync(authorization.Constr);
        using SqlConnection connection = new(connectionString);
        await connection.EnsureOpenAsync();
        using IDbTransaction transaction = await connection.BeginTransactionAsync();

        string sql = @"
            SELECT edu_id,edu_name
            FROM tbl_hrm_m_edu
           ";

        var parameters = new
        {
            userweb_id = authorization.Id
        };
        var result = await connection.ExecuteQueryAsync(sql, parameters, transaction: transaction);
        transaction.Commit();
        return result.ToList();
    }
}