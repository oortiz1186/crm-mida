using CrmMida.Domain.Commercial;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CrmMida.Api.Commercial;

public sealed class QuotePdfService
{
    public byte[] Generate(Quote quote)
    {
        var company = quote.Company ?? throw new InvalidOperationException("La empresa no fue cargada.");

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text("MIDA").FontSize(22).Bold();
                        column.Item().Text("Cotización comercial").FontSize(12);
                    });
                    row.ConstantItem(180).AlignRight().Column(column =>
                    {
                        column.Item().Text(quote.Folio).Bold();
                        column.Item().Text($"Fecha: {quote.CreatedAtUtc:dd/MM/yyyy}");
                        column.Item().Text($"Vigencia: {quote.ValidUntilUtc:dd/MM/yyyy}");
                    });
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(client =>
                    {
                        client.Item().Text(company.TradeName).Bold().FontSize(13);
                        client.Item().Text(company.BusinessName);
                        client.Item().Text($"RFC: {company.Rfc}");
                        if (!string.IsNullOrWhiteSpace(company.Email)) client.Item().Text(company.Email);
                    });

                    column.Item().Text(quote.Title).FontSize(16).Bold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(5);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        static IContainer Header(IContainer container) => container.Background(Colors.Grey.Lighten3).Padding(6);
                        table.Header(header =>
                        {
                            header.Cell().Element(Header).Text("Descripción").Bold();
                            header.Cell().Element(Header).AlignRight().Text("Cant.").Bold();
                            header.Cell().Element(Header).AlignRight().Text("Precio").Bold();
                            header.Cell().Element(Header).AlignRight().Text("IVA").Bold();
                            header.Cell().Element(Header).AlignRight().Text("Total").Bold();
                        });

                        foreach (var item in quote.Items)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(item.Description);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text(item.Quantity.ToString("N2"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{item.UnitPrice:N2}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{item.TaxRate:N2}%");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{item.Total:N2}");
                        }
                    });

                    column.Item().AlignRight().Width(240).Column(totals =>
                    {
                        totals.Item().Row(row => { row.RelativeItem().Text("Subtotal"); row.ConstantItem(100).AlignRight().Text($"{quote.Currency} {quote.Subtotal:N2}"); });
                        totals.Item().Row(row => { row.RelativeItem().Text("IVA"); row.ConstantItem(100).AlignRight().Text($"{quote.Currency} {quote.Tax:N2}"); });
                        totals.Item().Row(row => { row.RelativeItem().Text("Descuento"); row.ConstantItem(100).AlignRight().Text($"{quote.Currency} {quote.Discount:N2}"); });
                        totals.Item().PaddingTop(6).BorderTop(1).Row(row => { row.RelativeItem().Text("TOTAL").Bold(); row.ConstantItem(100).AlignRight().Text($"{quote.Currency} {quote.Total:N2}").Bold(); });
                    });

                    if (!string.IsNullOrWhiteSpace(quote.Notes)) column.Item().Text($"Notas: {quote.Notes}");
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("CRM MIDA · ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
