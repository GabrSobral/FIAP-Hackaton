using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using fiap_hackaton.Domain.Entities.Analysis;

namespace report_service;

public static class PdfReportGenerator
{
    // Design tokens — mirrors frontend variables.css
    private const string BgSurface    = "#ffffff";
    private const string BgInset      = "#e2e8f0";
    private const string TextPrimary  = "#0f172a";
    private const string TextSecondary = "#475569";
    private const string TextMuted    = "#94a3b8";
    private const string Border       = "#e2e8f0";
    private const string BorderStrong = "#cbd5e1";

    private const string Blue        = "#3b82f6";
    private const string Red         = "#ef4444";
    private const string Green       = "#10b981";
    private const string Amber       = "#f59e0b";

    private const string Font = "Inter";

    // ── Public API ────────────────────────────────────────────────────────────

    public static byte[] Generate(Report report) =>
        Document.Create(c => c.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(t => t
                .FontFamily(Font)
                .FontSize(11)
                .FontColor(TextSecondary));

            page.Header().Element(Header(report));
            page.Content().Element(Content(report));
            page.Footer().Element(Footer());
        })).GeneratePdf();

    // ── Header ────────────────────────────────────────────────────────────────

    private static Action<IContainer> Header(Report r) => c =>
        c.PaddingBottom(16).BorderBottom(1).BorderColor(Border)
         .Row(row =>
         {
             row.RelativeItem().Column(col =>
             {
                 col.Item().Text("ArchAnalyzer")
                    .FontSize(22).FontColor(TextPrimary).Bold();
                 col.Item().Text("AI-Powered Architecture Review")
                    .FontSize(10).FontColor(TextMuted);
             });
             row.AutoItem().AlignRight().AlignBottom().Column(col =>
             {
                 col.Item().Text($"Generated {r.GeneratedAt:yyyy-MM-dd HH:mm} UTC")
                    .FontSize(9).FontColor(TextMuted);
                 col.Item().Text($"ID: {r.AnalysisId}")
                    .FontSize(8).FontColor(TextMuted);
             });
         });

    // ── Content ───────────────────────────────────────────────────────────────

    private static Action<IContainer> Content(Report r) => c =>
        c.PaddingTop(20).Column(col =>
        {
            col.Spacing(16);
            col.Item().Element(Card(
                "Components",
                "Identified services, databases, queues, and their relationships",
                r.Components, Blue));
            col.Item().Element(Card(
                "Risks",
                "Architectural risks, single points of failure, and security concerns",
                r.Risks, Red));
            col.Item().Element(Card(
                "Recommendations",
                "Concrete improvements, best practices, and architectural guidance",
                r.Recommendations, Green));

            if (!string.IsNullOrWhiteSpace(r.Feedback))
                col.Item().Element(Card(
                    "AI Feedback",
                    "Overall expert assessment of architecture quality and maturity",
                    r.Feedback, Amber));
        });

    // ── Section card ──────────────────────────────────────────────────────────

    private static Action<IContainer> Card(
        string title, string desc, string markdown, string accent) => c =>
        c.Border(1).BorderColor(Border).Background(BgSurface)
         .Column(col =>
         {
             // 3px top accent stripe (matches .card-stripe in frontend)
             col.Item().Height(3).Background(accent);

             // Card header
             col.Item().BorderBottom(1).BorderColor(Border).Padding(16)
                .Row(row =>
                {
                    row.ConstantItem(4).Background(accent);  // colored left bar
                    row.ConstantItem(10);                     // spacer
                    row.RelativeItem().Column(hcol =>
                    {
                        hcol.Item().Text(title)
                            .FontSize(13).FontColor(TextPrimary).Bold();
                        hcol.Item().Text(desc)
                            .FontSize(9).FontColor(TextMuted);
                    });
                });

             // Markdown body
             col.Item().Padding(16).Element(RenderMarkdown(markdown));
         });

    // ── Footer ────────────────────────────────────────────────────────────────

    private static Action<IContainer> Footer() => c =>
        c.BorderTop(1).BorderColor(Border).PaddingTop(8)
         .Row(row =>
         {
             row.RelativeItem().Text("ArchAnalyzer — FIAP Hackathon")
                .FontSize(8).FontColor(TextMuted);
             row.AutoItem().Text(t =>
             {
                 t.DefaultTextStyle(s => s.FontSize(8).FontColor(TextMuted));
                 t.Span("Page "); t.CurrentPageNumber();
                 t.Span(" of "); t.TotalPages();
             });
         });

    // ── Markdown → QuestPDF ──────────────────────────────────────────────────

    private static Action<IContainer> RenderMarkdown(string md) => c =>
    {
        if (string.IsNullOrWhiteSpace(md))
        {
            c.Text("No content").FontColor(TextMuted).Italic();
            return;
        }

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var doc = Markdown.Parse(md, pipeline);

        c.Column(col =>
        {
            col.Spacing(6);
            foreach (var block in doc)
                col.Item().Element(RenderBlock(block));
        });
    };

    private static Action<IContainer> RenderBlock(Block block) => block switch
    {
        HeadingBlock h       => RenderHeading(h),
        ParagraphBlock p     => RenderParagraph(p),
        ListBlock l          => RenderList(l, 0),
        FencedCodeBlock code => RenderCodeBlock(code),
        CodeBlock code       => RenderCodeBlock(code),
        QuoteBlock q         => RenderQuote(q),
        ThematicBreakBlock _ => RenderHr(),
        _                    => _ => { }
    };

    private static Action<IContainer> RenderHeading(HeadingBlock h) => c =>
    {
        var size = h.Level switch { 1 => 15f, 2 => 13f, _ => 11f };
        c.PaddingTop(h.Level <= 2 ? 8 : 4)
         .Text(InlineText(h.Inline))
         .FontSize(size).FontColor(TextPrimary).Bold();
    };

    private static Action<IContainer> RenderParagraph(ParagraphBlock p) => c =>
        c.Text(t =>
        {
            t.DefaultTextStyle(s => s
                .FontSize(11).FontColor(TextSecondary).LineHeight(1.6f));
            RenderInlines(t, p.Inline);
        });

    private static Action<IContainer> RenderList(ListBlock list, int depth) => c =>
        c.Column(col =>
        {
            col.Spacing(3);
            int n = 1;
            foreach (var item in list.OfType<ListItemBlock>())
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(16 + depth * 12)
                       .AlignRight().PaddingRight(4)
                       .Text(list.IsOrdered ? $"{n++}." : "•")
                       .FontSize(11).FontColor(TextSecondary);

                    row.RelativeItem().Column(ic =>
                    {
                        ic.Spacing(3);
                        foreach (var child in item)
                        {
                            if (child is ListBlock nested)
                                ic.Item().Element(RenderList(nested, depth + 1));
                            else
                                ic.Item().Element(RenderBlock(child));
                        }
                    });
                });
            }
        });

    private static Action<IContainer> RenderCodeBlock(LeafBlock code) => c =>
    {
        var raw = string.Join("\n", code.Lines.Lines
            .Take(code.Lines.Count)
            .Select(l => l.Slice.ToString()));
        c.Background(BgInset).Border(1).BorderColor(Border).Padding(10)
         .Text(raw).FontFamily("Courier New").FontSize(9).FontColor(TextPrimary);
    };

    private static Action<IContainer> RenderQuote(QuoteBlock q) => c =>
        c.Row(row =>
        {
            row.ConstantItem(3).Background(BorderStrong);
            row.RelativeItem().PaddingLeft(10).Column(col =>
            {
                col.Spacing(4);
                foreach (var child in q)
                    col.Item().Element(RenderBlock(child));
            });
        });

    private static Action<IContainer> RenderHr() => c =>
        c.PaddingVertical(6).LineHorizontal(1).LineColor(Border);

    // ── Inline helpers ────────────────────────────────────────────────────────

    private static string InlineText(ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var node in inline)
        {
            sb.Append(node switch
            {
                LiteralInline lit        => lit.Content.ToString(),
                CodeInline ci            => ci.Content,
                EmphasisInline em        => InlineText(em),
                ContainerInline ci2      => InlineText(ci2),
                _                        => string.Empty,
            });
        }
        return sb.ToString();
    }

    private static void RenderInlines(TextDescriptor t, ContainerInline? inline)
    {
        if (inline is null) return;
        foreach (var node in inline)
        {
            switch (node)
            {
                case LiteralInline lit:
                    t.Span(lit.Content.ToString());
                    break;
                case EmphasisInline { DelimiterCount: 2 } em:
                    t.Span(InlineText(em)).Bold().FontColor(TextPrimary);
                    break;
                case EmphasisInline em:
                    t.Span(InlineText(em)).Italic();
                    break;
                case CodeInline ci:
                    t.Span(ci.Content).FontFamily("Courier New")
                     .FontSize(9.5f).FontColor(Blue);
                    break;
                case LinkInline link:
                    t.Span(InlineText(link)).FontColor(Blue).Underline();
                    break;
                case LineBreakInline:
                    t.Span("\n");
                    break;
                case ContainerInline container:
                    RenderInlines(t, container);
                    break;
            }
        }
    }
}
