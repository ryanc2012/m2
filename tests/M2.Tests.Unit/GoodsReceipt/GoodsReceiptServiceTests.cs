using FluentAssertions;
using M2.Domain.GoodsReceipt;
using M2.SharedKernel;
using Moq;

namespace M2.Tests.Unit.GoodsReceipt;

/// <summary>
/// Domain invariant tests for GoodsReceiptNote and contract tests for IGoodsReceiptService.
/// Covers the goods receipt lifecycle: Pending → Received → Confirmed | Discrepancy.
/// </summary>
public class GoodsReceiptServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();

    private GoodsReceiptNote BuildNote(string dnNumber = "SAP-DN-001") =>
        new(_tenantId, _shopId, dnNumber);

    [Fact]
    public void CreateAsync_SetsStatusToPending()
    {
        // A newly-created GRN must start in Pending status — no goods have arrived yet.
        var note = BuildNote();

        note.Status.Should().Be(GoodsReceiptStatus.Pending,
            "a newly created goods receipt note must always start in Pending status");
        note.ConfirmedAt.Should().BeNull(
            "ConfirmedAt must be null until the GRN is confirmed");
        note.DiscrepancyNote.Should().BeNull(
            "DiscrepancyNote must be null for a fresh GRN with no reported issues");
    }

    [Fact]
    public void ConfirmAsync_SetsStatusToConfirmed()
    {
        // Confirming a GRN transitions it to Confirmed and stamps ConfirmedAt.
        var note = BuildNote();

        note.Confirm();

        note.Status.Should().Be(GoodsReceiptStatus.Confirmed,
            "calling Confirm() must transition the GRN to Confirmed status");
        note.ConfirmedAt.Should().NotBeNull(
            "ConfirmedAt must be stamped when the GRN is confirmed");
        note.ConfirmedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ConfirmAsync_ThrowsWhenAlreadyConfirmed()
    {
        // Confirming an already-confirmed GRN is an invariant violation.
        var note = BuildNote();
        note.Confirm();

        var act = () => note.Confirm();

        act.Should().Throw<InvalidOperationException>(
            "a GRN that is already Confirmed must not be confirmed again — double-confirm is a data integrity error");
    }

    [Fact]
    public void RecordDiscrepancy_SetsStatusToDiscrepancy()
    {
        // Recording a discrepancy flags the GRN for manual review.
        var note = BuildNote();
        const string discrepancyNote = "Received 95 units; delivery note says 100.";

        note.RecordDiscrepancy(discrepancyNote);

        note.Status.Should().Be(GoodsReceiptStatus.Discrepancy,
            "recording a discrepancy must set status to Discrepancy for manual review");
        note.DiscrepancyNote.Should().Be(discrepancyNote,
            "the discrepancy note text must be persisted verbatim");
    }

    [Fact]
    public void RecordDiscrepancy_RequiresNote()
    {
        // An empty or whitespace-only note provides no actionable context — reject it.
        var note = BuildNote();

        var actEmpty = () => note.RecordDiscrepancy(string.Empty);
        var actWhitespace = () => note.RecordDiscrepancy("   ");

        actEmpty.Should().Throw<ArgumentException>(
            "RecordDiscrepancy with an empty note must throw — a note is required for traceability");
        actWhitespace.Should().Throw<ArgumentException>(
            "RecordDiscrepancy with a whitespace-only note must throw — it provides no actionable information");
    }
}
