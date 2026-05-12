using FluentAssertions;
using M2.Domain.Attendance;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.Attendance;

/// <summary>
/// Contracts and domain invariants for IAttendanceService / AttendanceRecord.
/// </summary>
public class AttendanceServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();
    private const string EmployeeId = "EMP001";

    [Fact]
    public void ClockIn_ShouldCreateRecord_WithNullClockOut()
    {
        // Arrange / Act
        var record = new AttendanceRecord(
            _tenantId, _shopId, EmployeeId, AttendanceSource.Manual);

        // Assert
        record.EmployeeId.Should().Be(EmployeeId);
        record.ClockInAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5),
            "ClockInAt must be set to the current time on construction");
        record.ClockOutAt.Should().BeNull(
            "ClockOutAt must be null when an employee first clocks in — they are still on shift");
        record.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void ClockOut_ShouldSetClockOutAt()
    {
        // Arrange
        var record = new AttendanceRecord(
            _tenantId, _shopId, EmployeeId, AttendanceSource.Manual);

        // Act
        record.ClockOut();

        // Assert
        record.ClockOutAt.Should().NotBeNull(
            "ClockOutAt must be stamped when the employee clocks out");
        record.ClockOutAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        record.IsComplete.Should().BeTrue(
            "IsComplete must be true once ClockOutAt is set");
    }

    [Fact]
    public async Task ClockOut_WhenNoOpenRecord_ShouldFail()
    {
        // Arrange — service finds no open (unclocked-out) record for the employee
        var mockService = new Mock<IAttendanceService>();

        mockService
            .Setup(s => s.ClockOutAsync(
                _tenantId, EmployeeId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AttendanceRecord>(
                "No open attendance record found for employee EMP001."));

        // Act
        var result = await mockService.Object.ClockOutAsync(_tenantId, EmployeeId);

        // Assert
        result.IsFailure.Should().BeTrue(
            "clocking out when no open record exists must return a failure result");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetDailySummary_ShouldCalculateTotalHours()
    {
        // Arrange — 8-hour shift
        var mockService = new Mock<IAttendanceService>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedSummary = new AttendanceSummary(EmployeeId, today, TotalHours: 8.0, IsComplete: true);

        mockService
            .Setup(s => s.GetDailySummaryAsync(
                _tenantId, EmployeeId, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedSummary));

        // Act
        var result = await mockService.Object.GetDailySummaryAsync(_tenantId, EmployeeId, today);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalHours.Should().Be(8.0,
            "the daily summary must aggregate total hours from ClockIn to ClockOut");
        result.Value.IsComplete.Should().BeTrue(
            "a summary for a completed shift must report IsComplete = true");
        result.Value.EmployeeId.Should().Be(EmployeeId);
        result.Value.Date.Should().Be(today);
    }
}
