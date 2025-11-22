using System.Text.Json.Serialization;

namespace WebAuthen.Models;

public record WorkSpaceDto
{
    public string? Space_Name { get; set; }
    public List<int> Tag_ids { get; set; } = []; // tags name
    public string? Img_Name { get; set; } // image name
    public string? Img_Type { get; set; } // image type
    public string? Img { get; set; }
    public string? Create_By { get; set; }
}

public sealed class WorkspaceResult
{
    [JsonPropertyName("space_id")]
    public int space_id { get; init; }

    [JsonPropertyName("space_name")]
    public string space_name { get; init; } = "";

    [JsonPropertyName("img_path")]
    public string? img_path { get; init; }

    [JsonPropertyName("tags_name")]
    public List<string> tags_name { get; init; } = new();
}

public record TagDto
{
    public required string Tags_Name { get; set; }
    public required string Create_By { get; set; }
}

public record FolderWorkSpaceDto
{
    public required string folder_name { get; set; }
    public int space_id { get; set; }
    public string? folder_color { get; set; }
    public required string create_by { get; set; }

}

public record FolderUpdateDto
{
    public string? folder_name { get; set; }
    public string? space_id { get; set; }
    public string? folder_coler { get; set; }
    public int? folder_id { get; set; }
    public string? update_by { get; set; }

}

public record DocumentDto
{
    public required string doc_name { get; init; }
    public string? doc_description { get; init; }
    public required int folder_id { get; init; }
    public List<DocumentFileDto>? doc_attachment { get; init; }
    public required string create_by { get; init; }
}

public record DocumentFileDto(string file_name, string file_type, string file_base64);

public record FileUpdateDto
{
    public string? file_name { get; set; }
    public string? file_html { get; set; }
    public string? folder_id { get; set; }
    public int? file_id { get; set; }
    public string? update_by { get; set; }

}

public record ImageDto
{
    public required string Img_Name { get; set; }
    public required string Img_Type { get; set; }
    public required string Img_Base64 { get; set; }
}

public record UploadImageDto
{
    public required List<ImageDto> Images { get; set; }
}





