namespace DukkanPilot.Web.Models.Help;

public class HelpArticle
{
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string RoleScope { get; set; } = string.Empty;

    public int EstimatedReadMinutes { get; set; }

    public string Difficulty { get; set; } = "Başlangıç";

    public IReadOnlyList<string> Steps { get; set; } = Array.Empty<string>();

    public IReadOnlyList<HelpRelatedLink> RelatedLinks { get; set; } = Array.Empty<HelpRelatedLink>();

    public IReadOnlyList<string> Tips { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

    public string SearchKeywords { get; set; } = string.Empty;

    public IReadOnlyList<string> RelatedArticleSlugs { get; set; } = Array.Empty<string>();
}

public class HelpRelatedLink
{
    public string Text { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool OpenInNewTab { get; set; }
}

public class HelpArticleCardViewModel
{
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public int EstimatedReadMinutes { get; set; }

    public string SearchKeywords { get; set; } = string.Empty;
}

public class HelpCategoryGroupViewModel
{
    public string Category { get; set; } = string.Empty;

    public IReadOnlyList<HelpArticleCardViewModel> Articles { get; set; } = Array.Empty<HelpArticleCardViewModel>();
}

public class HelpQuickLinkViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Icon { get; set; } = "📄";
}

public class HelpCenterIndexViewModel
{
    public string RoleScope { get; set; } = string.Empty;

    public string PageTitle { get; set; } = string.Empty;

    public string Intro { get; set; } = string.Empty;

    public string ArticleBasePath { get; set; } = string.Empty;

    public IReadOnlyList<HelpCategoryGroupViewModel> Categories { get; set; } = Array.Empty<HelpCategoryGroupViewModel>();

    public IReadOnlyList<HelpArticleCardViewModel> PopularArticles { get; set; } = Array.Empty<HelpArticleCardViewModel>();

    public IReadOnlyList<HelpQuickLinkViewModel> QuickLinks { get; set; } = Array.Empty<HelpQuickLinkViewModel>();

    public IReadOnlyList<HelpArticleCardViewModel> StarterSteps { get; set; } = Array.Empty<HelpArticleCardViewModel>();

    public bool ShowStaffTraining { get; set; }

    public string? StaffTrainingArticleSlug { get; set; }
}

public class HelpArticleDetailViewModel
{
    public HelpArticle Article { get; set; } = new();

    public string ArticleBasePath { get; set; } = string.Empty;

    public string IndexPath { get; set; } = string.Empty;

    public IReadOnlyList<HelpArticleCardViewModel> RelatedArticles { get; set; } = Array.Empty<HelpArticleCardViewModel>();

    public bool ShowPublicCtas { get; set; }
}
