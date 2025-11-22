namespace WebAuthen.Models;

public record CompanyDto
{
    public string? COMP_NAME_TH { get; init; } = "";
    public string? COMP_SHORT_NAME { get; init; } = "";
    public string? COMP_CONTRACT { get; init; } = "";
    public string? COMP_TAX_ID { get; init; } = "";
    public string? COMP_COMMERCIAL_NO { get; init; } = "";
    public string? COMP_BRANCH { get; init; } = "";
    public string? COMP_ADDRESS { get; init; } = "";
    public string? COMP_TEL { get; init; } = "";
    public string? COMP_FAX { get; init; } = "";
    public string? COMP_WEB { get; init; } = "";
    public int? COMP_VAT { get; init; }
    public string? COMP_SSO_ID { get; init; } = "";
    public string? COMP_IMAGE { get; init; } = "";
    public int? COMP_TYPE_ID { get; init; }
    public string? COMP_TYPE_NAME { get; init; } = "";
    public int? COMP_EMP_AMOUNT { get; init; }
    public string? created_by { get; init; } = "";
    public string? update_by { get; init; } = "";
    public int comp_id { get; init; }

}
public record CompDepartmentDto
{
    public int? parent_id { get; init; }
    public required string dep_name { get; init; }
    public int comp_id { get; init; }

    public int dep_id { get; init; }
    public string created_by { get; init; } = "";
    public string update_by { get; init; } = "";
}

public record CompdepartmentDeleteAllDto
{
    public int comp_id { get; init; }
    public int dep_id { get; init; }
    public string update_by { get; init; } = "";
}


public record PositionDto
{

    public string pos_name_th { get; init; } = "";
    public int comp_id { get; init; }

    public int pos_id { get; init; }
    public string created_by { get; init; } = "";
    public string update_by { get; init; } = "";

}
public record CompStructureDto
{
    public int? parent_id { get; init; }
    public int comp_id { get; init; }
    public int dep_id { get; init; }
    public int pos_id { get; init; }
    public string created_by { get; init; } = "";
    public string update_by { get; init; } = "";
    public int org_id { get; init; }

}

public record MainCalendarDto
{
    public int c_id { get; init; }
    public string c_name { get; init; } = "";
    public int? c_parent_id { get; init; }
    public string created_by { get; init; } = "";
    public string update_by { get; init; } = "";
}

public record holidaytypeDto
{
    public int h_type_id { get; init; }
    public string h_type_name { get; init; } = "";
    public int is_holiday { get; init; }
    public string h_color { get; init; } = "";
    public string created_by { get; init; } = "";
    public string update_by { get; init; } = "";
}

public record holidayDto
{
    public int h_id { get; init; }
    public int c_id { get; init; }
    public string? h_date { get; init; }
    public int h_type_id { get; init; }
    public string h_detail { get; init; } = "";
    public string created_by { get; init; } = "";
    public string update_by { get; init; } = "";

    public string month { get; init; } = "";
    public string year { get; init; } = "";
}

public class WorkshiftRequestDto
{
    public string ws_name { get; set; } = default!;
    public string ws_color { get; set; } = default!;
    public int ws_type { get; set; }
    public string ws_remark { get; set; } = default!;
    public int auto_time { get; set; }
    public List<shift_item>? shift_items { get; set; } = [];
    public List<ot_break>? ot_breaks { get; set; }
    public string? username { get; set; } = default!;
}

public class shift_item
{
    public int wsi_set { get; set; }
    public string? time_in { get; set; } = default!;
    public int? time_in_day { get; set; }
    public string? time_out { get; set; } = default!;
    public int? time_out_day { get; set; }
    public int? time_hours { get; set; }
    public int? time_minute { get; set; }
    public List<time_break>? time_breaks { get; set; } = [];
}

public class time_break
{
    public string? tb_in { get; set; } = default!;
    public string? tb_out { get; set; } = default!;
    public int auto_cut { get; set; }
    public int? tb_in_day { get; set; }
    public int? tb_out_day { get; set; }
    public int? tb_minute { get; set; }
    public int? tb_start_hour { get; set; }
    public int? tb_start_minute { get; set; }
}

public class ot_break
{
    public string? ot_break_in { get; set; } = default!;
    public string? ot_break_out { get; set; } = default!;
    public int ot_break_type { get; set; }
    public int auto_cut { get; set; }
    public int? ot_break_in_day { get; set; }
    public int? ot_break_out_day { get; set; }
    public int? ot_break_minute { get; set; }
    public int? ot_break_start_hour { get; set; }
    public int? ot_break_start_minute { get; set; }
}

public class personalDto
{
    public int personal_id { get; set; }
    public int? title_id { get; set; }
    public int? id_stamp { get; set; }
    public string? personal_code { get; set; }
    public string? firstname_th { get; set; }
    public string? lastname_th { get; set; }
    public string? id_card { get; set; }
    public DateOnly? dateofbirth { get; set; }
    public string? sex { get; set; }
    public string? nationality { get; set; }
    public string? race { get; set; }
    public string? religion { get; set; }
    public string? tel { get; set; }
    public string? bankacc { get; set; }
    public int? bank_id { get; set; }
    public int? status_id { get; set; }
    public string? address { get; set; }
    public string? address2 { get; set; }
    public string? disability_id { get; set; }
    public int? emptype_id { get; set; }
    public DateOnly? date_start { get; set; }
    public DateOnly? date_out { get; set; }
    public int? org_id { get; set; }
    public int? cost_id { get; set; }
    public string? created_by { get; set; } = "";
    public string? update_by { get; set; } = "";
    public string? created_date { get; set; } = "";
    public string? update_date { get; set; } = "";
    public int? user_id { get; set; }
    public int? edu_id { get; set; }
    public string? edu_details { get; set; }
    public string? hosp_right { get; set; }
    public string? sso_no { get; set; }
    public string? tax_no { get; set; }
    public string? nickname { get; set; }
    public
}