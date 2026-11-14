using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.ServiceDefaults.HealthChecks;

/// <summary>JSON-відповідь health check: статус, тривалість, теги й опис кожної перевірки.</summary>
public static class HealthJsonWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report, bool includeErrors)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("status", report.Status.ToString());
            writer.WriteNumber("totalDurationMs", report.TotalDuration.TotalMilliseconds);
            writer.WriteStartObject("entries");
            foreach (var (name, entry) in report.Entries)
            {
                writer.WriteStartObject(name);
                writer.WriteString("status", entry.Status.ToString());
                writer.WriteString("description", entry.Description);
                writer.WriteNumber("durationMs", entry.Duration.TotalMilliseconds);
                writer.WriteStartArray("tags");
                foreach (var tag in entry.Tags) writer.WriteStringValue(tag);
                writer.WriteEndArray();
                if (includeErrors && entry.Exception is not null)        // текст помилки лише в Development
                    writer.WriteString("error", entry.Exception.Message);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return context.Response.WriteAsync(Encoding.UTF8.GetString(stream.ToArray()));
    }
}