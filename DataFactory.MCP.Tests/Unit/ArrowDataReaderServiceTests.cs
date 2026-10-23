using Apache.Arrow;
using Apache.Arrow.Ipc;
using DataFactory.MCP.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DataFactory.MCP.Tests.Unit;

public class ArrowDataReaderServiceTests
{
    [Fact]
    public async Task ReadArrowStreamAsync_WithDateColumns_ReturnsCalendarDates()
    {
        // Arrange
        var date = new DateTime(2026, 9, 26);
        var batch = new RecordBatch.Builder()
            .Append("day", false, col => col.Date32(array => array.Append(date)))
            .Append("moment", false, col => col.Date64(array => array.Append(date)))
            .Build();
        using var stream = new MemoryStream();
        using (var writer = new ArrowStreamWriter(stream, batch.Schema, leaveOpen: true))
        {
            await writer.WriteRecordBatchAsync(batch);
            await writer.WriteEndAsync();
        }
        var service = new ArrowDataReaderService(NullLogger<ArrowDataReaderService>.Instance);

        // Act
        var summary = await service.ReadArrowStreamAsync(stream.ToArray());

        // Assert
        Assert.True(summary.ArrowParsingSuccess, summary.ArrowParsingError);
        Assert.Equal("2026-09-26", summary.StructuredSampleData!["day"][0]);
        Assert.Equal("2026-09-26", summary.StructuredSampleData["moment"][0]);
    }
}
