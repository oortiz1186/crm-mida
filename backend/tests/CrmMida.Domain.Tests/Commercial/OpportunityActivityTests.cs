using CrmMida.Domain.Commercial;
using Xunit;

namespace CrmMida.Domain.Tests.Commercial;

public sealed class OpportunityActivityTests
{
    [Fact]
    public void Opportunity_clamps_probability_and_amount()
    {
        var companyId = Guid.NewGuid();
        var item = new Opportunity(" Renovación anual ", companyId, 1000);

        item.Update(" Renovación anual ", companyId, null, null, null, " Comercial Premium ", -100, 140, null, "qualification", "open", null, " Nota ");

        Assert.Equal("Renovación anual", item.Name);
        Assert.Equal("Comercial Premium", item.ProductOrService);
        Assert.Equal(0, item.EstimatedAmount);
        Assert.Equal(100, item.Probability);
    }

    [Fact]
    public void Opportunity_won_and_lost_stages_update_status()
    {
        var item = new Opportunity("Venta", Guid.NewGuid(), 5000);

        item.MoveToStage("won");
        Assert.Equal("won", item.Status);
        Assert.Equal(100, item.Probability);

        item.MoveToStage("lost", "Sin presupuesto");
        Assert.Equal("lost", item.Status);
        Assert.Equal(0, item.Probability);
        Assert.Equal("Sin presupuesto", item.LossReason);
    }

    [Fact]
    public void Activity_completion_sets_timestamp()
    {
        var activity = new Activity("call", "Llamar al cliente", DateTime.UtcNow.AddDays(1));

        activity.SetStatus("completed");

        Assert.Equal("completed", activity.Status);
        Assert.NotNull(activity.CompletedAtUtc);
    }
}
